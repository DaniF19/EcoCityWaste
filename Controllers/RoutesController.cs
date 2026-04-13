using EcoCityWaste.Data;
using EcoCityWaste.Services;
using EcoCityWaste.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EcoCityWaste.Controllers
{
    /// <summary>
    /// Controlador responsável pela gestão de rotas de recolha de resíduos.
    /// Permite o planeamento, otimização, atribuição e monitorização das rotas no terreno.
    /// </summary>
    [Authorize(Roles = "Admin,Funcionario")]
    public class RoutesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly RouteOptimisationService _optimiser;
        private readonly ContainerHistoryService _historyService;

        public RoutesController(AppDbContext context, RouteOptimisationService optimiser, ContainerHistoryService historyService)
        {
            _context = context;
            _optimiser = optimiser;
            _historyService = historyService;
        }

        /// <summary>
        /// Lista as rotas disponíveis. Se o utilizador for um funcionário, 
        /// apenas vê as rotas que lhe foram especificamente atribuídas.
        /// </summary>
        /// <param name="statusFilter">Filtro opcional por estado (Pendente, Em Curso, Concluída).</param>
        public async Task<IActionResult> Index(string? statusFilter)
        {
            var query = _context.Routes
                .Include(r => r.AssignedEmployee)
                .Include(r => r.RouteContainers)
                .AsQueryable();

            // filtrar por estado
            if (!string.IsNullOrEmpty(statusFilter) &&
                Enum.TryParse<EcoCityWaste.Models.Route.RouteStatus>(statusFilter, out var statusEnum))
            {
                query = query.Where(r => r.Status == statusEnum);
            }

            // se for funcionario, so ve as suas rotas
            if (User.IsInRole("Funcionario"))
            {
                var username = User.Identity!.Name;
                query = query.Where(r =>
                    r.AssignedEmployee != null && r.AssignedEmployee.Username == username);
            }

            // dados para o dashboard
            ViewBag.CurrentFilter = statusFilter;
            ViewBag.TotalRoutes = await _context.Routes.CountAsync();
            ViewBag.Pending = await _context.Routes.CountAsync(r => r.Status == EcoCityWaste.Models.Route.RouteStatus.Pending);
            ViewBag.InProgress = await _context.Routes.CountAsync(r => r.Status == EcoCityWaste.Models.Route.RouteStatus.InProgress);
            ViewBag.Completed = await _context.Routes.CountAsync(r => r.Status == EcoCityWaste.Models.Route.RouteStatus.Completed);

            // Indicadores rápidos para os cartões do dashboard de rotas
            ViewBag.TotalRoutes = routes.Count;
            ViewBag.Pending = routes.Count(r => r.Status == EcoCityWaste.Models.Route.RouteStatus.Pending);
            ViewBag.InProgress = routes.Count(r => r.Status == EcoCityWaste.Models.Route.RouteStatus.InProgress);
            ViewBag.Completed = routes.Count(r => r.Status == EcoCityWaste.Models.Route.RouteStatus.Completed);

            return View(routes);
        }

        /// <summary>
        /// Apresenta os detalhes de uma rota, incluindo a lista ordenada de contentores a recolher.
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            var route = await _context.Routes
                .Include(r => r.AssignedEmployee)
                .Include(r => r.RouteContainers)
                    .ThenInclude(rc => rc.Container)
                .FirstOrDefaultAsync(r => r.Id == id);

            // Proteção de privacidade: um funcionário não pode visualizar as rotas dos colegas
            if (User.IsInRole("Funcionario") &&
                route.AssignedEmployee?.Username != User.Identity?.Name)
                return Forbid();

            return View(route);
        }

        /// <summary>
        /// Mostra o formulário de criação de rota, carregando apenas os contentores que estão ativos.
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Containers = await ActiveContainersAsync();
            return View(new RouteCreateViewModel());
        }

        /// <summary>
        /// Processa a criação de uma nova rota no sistema.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(RouteCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Containers = await ActiveContainersAsync();
                return View(model);
            }

            // get contentores validos e ativos
            var containers = await _context.Contentores
                .Where(c => model.ContainerIds.Contains(c.Id) && c.IsActive)
                .ToListAsync();

            if (containers.Count != model.ContainerIds.Count)
            {
                ModelState.AddModelError("ContainerIds", "Um ou mais contentores selecionados são inválidos.");
                ViewBag.Containers = await _routeService.GetActiveContainersAsync();
                return View(model);
            }

            // criar rota
            var route = new EcoCityWaste.Models.Route
            {
                Name = model.Name.Trim(),
                Code = GenerateRouteCode(),
                Description = model.Description?.Trim(),
                Status = EcoCityWaste.Models.Route.RouteStatus.Pending,
                CreatedAt = DateTime.Now,
                CreatedBy = User.Identity!.Name ?? "Admin"
            };

            for (int i = 0; i < model.ContainerIds.Count; i++)
            {
                route.RouteContainers.Add(new RouteContainer
                {
                    ContainerId = model.ContainerIds[i],
                    PickupOrder = i + 1
                });
            }

            _context.Routes.Add(route);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Rota '{route.Code}' criada com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Permite ao administrador editar as informações básicas ou a composição de contentores de uma rota.
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var route = await _context.Routes
                .Include(r => r.RouteContainers)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (route == null) return NotFound();

            var orderedIds = route.RouteContainers
                .OrderBy(rc => rc.PickupOrder)
                .Select(rc => rc.ContainerId)
                .ToList();

            var vm = new RouteEditViewModel
            {
                Id = route.Id,
                Name = route.Name,
                Description = route.Description,
                ContainerIds = orderedIds
            };

            ViewBag.Containers = await ActiveContainersAsync();
            return View(vm);
        }

        /// <summary>
        /// Processa a conclusão de uma rota, registando a data de finalização.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Complete(int id)
        {
            // incluir os contentores para depois colocar o fill level a 0
            var route = await _context.Routes
                .Include(r => r.RouteContainers)
                    .ThenInclude(rc => rc.Container)
                .FirstOrDefaultAsync(r => r.Id == id);
              
            if (route == null) return NotFound();

            // o funcionario so pode concluir a sua rota associada
            if (User.IsInRole("Funcionario"))
            {
                var username = User.Identity!.Name;
                var employee = await _context.Users.FindAsync(route.AssignedEmployeeId);
                if (employee?.Username != username) return Forbid();
            }

            foreach (var rc in route.RouteContainers)
            {
                var container = rc.Container;

                if (container != null)
                {
                    container.FillLevel = 0;
                    container.LastUpdated = DateTime.Now;

                    await _historyService.AddHistory(container, User?.Identity?.Name);
                }
            }

            route.Status = EcoCityWaste.Models.Route.RouteStatus.Completed;
            route.CompletedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Rota marcada como concluída.";
            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>
        /// Carrega o formulário para atribuir uma rota a um funcionário específico.
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Assign(int id)
        {
            var route = await _routeService.GetRouteWithDetailsAsync(id);
            if (route is null) return NotFound();

            ViewBag.Route = route;
            ViewBag.Employees = await _routeService.GetEmployeesAsync();

            return View(new RouteAssignViewModel
            {
                RouteId = id,
                EmployeeId = route.AssignedEmployeeId
            });
        }

        /// <summary>
        /// Processa a atribuição da rota e notifica o utilizador selecionado.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Assign(RouteAssignViewModel model)
        {
            var route = await _routeService.GetRouteWithDetailsAsync(model.RouteId);
            if (route is null) return NotFound();

            var employees = await _routeService.GetEmployeesAsync();
            var employee = employees.FirstOrDefault(e => e.Id == model.EmployeeId);

            if (employee is null)
            {
                ModelState.AddModelError("EmployeeId", "Funcionário inválido.");
                ViewBag.Route = route;
                ViewBag.Employees = employees;
                return View(model);
            }

            await _routeService.AssignRouteAsync(model, route, employee);

            TempData["SuccessMessage"] = $"Rota atribuída a {employee.Username}.";
            return RedirectToAction(nameof(Details), new { id = route.Id });
        }

        /// <summary>
        /// Remove uma rota do sistema. Inclui um Log de Falhas para capturar erros inesperados.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _routeService.DeleteRouteAsync(id);
                if (!success) return NotFound();

                TempData["SuccessMessage"] = "Rota eliminada com sucesso.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await _failureLogger.LogAsync(ex, nameof(RoutesController), nameof(Delete), User.Identity?.Name);
                TempData["ErrorMessage"] = "Erro ao eliminar. A falha foi registada para análise.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Aciona o algoritmo de otimização para sugerir a melhor ordem de recolha.
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Optimise(int id)
        {
            var route = await _context.Routes
                .Include(r => r.RouteContainers)
                    .ThenInclude(rc => rc.Container)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (route == null) return NotFound();

            var containers = route.RouteContainers
                .Select(rc => rc.Container)
                .ToList();

            var result = _optimiser.Optimise(containers);

            ViewBag.Route = route;
            return View(result);
        }

        /// <summary>
        /// Aplica a ordem de contentores sugerida pelo algoritmo de otimização à base de dados.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApplyOptimisation(int routeId, List<int> orderedContainerIds)
        {
            var route = await _context.Routes
                .Include(r => r.RouteContainers)
                .FirstOrDefaultAsync(r => r.Id == routeId);

            if (route == null) return NotFound();

            // atualiza ordem de recolha
            foreach (var rc in route.RouteContainers)
            {
                int idx = orderedContainerIds.IndexOf(rc.ContainerId);
                rc.PickupOrder = idx >= 0 ? idx + 1 : 999;
            }

            // distancia total estimada calculada de novo
            route.EstimatedDistanceKm = await RecalcDistanceAsync(routeId, orderedContainerIds);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Ordem otimizada aplicada com sucesso.";
            return RedirectToAction(nameof(Details), new { id = routeId });
        }

        /// <summary>
        /// Mostra a rota desenhada num mapa interativo.
        /// </summary>
        public async Task<IActionResult> Map(int id)
        {
            var route = await _context.Routes
                .Include(r => r.RouteContainers)
                    .ThenInclude(rc => rc.Container)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (route == null) return NotFound();

            // os funcionarios so visualizam as suas rotas associadas no mapa
            if (User.IsInRole("Funcionario"))
            {
                var username = User.Identity!.Name;
                var emp = await _context.Users.FindAsync(route.AssignedEmployeeId);
                if (emp?.Username != username) return Forbid();
            }

            return View(route);
        }

        /// <summary>
        /// Calcula a distância total da rota percorrendo a sequência de coordenadas.
        /// </summary>
        private async Task<double?> RecalcDistanceAsync(int routeId, List<int> orderedIds)
        {
            var containers = await _context.Contentores
                .Where(c => orderedIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id);

            double total = 0;
            for (int i = 0; i < orderedIds.Count - 1; i++)
            {
                if (containers.TryGetValue(orderedIds[i], out var a) &&
                    containers.TryGetValue(orderedIds[i + 1], out var b))
                {
                    total += Haversine(a.Latitude, a.Longitude, b.Latitude, b.Longitude);
                }
            }
            return Math.Round(total, 2);
        }

        /// <summary>
        /// Implementação da fórmula matemática para calcular a distância em linha reta
        /// entre dois pontos à superfície da Terra.
        /// </summary>
        private static double Haversine(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0; // Raio da Terra em km
            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLon = (lon2 - lon1) * Math.PI / 180;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
                     * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        /// <summary>
        /// Abre a vista de simulação em tempo real do progresso do camião na rota.
        /// </summary>
        public async Task<IActionResult> Simulate(int id)
        {
            var route = await _context.Routes
                .Include(r => r.RouteContainers)
                    .ThenInclude(rc => rc.Container)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (route == null) return NotFound();

            return View(route);
        }
    }
}
