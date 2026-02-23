using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcoCityWaste.Data;
using EcoCityWaste.Models;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EcoCityWaste.Controllers
{
    [Authorize(Roles = "Admin,Funcionario")]
    public class ContainersController : Controller
    {
        private readonly AppDbContext _context;

        public ContainersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Containers
        public async Task<IActionResult> Index(string searchString, string statusFilter, string sortOrder)
        {
            try
            {
                // Estatísticas para o dashboard
                ViewBag.TotalContainers = await _context.Contentores.CountAsync();
                ViewBag.TotalCheios = await _context.Contentores.CountAsync(c => c.FillLevel >= 90);
                ViewBag.TotalAvariados = await _context.Contentores.CountAsync(c => c.Status == "Avariado");
                ViewBag.TotalAtivos = await _context.Contentores.CountAsync(c => c.IsActive);

                // Manter os valores para a View
                ViewBag.CurrentSearch = searchString;
                ViewBag.CurrentStatus = statusFilter;

                // Parâmetros de ordenação
                ViewBag.CodeSortParam = String.IsNullOrEmpty(sortOrder) ? "code_desc" : "";
                ViewBag.LevelSortParam = sortOrder == "Level" ? "level_desc" : "Level";
                ViewBag.StatusSortParam = sortOrder == "Status" ? "status_desc" : "Status";

                var containers = _context.Contentores.AsQueryable();

                // Filtro de Pesquisa por Texto
                if (!String.IsNullOrEmpty(searchString))
                {
                    containers = containers.Where(c => c.Code.Contains(searchString) || c.Location.Contains(searchString));
                }

                // Filtros
                if (!String.IsNullOrEmpty(statusFilter))
                {
                    containers = statusFilter switch
                    {
                        // Filtros de Atividade
                        "Ativos" => containers.Where(c => c.IsActive),
                        "Inativos" => containers.Where(c => !c.IsActive),

                        // Filtros de Nível
                        "Critico" or "Cheio" => containers.Where(c => c.FillLevel >= 90),
                        "Medio" => containers.Where(c => c.FillLevel >= 50 && c.FillLevel < 90),
                        "Baixo" => containers.Where(c => c.FillLevel < 50),

                        // Filtros de Tipo de Residuo
                        "Plástico" or "Papel" or "Vidro" or "Indiferenciado" => containers.Where(c => c.Type == statusFilter),

                        // Filtros Físicos
                        _ => containers.Where(c => c.Status == statusFilter)
                    };
                }

                // Ordenação 
                switch (sortOrder)
                {
                    case "code_desc": containers = containers.OrderByDescending(c => c.Code); break;
                    case "Level": containers = containers.OrderBy(c => c.FillLevel); break;
                    case "level_desc": containers = containers.OrderByDescending(c => c.FillLevel); break;
                    case "Status": containers = containers.OrderBy(c => c.Status); break;
                    case "status_desc": containers = containers.OrderByDescending(c => c.Status); break;
                    default: containers = containers.OrderBy(c => c.Code); break;
                }

                return View(await containers.ToListAsync());
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Ocorreu um problema ao tentar aceder à base de dados.";
                return View(new List<EcoCityWaste.Models.Container>());
            }
        }
    
        // GET: /Container/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Container/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(ContainerRegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var container = new Container
                {
                    Code = GenerateContainerCode(),
                    Location = model.Location,
                    Type = model.Type,
                    Status = model.Status,
                    Latitude = 0,
                    Longitude = 0,
                    FillLevel = 0,
                    InstallationDate = DateTime.Now,
                    LastUpdated = DateTime.Now,
                    IsActive = true
                };

                _context.Contentores.Add(container);
                await _context.SaveChangesAsync();

                ViewBag.Success = "Contentor registado com sucesso!";
                ModelState.Clear();

                return View();
            }
            catch
            {
                ViewBag.Error = "Erro ao registar o contentor.";
                return View(model);
            }
        }

        public async Task<IActionResult> List()
        {
            var containers = await _context.Contentores
                                           //.Where(c => c.IsActive) to show just the active ones
                                           .ToListAsync();

            return View(containers);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var container = await _context.Contentores.FindAsync(id);

            if (container == null)
                return NotFound();

            return View(container);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ContainerEditViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var container = await _context.Contentores.FindAsync(model.Id);

                if (container == null)
                    return NotFound();

                container.Location = model.Location;
                container.Type = model.Type;
                container.Status = model.Status;
                /*container.Latitude = model.Latitude;
                container.Longitude = model.Longitude;
                container.FillLevel = model.FillLevel;
                container.IsActive = model.IsActive;*/
                container.LastUpdated = DateTime.Now;

                await _context.SaveChangesAsync();

                return RedirectToAction("List");
            }
            catch
            {
                ViewBag.Error = "Erro ao atualizar o contentor.";
                return View(model);
            }
        }

        // Desativar contentor
        public async Task<IActionResult> Deactivate(int id)
        {
            var container = await _context.Contentores.FindAsync(id);

            if (container == null)
                return NotFound();

            container.IsActive = false;
            container.LastUpdated = DateTime.Now;

            await _context.SaveChangesAsync();

            return RedirectToAction("List");
        }

        // Container code generator
        private string GenerateContainerCode()
        {
            var count = _context.Contentores.Count() + 1;
            return $"CNT-{count:D3}";
        }

    }
}
