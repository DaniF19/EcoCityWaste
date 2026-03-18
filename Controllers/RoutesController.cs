using EcoCityWaste.Data;
using EcoCityWaste.Models;
using EcoCityWaste.Models.ViewModels;
using EcoCityWaste.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EcoCityWaste.Controllers
{
    [Authorize(Roles = "Admin,Funcionario")]
    public class RoutesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly RouteOptimisationService _optimiser;

        public RoutesController(AppDbContext context, RouteOptimisationService optimiser)
        {
            _context = context;
            _optimiser = optimiser;
        }

        // ── Index ─────────────────────────────────────────────────────────────

        public async Task<IActionResult> Index(string? statusFilter)
        {
            var query = _context.Routes
                .Include(r => r.AssignedEmployee)
                .Include(r => r.RouteContainers)
                .AsQueryable();

            if (!string.IsNullOrEmpty(statusFilter) &&
                Enum.TryParse<EcoCityWaste.Models.Route.RouteStatus>(statusFilter, out var statusEnum))
            {
                query = query.Where(r => r.Status == statusEnum);
            }

            // For employees: only show routes assigned to them
            if (User.IsInRole("Funcionario"))
            {
                var username = User.Identity!.Name;
                query = query.Where(r =>
                    r.AssignedEmployee != null && r.AssignedEmployee.Username == username);
            }

            ViewBag.CurrentFilter = statusFilter;
            ViewBag.TotalRoutes = await _context.Routes.CountAsync();
            ViewBag.Pending = await _context.Routes.CountAsync(r => r.Status == EcoCityWaste.Models.Route.RouteStatus.Pending);
            ViewBag.InProgress = await _context.Routes.CountAsync(r => r.Status == EcoCityWaste.Models.Route.RouteStatus.InProgress);
            ViewBag.Completed = await _context.Routes.CountAsync(r => r.Status == EcoCityWaste.Models.Route.RouteStatus.Completed);

            return View(await query.OrderByDescending(r => r.CreatedAt).ToListAsync());
        }

        // ── Details ───────────────────────────────────────────────────────────

        public async Task<IActionResult> Details(int id)
        {
            var route = await _context.Routes
                .Include(r => r.AssignedEmployee)
                .Include(r => r.RouteContainers)
                    .ThenInclude(rc => rc.Container)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (route == null) return NotFound();

            if (User.IsInRole("Funcionario"))
            {
                var username = User.Identity!.Name;
                if (route.AssignedEmployee?.Username != username)
                    return Forbid();
            }

            return View(route);
        }

        // ── Create ────────────────────────────────────────────────────────────

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Containers = await ActiveContainersAsync();
            return View(new RouteCreateViewModel());
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RouteCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Containers = await ActiveContainersAsync();
                return View(model);
            }

            var containers = await _context.Contentores
                .Where(c => model.ContainerIds.Contains(c.Id) && c.IsActive)
                .ToListAsync();

            if (containers.Count != model.ContainerIds.Count)
            {
                ModelState.AddModelError("ContainerIds",
                    "Um ou mais contentores seleccionados são inválidos ou estão inactivos.");
                ViewBag.Containers = await ActiveContainersAsync();
                return View(model);
            }

            // Explicitly using the Model type to avoid compiler confusion
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

        // ── Edit ──────────────────────────────────────────────────────────────

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

        [HttpPost]
        [Authorize(Roles = "Admin")]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RouteEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Containers = await ActiveContainersAsync();
                return View(model);
            }

            var route = await _context.Routes
                .Include(r => r.RouteContainers)
                .FirstOrDefaultAsync(r => r.Id == model.Id);

            if (route == null) return NotFound();

            var validIds = await _context.Contentores
                .Where(c => model.ContainerIds.Contains(c.Id) && c.IsActive)
                .Select(c => c.Id)
                .ToListAsync();

            if (validIds.Count != model.ContainerIds.Count)
            {
                ModelState.AddModelError("ContainerIds", "Um ou mais contentores são inválidos.");
                ViewBag.Containers = await ActiveContainersAsync();
                return View(model);
            }

            route.Name = model.Name.Trim();
            route.Description = model.Description?.Trim();

            _context.RouteContainers.RemoveRange(route.RouteContainers);
            for (int i = 0; i < model.ContainerIds.Count; i++)
            {
                route.RouteContainers.Add(new RouteContainer
                {
                    ContainerId = model.ContainerIds[i],
                    PickupOrder = i + 1
                });
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Rota actualizada com sucesso.";
            return RedirectToAction(nameof(Details), new { id = route.Id });
        }

        // ── Complete route ─────────────────────────────────────────────────────

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            var route = await _context.Routes.FindAsync(id);
            if (route == null) return NotFound();

            if (User.IsInRole("Funcionario"))
            {
                var username = User.Identity!.Name;
                var employee = await _context.Users.FindAsync(route.AssignedEmployeeId);
                if (employee?.Username != username) return Forbid();
            }

            route.Status = EcoCityWaste.Models.Route.RouteStatus.Completed;
            route.CompletedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Rota marcada como concluída.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private string GenerateRouteCode()
        {
            int count = _context.Routes.Count() + 1;
            return $"RT-{count:D3}";
        }

        private Task<List<Container>> ActiveContainersAsync() =>
            _context.Contentores
                .Where(c => c.IsActive)
                .OrderBy(c => c.Code)
                .ToListAsync();

        private Task<List<User>> EmployeesAsync() =>
            _context.Users
                .Where(u => u.Role == "Funcionario")
                .OrderBy(u => u.Username)
                .ToListAsync();

        // ── Delete ────────────────────────────────────────────────────────────

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var route = await _context.Routes
                .Include(r => r.RouteContainers)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (route == null) return NotFound();

            // Remove as associações primeiro para evitar erro de FK
            _context.RouteContainers.RemoveRange(route.RouteContainers);
            _context.Routes.Remove(route);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Rota eliminada com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        // ── Optimise (RF G02) ─────────────────────────────────────────────────

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

            // Chama o serviço de lógica de otimização
            var result = _optimiser.Optimise(containers);

            ViewBag.Route = route;
            return View(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyOptimisation(int routeId, List<int> orderedContainerIds)
        {
            var route = await _context.Routes
                .Include(r => r.RouteContainers)
                .FirstOrDefaultAsync(r => r.Id == routeId);

            if (route == null) return NotFound();

            // Atualiza a ordem de recolha baseada no resultado do otimizador
            foreach (var rc in route.RouteContainers)
            {
                int idx = orderedContainerIds.IndexOf(rc.ContainerId);
                rc.PickupOrder = idx >= 0 ? idx + 1 : 999;
            }

            // Recalcula a distância estimada total
            route.EstimatedDistanceKm = await RecalcDistanceAsync(routeId, orderedContainerIds);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Ordem optimizada aplicada com sucesso.";
            return RedirectToAction(nameof(Details), new { id = routeId });
        }

        // ── Assign (Atribuição) ────────────────────────────────────────────────

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Assign(int id)
        {
            var route = await _context.Routes.FindAsync(id);
            if (route == null) return NotFound();

            ViewBag.Route = route;
            ViewBag.Employees = await EmployeesAsync();

            return View(new RouteAssignViewModel { RouteId = id });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(RouteAssignViewModel model)
        {
            var route = await _context.Routes.FindAsync(model.RouteId);
            if (route == null) return NotFound();

            var employee = await _context.Users.FindAsync(model.EmployeeId);
            if (employee == null || employee.Role != "Funcionario")
            {
                ModelState.AddModelError("EmployeeId", "Funcionário inválido.");
                ViewBag.Route = route;
                ViewBag.Employees = await EmployeesAsync();
                return View(model);
            }

            route.AssignedEmployeeId = model.EmployeeId;
            route.AssignedAt = DateTime.Now;
            route.Status = EcoCityWaste.Models.Route.RouteStatus.InProgress;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Rota atribuída a {employee.Username}.";
            return RedirectToAction(nameof(Details), new { id = route.Id });
        }

        // ── Map view (Visualização no Mapa) ───────────────────────────────────

        public async Task<IActionResult> Map(int id)
        {
            var route = await _context.Routes
                .Include(r => r.RouteContainers)
                    .ThenInclude(rc => rc.Container)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (route == null) return NotFound();

            // Segurança: Funcionários só veem o mapa das suas próprias rotas
            if (User.IsInRole("Funcionario"))
            {
                var username = User.Identity!.Name;
                var emp = await _context.Users.FindAsync(route.AssignedEmployeeId);
                if (emp?.Username != username) return Forbid();
            }

            return View(route);
        }

        // ── Helpers Adicionais ────────────────────────────────────────────────

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

    }
}
