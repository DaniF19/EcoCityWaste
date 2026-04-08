using EcoCityWaste.Data;
using EcoCityWaste.Models.ViewModels;
using EcoCityWaste.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoCityWaste.Controllers
{
    [Authorize(Roles = "Admin,Funcionario")] // apenas admins e funcionarios tem acesso as rotas
    public class RoutesController : Controller
    {
        private readonly IRouteService _routeService;
        private readonly AppDbContext _context;

        public RoutesController(IRouteService routeService, AppDbContext context)
        {
            _routeService = routeService;
            _context = context;
        }

        // listar rotas

        public async Task<IActionResult> Index(string? statusFilter)
        {
            bool isEmployee = User.IsInRole("Funcionario");
            string? username = User.Identity?.Name;

            var routes = await _routeService.GetRoutesAsync(statusFilter, username, isEmployee);

            ViewBag.CurrentFilter = statusFilter;

            // indicadores dashboard
            ViewBag.TotalRoutes = routes.Count;
            ViewBag.Pending    = routes.Count(r => r.Status == EcoCityWaste.Models.Route.RouteStatus.Pending);
            ViewBag.InProgress = routes.Count(r => r.Status == EcoCityWaste.Models.Route.RouteStatus.InProgress);
            ViewBag.Completed  = routes.Count(r => r.Status == EcoCityWaste.Models.Route.RouteStatus.Completed);

            return View(routes);
        }

        // detalhes da rota

        public async Task<IActionResult> Details(int id)
        {
            var route = await _routeService.GetRouteWithDetailsAsync(id);
            if (route is null) return NotFound();

            if (User.IsInRole("Funcionario") &&
                route.AssignedEmployee?.Username != User.Identity?.Name)
                return Forbid();

            return View(route);
        }

        // criar rota

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Containers = await _routeService.GetActiveContainersAsync();
            return View(new RouteCreateViewModel());
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(RouteCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Containers = await _routeService.GetActiveContainersAsync();
                return View(model);
            }

            var (success, code) = await _routeService.CreateRouteAsync(
                model, User.Identity!.Name ?? "Admin");

            if (!success)
            {
                ModelState.AddModelError("ContainerIds",
                    "Um ou mais contentores seleccionados são inválidos ou estão inactivos.");
                ViewBag.Containers = await _routeService.GetActiveContainersAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = $"Rota '{code}' criada com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        // editar detalhes da rota

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var route = await _routeService.GetRouteWithDetailsAsync(id);
            if (route is null) return NotFound();

            var vm = new RouteEditViewModel
            {
                Id = route.Id,
                Name = route.Name,
                Description = route.Description,
                ContainerIds = route.RouteContainers
                    .OrderBy(rc => rc.PickupOrder)
                    .Select(rc => rc.ContainerId)
                    .ToList()
            };

            ViewBag.Containers = await _routeService.GetActiveContainersAsync();
            return View(vm);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(RouteEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Containers = await _routeService.GetActiveContainersAsync();
                return View(model);
            }

            var success = await _routeService.EditRouteAsync(model);
            if (!success)
            {
                ModelState.AddModelError("ContainerIds", "Um ou mais contentores são inválidos.");
                ViewBag.Containers = await _routeService.GetActiveContainersAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = "Rota actualizada com sucesso.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        // marcar rota como concluida

        [HttpPost]
        public async Task<IActionResult> Complete(int id)
        {
            bool isEmployee = User.IsInRole("Funcionario");
            var success = await _routeService.CompleteRouteAsync(id, User.Identity?.Name, isEmployee);

            if (!success) return isEmployee ? Forbid() : NotFound();

            TempData["SuccessMessage"] = "Rota marcada como concluída.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // atribuir rota a funcionario

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Assign(int id)
        {
            var route = await _routeService.GetRouteWithDetailsAsync(id);
            if (route is null) return NotFound();

            ViewBag.Route     = route;
            ViewBag.Employees = await _routeService.GetEmployeesAsync();

            return View(new RouteAssignViewModel
            {
                RouteId    = id,
                EmployeeId = route.AssignedEmployeeId
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Assign(RouteAssignViewModel model)
        {
            var route = await _routeService.GetRouteWithDetailsAsync(model.RouteId);
            if (route is null) return NotFound();

            var employees = await _routeService.GetEmployeesAsync();
            var employee  = employees.FirstOrDefault(e => e.Id == model.EmployeeId);

            if (employee is null)
            {
                ModelState.AddModelError("EmployeeId", "Funcionário inválido.");
                ViewBag.Route     = route;
                ViewBag.Employees = employees;
                return View(model);
            }

            await _routeService.AssignRouteAsync(model, route, employee);

            TempData["SuccessMessage"] = $"Rota atribuída a {employee.Username}.";
            return RedirectToAction(nameof(Details), new { id = route.Id });
        }

        // eliminar rota

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _routeService.DeleteRouteAsync(id);
            if (!success) return NotFound();

            TempData["SuccessMessage"] = "Rota eliminada com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        // otimizar rota de recolha

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Optimise(int id)
        {
            var (route, result) = await _routeService.GetOptimisedRouteAsync(id);
            if (route is null) return NotFound();

            ViewBag.Route = route;
            return View(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApplyOptimisation(int routeId, List<int> orderedContainerIds)
        {
            var success = await _routeService.ApplyOptimisationAsync(routeId, orderedContainerIds);
            if (!success) return NotFound();

            TempData["SuccessMessage"] = "Ordem optimizada aplicada com sucesso.";
            return RedirectToAction(nameof(Details), new { id = routeId });
        }

        // visualizar rota no mapa

        public async Task<IActionResult> Map(int id)
        {
            var route = await _routeService.GetRouteWithDetailsAsync(id);
            if (route is null) return NotFound();

            if (User.IsInRole("Funcionario") &&
                route.AssignedEmployee?.Username != User.Identity?.Name)
                return Forbid();

            return View(route);
        }

        // metodo que calcula a distancia total da rota com base na ordem dos contentores
        private async Task<double?> RecalcDistanceAsync(int routeId, List<int> orderedIds)
        {
            var containers = await _context.Contentores
                .Where(c => orderedIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id);

            double total = 0;

            // percorrer a lista de contentores na ordem definida pelo user
            // calcula distancia entre pares consecutivos
            for (int i = 0; i < orderedIds.Count - 1; i++)
            {
                // tenta obter os dois contentores consecutivos
                if (containers.TryGetValue(orderedIds[i], out var a) &&
                    containers.TryGetValue(orderedIds[i + 1], out var b))
                {
                    // soma a distancia entre os dois pontos
                    total += Haversine(a.Latitude, a.Longitude, b.Latitude, b.Longitude);
                }
            }
            // arredonda a distancia total 2 casas dec
            return Math.Round(total, 2);
        }

        // metodo que calcula a distancia entre dois pontos lat/long
        private static double Haversine(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0; // raio da terra em km

            // diferencas de lat/long
            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLon = (lon2 - lon1) * Math.PI / 180;

            // formula de haversine
            // 'a' representa a componente intermedia do calculo 
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
                     * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            // retorna distancia entre os dois pontos
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }




        // Vista da simulação
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