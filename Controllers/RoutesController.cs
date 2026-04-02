using EcoCityWaste.Models.ViewModels;
using EcoCityWaste.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoCityWaste.Controllers
{
    [Authorize(Roles = "Admin,Funcionario")] // apenas admins e funcionarios tem acesso as rotas
    public class RoutesController : Controller
    {
        private readonly IRouteService _routeService;

        public RoutesController(IRouteService routeService)
        {
            _routeService = routeService;
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
    }
}