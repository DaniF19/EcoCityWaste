using EcoCityWaste.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EcoCityWaste.Hubs
{
    /// <summary>
    /// Hub do SignalR responsável pela gestão da simulação de rotas em tempo real.
    /// Permite uma comunicação bidirecional entre o servidor e os clientes conectados.
    /// </summary>
    public class SimulationHub : Hub
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Injeta o contexto da base de dados para aceder aos dados das rotas e contentores durante a simulação.
        /// </summary>
        public SimulationHub(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Inicia o processo de simulação para uma rota específica.
        /// Carrega os dados geográficos e de enchimento dos contentores e envia-os para o cliente.
        /// </summary>
        /// <param name="routeId">O identificador da rota que se pretende simular.</param>
        /// <returns>Uma Task assíncrona que notifica o cliente quando os dados estão prontos.</returns>
        public async Task StartSimulation(int routeId)
        {
            // Procura a rota e inclui os detalhes dos contentores associados
            var route = await _context.Routes
                .Include(r => r.RouteContainers)
                    .ThenInclude(rc => rc.Container)
                .FirstOrDefaultAsync(r => r.Id == routeId);

            if (route == null) return;

            // Prepara um objeto anónimo simplificado com apenas os dados necessários para o mapa
            var containers = route.RouteContainers
                .OrderBy(rc => rc.PickupOrder)
                .Select(rc => new {
                    id = rc.Container.Id,
                    code = rc.Container.Code,
                    lat = rc.Container.Latitude,
                    lng = rc.Container.Longitude,
                    fillLevel = rc.Container.FillLevel,
                    order = rc.PickupOrder
                }).ToList();

            // Envia a lista de contentores exclusivamente para o cliente que iniciou a simulação.
            // O método "RouteLoaded" deve estar definido no JavaScript do lado do cliente.
            await Clients.Caller.SendAsync("RouteLoaded", containers);
        }
    }
}