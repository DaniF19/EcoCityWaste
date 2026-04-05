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
using Microsoft.Extensions.Configuration;

namespace EcoCityWaste.Controllers
{
    public class OccurrencesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly int _hideResolvedAfterDays;


        public OccurrencesController(AppDbContext context, IWebHostEnvironment webHostEnvironment, IConfiguration configuration)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _hideResolvedAfterDays = configuration.GetValue<int>("HideResolvedAfterDays", 30); // Valor padrão de 30 dias se não estiver configurado
        }

        // GET: Occurrences/Report
        public IActionResult Report()
        {
            // Vai buscar os dados dos contentores à base de dados
            var containers = _context.Contentores
                .Select(c => new
                {
                    c.Code,
                    c.Location,
                    c.Type,
                    c.Status,
                    c.FillLevel
                }).ToList();

            var translate = containers
                .Select(c => new
                {
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

            // Enviamos a lista para criar as <option> do HTML
            ViewBag.ContainersList = translate;

            // Enviamos em formato JSON para o JavaScript conseguir ler no browser
            ViewBag.ContainersJson = JsonSerializer.Serialize(translate);

            return View(new ReportOccurrenceViewModel());
        }

        // POST: Occurrences/Report
        [HttpPost]
        public async Task<IActionResult> Report(ReportOccurrenceViewModel model)
        {
            // Verifica se os campos obrigatórios vieram preenchidos
            if (!ModelState.IsValid)
            {
                // Limpa o form
                ModelState.Clear();

                // Coloca um aviso para o cidadão não ficar confuso com o reset
                ViewBag.Error = "Faltam preencher campos obrigatórios ou os dados são inválidos. Por favor, preencha novamente.";

                // Carrega os contentores novamente
                var containersBD = _context.Contentores
                    .Select(c => new { c.Code, c.Location, c.Type, c.Status, c.FillLevel })
                    .ToList();

                var containersTraduzidos = containersBD
                    .Select(c => new
                    {
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

                ViewBag.ContainersList = containersTraduzidos;
                ViewBag.ContainersJson = JsonSerializer.Serialize(containersTraduzidos);

                // Devolvemos um modelo vazio
                return View(new ReportOccurrenceViewModel());
            }

            try
            {
                // Ir buscar o id do utilizador autenticado
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int.TryParse(userIdString, out int userId); // Converte para int com segurança

                string photoPath = null;

                if (model.Photo != null && model.Photo.Length > 0)
                {
                    // Define a pasta onde guardar
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "ocorrencias");

                    // Cria a pasta se ela não existir
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Cria um nome único para não haver ficheiros substituídos com o mesmo nome
                    string nameFile = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.Photo.FileName);
                    string completePath = Path.Combine(uploadsFolder, nameFile);

                    // Copia o ficheiro para o servidor
                    using (var fileStream = new FileStream(completePath, FileMode.Create))
                    {
                        await model.Photo.CopyToAsync(fileStream);
                    }

                    // Guarda o caminho relativo que vai ser gravado na BD
                    photoPath = "/uploads/ocorrencias/" + nameFile;
                }

                // Mapear os dados do formulário para a entidade da Base de Dados
                var occurrence = new Occurrence
                {
                    ContainerCode = model.ContainerCode,
                    OccurrenceType = model.OccurrenceType,
                    Description = model.Description,
                    ReportDate = DateTime.Now,
                    Status = OccurrenceStatus.Pendente.ToString(), // Ocorrência guardada com estado inicial
                    UserId = userId,
                    ImagePath = photoPath
                };

                // Guardar na base de dados
                _context.Occurrences.Add(occurrence);
                await _context.SaveChangesAsync();

                // Feedback ao utilizador
                ModelState.Clear();
                ViewBag.Success = "Obrigado! A anomalia foi registada e será analisada pela nossa equipa.";

                return View();
            }
            catch (Exception)
            {
                //Mostra mensagem se a base de dados falhar
                ViewBag.Error = "Ocorreu um problema técnico ao tentar enviar o reporte. Por favor, tente novamente.";
                return View(model);
            }
        }

        public async Task<IActionResult> Status()
        {
            // Busca o utilizador logado
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var cutoffDate = DateTime.Now.AddDays(-_hideResolvedAfterDays);

            // Vai à base de dados buscar apenas as ocorrências deste cidadão
            var reports = await _context.Occurrences
                .Where(o => o.UserId == userId)
                .Where(o =>
            o.Status != OccurrenceStatus.Resolvido.ToString() &&
            o.Status != OccurrenceStatus.Rejeitado.ToString()
            ||
            o.LastUpdatedAt >= cutoffDate  // Mostra resolvidas/rejeitadas apenas dentro do período
            )
            .OrderByDescending(o => o.ReportDate)
            .ToListAsync();

            ViewBag.HideAfterDays = _hideResolvedAfterDays; // Para usar na view


            return View(reports);
        }

        [HttpGet]
        public async Task<IActionResult> Assign()
        {
            var occurrences = await _context.Occurrences
                .Where(o => o.AssignedEmployeeId == null)
                .ToListAsync();

            var employees = await _context.Users
                .Where(u => u.Role == "Funcionario")
                .ToListAsync();

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

        [HttpPost]
        public async Task<IActionResult> Assign(AssignOccurrenceViewModel model)
        {
            // Validação: impedir submit vazio
            if (model.SelectedOccurrenceId == 0 || model.SelectedEmployeeId == 0)
            {
                TempData["Error"] = "Tem de selecionar uma ocorrência e um funcionário.";
                return RedirectToAction("Assign");
            }

            var occurrence = await _context.Occurrences.FindAsync(model.SelectedOccurrenceId);

            if (occurrence == null)
            {
                TempData["Error"] = "A ocorrência selecionada não existe.";
                return RedirectToAction("Assign");
            }

            occurrence.AssignedEmployeeId = model.SelectedEmployeeId;
            occurrence.Status = OccurrenceStatus.EmAnalise.ToString();
            occurrence.AssignedAt = DateTime.Now;
            occurrence.LastUpdatedAt = DateTime.Now;

            _context.Notifications.Add(new Notification
            {
                Message = $"Foi-lhe atribuída uma ocorrência ({occurrence.OccurrenceType}).",
                UserId = model.SelectedEmployeeId,
                CreatedAt = DateTime.Now,
                IsRead = false
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Ocorrência atribuída com sucesso!";
            return RedirectToAction("Assign");
        }
        [HttpGet]
        public async Task<IActionResult> UpdateStatus(int id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized();

            int userId = int.Parse(userIdClaim);

            var employee = await _context.Users.FindAsync(userId);
            if (employee == null || employee.Role != "Funcionario")
                return Unauthorized();

            var occurrence = await _context.Occurrences.FindAsync(id);
            if (occurrence == null)
                return NotFound();

            if (occurrence.AssignedEmployeeId != employee.Id)
                return Unauthorized();

            var vm = new UpdateStatusViewModel
            {
                OccurrenceId = occurrence.Id,
                CurrentStatus = occurrence.Status,
                LastUpdatedAt = occurrence.LastUpdatedAt = DateTime.Now,
                NewStatus = Enum.Parse<OccurrenceStatus>(occurrence.Status)
            };

            return View(vm);
        }


        [HttpPost]
        public async Task<IActionResult> UpdateStatus(UpdateStatusViewModel model)
        {
            // Ignorar CurrentStatus porque não vem do form
            ModelState.Remove("CurrentStatus");

            if (!ModelState.IsValid)
            {
                var occReload = await _context.Occurrences.FindAsync(model.OccurrenceId);
                model.CurrentStatus = occReload?.Status;
                return View(model);
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized();

            int userId = int.Parse(userIdClaim);

            var employee = await _context.Users.FindAsync(userId);
            if (employee == null || employee.Role != "Funcionario")
                return Unauthorized();

            var occurrence = await _context.Occurrences.FindAsync(model.OccurrenceId);
            if (occurrence == null)
                return NotFound();

            if (occurrence.AssignedEmployeeId != employee.Id)
                return Unauthorized();

            // Atualizar o estado com o valor do enum (string)
            occurrence.Status = model.NewStatus.ToString();

            _context.Occurrences.Update(occurrence);

            _context.Notifications.Add(new Notification
            {
                Message = $"O estado da sua ocorrência foi atualizado para {occurrence.Status}.",
                UserId = occurrence.UserId,
                CreatedAt = DateTime.Now,
                IsRead = false
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Incident status updated successfully.";

            return RedirectToAction("AssignedIncidents", "Occurrences");
        }


        public async Task<IActionResult> AssignedIncidents()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized();

            int userId = int.Parse(userIdClaim);

            var employee = await _context.Users.FindAsync(userId);

            if (employee == null || employee.Role != "Funcionario")
                return Unauthorized();

            var incidents = await _context.Occurrences
                .Where(o => o.AssignedEmployeeId == employee.Id)
                .OrderByDescending(o => o.ReportDate)
                .ToListAsync();

            return View("AssignedIncidents", incidents);
        }


    }
}