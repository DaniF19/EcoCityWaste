using EcoCityWaste.Data;
using EcoCityWaste.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace EcoCityWaste.Controllers
{
    public class OccurrencesController : Controller
    {
        private readonly AppDbContext _context;

        public OccurrencesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Occurrences/Report
        public IActionResult Report()
        {
            // Vai buscar os dados dos contentores à base de dados
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

            // Enviamos a lista para criar as <option> do HTML
            ViewBag.ContainersList = translate;

            // Enviamos em formato JSON para o JavaScript conseguir ler no browser
            ViewBag.ContainersJson = JsonSerializer.Serialize(translate);

            return View(new ReportOccurrenceViewModel());
        }

        // POST: Occurrences/Report
        [HttpPost]
        [ValidateAntiForgeryToken] // Segurança contra ataques
        public async Task<IActionResult> Report(ReportOccurrenceViewModel model)
        {
            // Verifica se os campos obrigatórios vieram preenchidos
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Ir buscar o id do utilizador autenticado
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int.TryParse(userIdString, out int userId); // Converte para int com segurança

                // Mapear os dados do formulário para a entidade da Base de Dados
                var occurrence = new Occurrence
                {
                    ContainerCode = model.ContainerCode,
                    OccurrenceType = model.OccurrenceType,
                    Description = model.Description,
                    ReportDate = DateTime.Now,
                    Status = OccurrenceStatus.Pendente.ToString(), // Ocorrência guardada com estado inicial
                    UserId = userId
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
                // Gestão de erros (DoD) - Mostra mensagem amigável se a base de dados falhar
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

            // Vai à base de dados buscar apenas as ocorrências deste cidadão
            var reports = await _context.Occurrences
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.ReportDate)
                .ToListAsync();

            return View(reports);
        }
    }
}