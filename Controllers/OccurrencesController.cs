using EcoCityWaste.Data;
using EcoCityWaste.Models;
using EcoCityWaste.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace EcoCityWaste.Controllers
{
    /// <summary>
    /// Controlador responsável por todo o ciclo de vida das ocorrências.
    /// Gere desde o reporte inicial pelo cidadão até à atribuição e resolução pelos funcionários.
    /// </summary>
    public class OccurrencesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public OccurrencesController(AppDbContext context, IWebHostEnvironment webHostEnvironment, IConfiguration configuration, NotificationService notificationService, FailureLogger failureLogger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _hideResolvedAfterDays = configuration.GetValue<int>("HideResolvedAfterDays", 30);
            _notificationService = notificationService;
            _failureLogger = failureLogger;
        }

        /// <summary>
        /// Apresenta o formulário de reporte de anomalia. 
        /// Carrega a lista de contentores para que o cidadão possa selecionar o local exato.
        /// </summary>
        public IActionResult Report()
        {
            var containers = _context.Contentores
                .Select(c => new {
                    c.Code,
                    c.Location,
                    c.Type,
                    c.Status,
                    c.FillLevel
                }).ToList();

            var translate = containers
                .Select(c => new {
                    c.Code,
                    c.Location,
                    c.Type,
                    Status = c.Status switch
                    {
                        Container.ContainerStatus.Good => "Bom",
                        Container.ContainerStatus.Full => "Cheio",
                        Container.ContainerStatus.Empty => "Vazio",
                        Container.ContainerStatus.Broken => "Avariado",
                        Container.ContainerStatus.Maintenance => "Manutenção",
                        _ => "Desconhecido"
                    },
                    c.FillLevel
                }).ToList();

            ViewBag.ContainersList = translate;
            // Serialização para permitir que o JavaScript use os dados dos contentores
            ViewBag.ContainersJson = JsonSerializer.Serialize(translate);

            return View(new ReportOccurrenceViewModel());
        }

        /// <summary>
        /// Processa a submissão de uma nova ocorrência.
        /// Inclui o upload de imagens e a associação automática ao utilizador autenticado.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Report(ReportOccurrenceViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.Clear();
                ViewBag.Error = "Faltam preencher campos obrigatórios. Por favor, tente novamente.";

                // Recarrega dados necessários para a View em caso de erro
                var containersBD = _context.Contentores.ToList();
                ViewBag.ContainersList = containersBD;
                ViewBag.ContainersJson = JsonSerializer.Serialize(containersBD);

                return View(new ReportOccurrenceViewModel());
            }

            try
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int.TryParse(userIdString, out int userId);

                // Gravação física da fotografia no servidor
                string? photoPath = await SaveOccurrencePhotoAsync(model.Photo);

                var occurrence = new Occurrence
                {
                    ContainerCode = model.ContainerCode,
                    OccurrenceType = model.OccurrenceType,
                    Description = model.Description,
                    ReportDate = DateTime.Now,
                    Status = OccurrenceStatus.Pendente.ToString(),
                    UserId = userId,
                    ImagePath = photoPath
                };

                _context.Occurrences.Add(occurrence);
                await _context.SaveChangesAsync();

                ModelState.Clear();
                ViewBag.Success = "Obrigado! A anomalia foi registada e será analisada em breve.";
                return View();
            }
            catch (Exception)
            {
                // Registo de falha na base de dados
                await _failureLogger.LogAsync(ex, nameof(OccurrencesController), nameof(Report), User.Identity?.Name);
                ViewBag.Error = "Erro técnico ao submeter o reporte. Tente mais tarde.";
                return View(model);
            }
        }

        /// <summary>
        /// Permite ao cidadão consultar o estado dos seus próprios reportes.
        /// Implementa uma limpeza visual, escondendo ocorrências resolvidas há muito tempo.
        /// </summary>
        public async Task<IActionResult> Status()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId)) return RedirectToAction("Login", "Account");

            // Define o limite temporal para não sobrecarregar a lista do utilizador
            var cutoffDate = DateTime.Now.AddDays(-_hideResolvedAfterDays);

            var reports = await _context.Occurrences
                .Where(o => o.UserId == userId)
                .Where(o =>
                    (o.Status != "Resolvido" && o.Status != "Rejeitado") ||
                    (o.LastUpdatedAt >= cutoffDate))
                .OrderByDescending(o => o.ReportDate)
                .ToListAsync();

            ViewBag.HideAfterDays = _hideResolvedAfterDays;
            return View(reports);
        }

        /// <summary>
        /// Carrega a página de atribuição de tarefas (Admin).
        /// Mostra ocorrências pendentes e a carga de trabalho atual de cada funcionário.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Assign()
        {
            var occurrences = await _context.Occurrences.Where(o => o.AssignedEmployeeId == null).ToListAsync();
            var employees = await _context.Users.Where(u => u.Role == "Funcionario").ToListAsync();

            // Estatística rápida para ajudar o Admin a equilibrar as tarefas pelos funcionários
            var occurrenceCounts = await _context.Occurrences
                .Where(o => o.AssignedEmployeeId.HasValue)
                .GroupBy(o => o.AssignedEmployeeId.Value)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            var vm = new AssignOccurrenceViewModel
            {
                Occurrences = occurrences,
                Employees = employees,
                EmployeeOccurrenceCounts = occurrenceCounts
            };

            return View(vm);
        }

        /// <summary>
        /// Processa a atribuição de uma ocorrência a um funcionário e despoleta as notificações automáticas.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Assign(AssignOccurrenceViewModel model)
        {
            if (model.SelectedOccurrenceId == 0 || model.SelectedEmployeeId == 0)
            {
                TempData["Error"] = "Seleção inválida.";
                return RedirectToAction("Assign");
            }

            var occurrence = await _context.Occurrences.FindAsync(model.SelectedOccurrenceId);
            if (occurrence == null) return NotFound();

            occurrence.AssignedEmployeeId = model.SelectedEmployeeId;
            occurrence.Status = OccurrenceStatus.EmAnalise.ToString();
            occurrence.AssignedAt = DateTime.Now;

            _context.Notifications.Add(new Notification
            {
                Message = $"Foi-lhe atribuída uma ocorrência ({occurrence.OccurrenceType}).",
                UserId = model.SelectedEmployeeId,
                CreatedAt = DateTime.Now,
                IsRead = false
            });
            await _context.SaveChangesAsync();

            // Sistema de Notificações em tempo real
            await _notificationService.CreateNotificationAsync(
                $"Nova ocorrência atribuída: {occurrence.OccurrenceType}.",
                model.SelectedEmployeeId, "/Occurrences/AssignedIncidents", "occurrence");

            await _notificationService.CreateNotificationAsync(
                $"A sua ocorrência está agora em análise pela equipa técnica.",
                occurrence.UserId, "/Occurrences/Status", "occurrence");

            TempData["Success"] = "Ocorrência atribuída com sucesso!";
            return RedirectToAction("Assign");
        }

        /// <summary>
        /// Permite ao funcionário atualizar o progresso de uma tarefa que lhe foi confiada.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(UpdateStatusViewModel model)
        {
            ModelState.Remove("CurrentStatus");
            if (!ModelState.IsValid) return View(model);

            var occurrence = await _context.Occurrences.FindAsync(model.OccurrenceId);
            if (occurrence == null) return NotFound();

            occurrence.Status = model.NewStatus.ToString();
            occurrence.LastUpdatedAt = DateTime.Now;

            _context.Occurrences.Update(occurrence);
            await _context.SaveChangesAsync();

            // Notifica o cidadão sobre o desfecho ou progresso do seu reporte
            await _notificationService.CreateNotificationAsync(
                $"O estado da sua ocorrência foi alterado para: {occurrence.Status}.",
                occurrence.UserId, "/Occurrences/Status", "occurrence");

            TempData["Success"] = "Estado atualizado.";
            return RedirectToAction("AssignedIncidents");
        }

        /// <summary>
        /// Lista as tarefas atribuídas ao funcionário logado, com suporte para filtros avançados.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AssignedIncidents(string? searchStatus, string? searchType, DateTime? startDate, DateTime? endDate)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId = int.Parse(userIdClaim!);

            var query = _context.Occurrences.Where(o => o.AssignedEmployeeId == userId).AsQueryable();

            // Lógica de filtragem dinâmica
            if (!string.IsNullOrEmpty(searchStatus)) query = query.Where(o => o.Status == searchStatus);
            if (!string.IsNullOrEmpty(searchType)) query = query.Where(o => o.OccurrenceType == searchType);
            if (startDate.HasValue) query = query.Where(o => o.ReportDate.Date >= startDate.Value.Date);
            if (endDate.HasValue) query = query.Where(o => o.ReportDate.Date <= endDate.Value.Date);

            var incidents = await query.OrderByDescending(o => o.ReportDate).ToListAsync();

            ViewBag.CurrentStatus = searchStatus;
            ViewBag.CurrentType = searchType;
            return View(incidents);
        }

        /// <summary>
        /// Método auxiliar para guardar imagens no sistema de ficheiros do servidor.
        /// Gera nomes únicos para evitar conflitos de ficheiros com o mesmo nome.
        /// </summary>
        private async Task<string?> SaveOccurrencePhotoAsync(IFormFile? photo)
        {
            if (photo == null || photo.Length == 0) return null;

            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "ocorrencias");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string nameFile = Guid.NewGuid().ToString() + "_" + Path.GetFileName(photo.FileName);
            string completePath = Path.Combine(uploadsFolder, nameFile);

            using (var fileStream = new FileStream(completePath, FileMode.Create))
            {
                await photo.CopyToAsync(fileStream);
            }

            return "/uploads/ocorrencias/" + nameFile;
        }
    }
}