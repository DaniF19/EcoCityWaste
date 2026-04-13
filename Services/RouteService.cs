using EcoCityWaste.Data;
using EcoCityWaste.Models;
using EcoCityWaste.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace EcoCityWaste.Services
{
    public class RouteService : IRouteService
    {
        private readonly AppDbContext _context;
        private readonly RouteOptimisationService _optimiser;
        private readonly ContainerHistoryService _historyService;

        public RouteService(AppDbContext context, RouteOptimisationService optimiser, ContainerHistoryService historyService)
        {
            _context = context;
            _optimiser = optimiser;
            _historyService = historyService;
        }

        public async Task<List<EcoCityWaste.Models.Route>> GetRoutesAsync(
            string? statusFilter, string? username, bool isEmployee)
        {
            var query = _context.Routes
                .Include(r => r.AssignedEmployee)
                .Include(r => r.RouteContainers)
                .AsQueryable();

            if (!string.IsNullOrEmpty(statusFilter) &&
                Enum.TryParse<EcoCityWaste.Models.Route.RouteStatus>(statusFilter, out var status))
            {
                query = query.Where(r => r.Status == status);
            }

            // apenas os funcionarios visualizam as suas rotas
            if (isEmployee && username is not null)
            {
                query = query.Where(r =>
                    r.AssignedEmployee != null &&
                    r.AssignedEmployee.Username == username);
            }

            // ordenar pelos mais recentes primeiro
            return await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        }

        public async Task<EcoCityWaste.Models.Route?> GetRouteWithDetailsAsync(int id) =>
            // carrega todos os dados necessarios da rota
            await _context.Routes
                .Include(r => r.AssignedEmployee)
                .Include(r => r.RouteContainers)
                    .ThenInclude(rc => rc.Container)
                .FirstOrDefaultAsync(r => r.Id == id);

        public async Task<(bool Success, string Code)> CreateRouteAsync(
            RouteCreateViewModel model, string createdBy)
        {
            var containers = await _context.Contentores
                .Where(c => model.ContainerIds.Contains(c.Id) && c.IsActive)
                .ToListAsync();

            // validar que todos os containers existem e estao ativos
            if (containers.Count != model.ContainerIds.Count)
                return (false, string.Empty);

            // criar rota com os dados introduzidos pelo user admin
            var route = new EcoCityWaste.Models.Route
            {
                Name = model.Name.Trim(),
                Code = await GenerateRouteCodeAsync(),
                Description = model.Description?.Trim(),
                Status = EcoCityWaste.Models.Route.RouteStatus.Pending,
                CreatedAt = DateTime.Now,
                CreatedBy = createdBy
            };

            // associar os contentores à rota com a ordem definida
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

            return (true, route.Code);
        }

        public async Task<bool> EditRouteAsync(RouteEditViewModel model)
        {
            // carrega rota existente com os contentores
            var route = await _context.Routes
                .Include(r => r.RouteContainers)
                .FirstOrDefaultAsync(r => r.Id == model.Id);

            if (route is null) return false;

            var validIds = await _context.Contentores
                .Where(c => model.ContainerIds.Contains(c.Id) && c.IsActive)
                .Select(c => c.Id)
                .ToListAsync();

            if (validIds.Count != model.ContainerIds.Count) return false;

            // atualizacao dos dados
            route.Name = model.Name.Trim();
            route.Description = model.Description?.Trim();

            // substituir os contentores da rota
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
            return true;
        }

        public async Task<bool> CompleteRouteAsync(int id, string? username, bool isEmployee)
        {
            var route = await _context.Routes
                .Include(r => r.RouteContainers)
                    .ThenInclude(rc => rc.Container)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (route is null) return false;

            // funcionarios apenas podem marcar como concluido as rotas que estao associadas a si
            if (isEmployee)
            {
                var employee = await _context.Users.FindAsync(route.AssignedEmployeeId);
                if (employee?.Username != username) return false;
            }

            // atualiza todos os contentores da rota, colocando o fill level a 0 e registando no historico
            foreach (var rc in route.RouteContainers.Where(rc => rc.Container is not null))
            {
                rc.Container.FillLevel = 0;
                rc.Container.LastUpdated = DateTime.Now;
                await _historyService.AddHistory(rc.Container, username);
            }

            // atualiza estado da rota
            route.Status = EcoCityWaste.Models.Route.RouteStatus.Completed;
            route.CompletedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task AssignRouteAsync(RouteAssignViewModel model, EcoCityWaste.Models.Route route, User employee)
        {
            // atribui rota a funcionario
            route.AssignedEmployeeId = model.EmployeeId;
            route.AssignedAt = DateTime.Now;
            route.Status = EcoCityWaste.Models.Route.RouteStatus.InProgress;

            // criar notificacao para o user funcionario
            _context.Notifications.Add(new Notification
            {
                Message = $"Foi-lhe atribuída a rota {route.Code}.",
                UserId = employee.Id,
                CreatedAt = DateTime.Now,
                IsRead = false,
                LinkUrl = $"/Routes/Details/{route.Id}",
                NotificationType = "route"
            });

            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteRouteAsync(int id)
        {
            var route = await _context.Routes
                .Include(r => r.RouteContainers)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (route is null) return false;

            // remover as relacoes primeiro
            _context.RouteContainers.RemoveRange(route.RouteContainers);
            _context.Routes.Remove(route);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ApplyOptimisationAsync(int routeId, List<int> orderedContainerIds)
        {
            var route = await _context.Routes
                .Include(r => r.RouteContainers)
                .FirstOrDefaultAsync(r => r.Id == routeId);

            if (route is null) return false;

            // atualiza ordem de recolha
            foreach (var rc in route.RouteContainers)
            {
                int idx = orderedContainerIds.IndexOf(rc.ContainerId);
                rc.PickupOrder = idx >= 0 ? idx + 1 : 999;
            }

            // distancia total estimada calculada de novo
            route.EstimatedDistanceKm = await RecalcDistanceAsync(orderedContainerIds);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(EcoCityWaste.Models.Route? Route, OptimisedRouteDto? Result)> GetOptimisedRouteAsync(int id)
        {
            var route = await GetRouteWithDetailsAsync(id);
            if (route is null) return (null, null);

            // extrai contentores e aplica algoritmo de otimizacao
            var containers = route.RouteContainers
                .Select(rc => rc.Container)
                .ToList();

            var result = _optimiser.Optimise(containers);
            return (route, result);
        }

        public Task<List<Container>> GetActiveContainersAsync() =>
            _context.Contentores
                .Where(c => c.IsActive)
                .OrderBy(c => c.Code)
                .ToListAsync();

        public Task<List<User>> GetEmployeesAsync() =>
            _context.Users
                .Where(u => u.Role == "Funcionario")
                .OrderBy(u => u.Username)
                .ToListAsync();

        // helpers

        /// <summary>
        /// criar codigo de rota unico. utiliza MAX(Id) em vez de COUNT
        /// para evitar collisions depois de eliminar
        /// </summary>
        private async Task<string> GenerateRouteCodeAsync()
        {
            int next = (await _context.Routes.MaxAsync(r => (int?)r.Id) ?? 0) + 1;
            return $"RT-{next:D3}";
        }

        // metodo que calcula a distancia total da rota com base na ordem dos contentores (formula de Haversine)
        private async Task<double> RecalcDistanceAsync(List<int> orderedIds)
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
                    total += RouteOptimisationService.HaversineKm(
                        a.Latitude, a.Longitude,
                        b.Latitude, b.Longitude);
                }
            }

            return Math.Round(total, 2); // arredonda a distancia total 2 casas dec
        }
    }
}