using EcoCityWaste.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EcoCityWaste.Hubs
{
    public class SimulationHub : Hub
    {
        private readonly AppDbContext _context;

        public SimulationHub(AppDbContext context)
        {
            _context = context;
        }

        public async Task StartSimulation(int routeId)
        {
            var route = await _context.Routes
                .Include(r => r.RouteContainers)
                    .ThenInclude(rc => rc.Container)
                .FirstOrDefaultAsync(r => r.Id == routeId);

            if (route == null) return;

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

            // Envia a lista de contentores para o cliente que pediu a simulação
            await Clients.Caller.SendAsync("RouteLoaded", containers);
        }
    }
}