using EcoCityWaste.Data;
using EcoCityWaste.Models;
using EcoCityWaste.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace EcoCityWaste.Services
{
    /// <summary>
    /// Implementação principal do serviço de gestão de rotas.
    /// Orquestra a criação, atribuição, otimização e conclusão das tarefas de recolha de resíduos.
    /// </summary>
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

        /// <summary>
        /// Recupera a lista de rotas, aplicando filtros de estado e permissões de utilizador.
        /// Garante que funcionários apenas acedem ao seu próprio plano de trabalho.
        /// </summary>
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

            // Lógica de isolamento: funcionários apenas visualizam as suas rotas atribuídas
            if (isEmployee && username is not null)
            {
                query = query.Where(r =>
                    r.AssignedEmployee != null &&
                    r.AssignedEmployee.Username == username);
            }

            return await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        }

        /// <summary>
        /// Obtém todos os detalhes de uma rota específica, carregando os objetos relacionados.
        /// </summary>
        public async Task<EcoCityWaste.Models.Route?> GetRouteWithDetailsAsync(int id) =>
            await _context.Routes
                .Include(r => r.AssignedEmployee)
                .Include(r => r.RouteContainers)
                    .ThenInclude(rc => rc.Container)
                .FirstOrDefaultAsync(r => r.Id == id);

        /// <summary>
        /// Cria uma nova rota de recolha. Valida a existência e atividade dos contentores 
        /// e gera um código de rota único de forma segura.
        /// </summary>
        public async Task<(bool Success, string Code)> CreateRouteAsync(
            RouteCreateViewModel model, string createdBy)
        {
            var containers = await _context.Contentores
                .Where(c => model.ContainerIds.Contains(c.Id) && c.IsActive)
                .ToListAsync();

            if (containers.Count != model.ContainerIds.Count)
                return (false, string.Empty);

            var route = new EcoCityWaste.Models.Route
            {
                Name = model.Name.Trim(),
                Code = await GenerateRouteCodeAsync(),
                Description = model.Description?.Trim(),
                Status = EcoCityWaste.Models.Route.RouteStatus.Pending,
                CreatedAt = DateTime.Now,
                CreatedBy = createdBy
            };

            for (int i = 0; i < model.ContainerIds.Count; i++)
            {
                route.RouteContainers.Add(new RouteContainer
                {
                    ContainerId = model.ContainerIds[i],
                    PickupOrder = i + 1 // Define a sequência inicial baseada na escolha do admin
                });
            }

            _context.Routes.Add(route);
            await _context.SaveChangesAsync();

            return (true, route.Code);
        }

        /// <summary>
        /// Atualiza os dados de uma rota existente. Limpa as associações antigas 
        /// e reconstrói a lista de paragens com os novos contentores selecionados.
        /// </summary>
        public async Task<bool> EditRouteAsync(RouteEditViewModel model)
        {
            var route = await _context.Routes
                .Include(r => r.RouteContainers)
                .FirstOrDefaultAsync(r => r.Id == model.Id);

            if (route is null) return false;

            var validIds = await _context.Contentores
                .Where(c => model.ContainerIds.Contains(c.Id) && c.IsActive)
                .Select(c => c.Id)
                .ToListAsync();

            if (validIds.Count != model.ContainerIds.Count) return false;

            route.Name = model.Name.Trim();
            route.Description = model.Description?.Trim();

            // Substituição completa das paragens
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

        /// <summary>
        /// Finaliza a rota no sistema. Este método é vital pois simula o esvaziamento real:
        /// Reinicia o nível de enchimento de todos os contentores recolhidos e gera entradas no histórico.
        /// </summary>
        public async Task<bool> CompleteRouteAsync(int id, string? username, bool isEmployee)
        {
            var route = await _context.Routes
                .Include(r => r.RouteContainers)
                    .ThenInclude(rc => rc.Container)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (route is null) return false;

            if (isEmployee)
            {
                var employee = await _context.Users.FindAsync(route.AssignedEmployeeId);
                if (employee?.Username != username) return false;
            }

            // Lógica de Negócio: Ao concluir a rota, o nível de lixo volta a zero
            foreach (var rc in route.RouteContainers.Where(rc => rc.Container is not null))
            {
                rc.Container!.FillLevel = 0;
                rc.Container.LastUpdated = DateTime.Now;
                // Regista a ação no histórico para auditoria futura
                await _historyService.AddHistory(rc.Container, username);
            }

            route.Status = EcoCityWaste.Models.Route.RouteStatus.Completed;
            route.CompletedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Efetua a atribuição de uma rota a um funcionário, atualizando o estado 
        /// e disparando uma notificação para o colaborador.
        /// </summary>
        public async Task AssignRouteAsync(RouteAssignViewModel model, EcoCityWaste.Models.Route route, User employee)
        {
            route.AssignedEmployeeId = model.EmployeeId;
            route.AssignedAt = DateTime.Now;
            route.Status = EcoCityWaste.Models.Route.RouteStatus.InProgress;

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

        /// <summary>
        /// Elimina uma rota do sistema e limpa as suas associações na tabela intermédia.
        /// </summary>
        public async Task<bool> DeleteRouteAsync(int id)
        {
            var route = await _context.Routes
                .Include(r => r.RouteContainers)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (route is null) return false;

            _context.RouteContainers.RemoveRange(route.RouteContainers);
            _context.Routes.Remove(route);

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Aplica uma nova ordem de recolha à base de dados e recalcula a distância total estimada.
        /// </summary>
        public async Task<bool> ApplyOptimisationAsync(int routeId, List<int> orderedContainerIds)
        {
            var route = await _context.Routes
                .Include(r => r.RouteContainers)
                .FirstOrDefaultAsync(r => r.Id == routeId);

            if (route is null) return false;

            foreach (var rc in route.RouteContainers)
            {
                int idx = orderedContainerIds.IndexOf(rc.ContainerId);
                rc.PickupOrder = idx >= 0 ? idx + 1 : 999;
            }

            route.EstimatedDistanceKm = await RecalcDistanceAsync(orderedContainerIds);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Orquestra a execução do algoritmo de otimização geoespacial para uma rota.
        /// </summary>
        public async Task<(EcoCityWaste.Models.Route? Route, OptimisedRouteDto? Result)> GetOptimisedRouteAsync(int id)
        {
            var route = await GetRouteWithDetailsAsync(id);
            if (route is null) return (null, null);

            var containers = route.RouteContainers
                .Select(rc => rc.Container)
                .ToList();

            var result = _optimiser.Optimise(containers!);
            return (route, result);
        }

        /// <summary> Lista de contentores aptos para serem incluídos em novos planeamentos. </summary>
        public Task<List<Container>> GetActiveContainersAsync() =>
            _context.Contentores.Where(c => c.IsActive).OrderBy(c => c.Code).ToListAsync();

        /// <summary> Lista de colaboradores operacionais para atribuição de tarefas. </summary>
        public Task<List<User>> GetEmployeesAsync() =>
            _context.Users.Where(u => u.Role == "Funcionario").OrderBy(u => u.Username).ToListAsync();

        /// <summary>
        /// Gera um código RT-XXX único, garantindo que não há colisões mesmo após eliminações de registos.
        /// </summary>
        private async Task<string> GenerateRouteCodeAsync()
        {
            int next = (await _context.Routes.MaxAsync(r => (int?)r.Id) ?? 0) + 1;
            return $"RT-{next:D3}";
        }

        /// <summary>
        /// Calcula a distância teórica da rota percorrendo a sequência de IDs de contentores.
        /// </summary>
        private async Task<double> RecalcDistanceAsync(List<int> orderedIds)
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
                    total += RouteOptimisationService.HaversineKm(
                        a.Latitude, a.Longitude,
                        b.Latitude, b.Longitude);
                }
            }
            return Math.Round(total, 2);
        }
    }
}