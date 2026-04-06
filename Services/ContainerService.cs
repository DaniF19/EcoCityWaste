using EcoCityWaste.Data;
using EcoCityWaste.Models;
using EcoCityWaste.ViewModels;
using EcoCityWaste.Services;
using Microsoft.EntityFrameworkCore;

namespace EcoCityWaste.Services
{
    public class ContainerService
    {
        private readonly AppDbContext _context;
        private readonly GeocodingService _geo;
        private readonly ContainerHistoryService _history;

        public ContainerService(AppDbContext context, GeocodingService geo, ContainerHistoryService history)
        {
            _context = context;
            _geo = geo;
            _history = history;
        }

        // -----------------------------
        // CREATE
        // -----------------------------
        public async Task<Container> CreateAsync(ContainerRegisterViewModel model, string user)
        {
            var coords = await _geo.GetCoordinates(model.Location);

            var status = ParseStatus(model.Status);

            var container = new Container
            {
                Code = await GenerateCodeAsync(),
                Location = model.Location,
                Type = model.Type,
                Status = status,
                Latitude = coords.lat,
                Longitude = coords.lon,
                FillLevel = 0,
                InstallationDate = DateTime.Now,
                LastUpdated = DateTime.Now,
                IsActive = IsActiveFromStatus(status)
            };

            _context.Contentores.Add(container);
            await _context.SaveChangesAsync();

            await _history.AddHistory(container, user);

            return container;
        }

        // -----------------------------
        // EDIT
        // -----------------------------
        public async Task<Container?> EditAsync(ContainerEditViewModel model, string user)
        {
            var container = await _context.Contentores.FindAsync(model.Id);
            if (container == null)
                return null;

            container.Location = model.Location;
            container.Type = model.Type;
            container.Status = model.Status;
            container.IsActive = IsActiveFromStatus(model.Status);
            container.LastUpdated = DateTime.Now;

            await _context.SaveChangesAsync();
            await _history.AddHistory(container, user);

            return container;
        }

        // -----------------------------
        // UPDATE STATUS
        // -----------------------------
        public async Task<Container?> UpdateStatusAsync(int id, string newStatus, string user)
        {
            var container = await _context.Contentores.FindAsync(id);
            if (container == null)
                return null;

            if (!Enum.TryParse<Container.ContainerStatus>(newStatus, true, out var statusEnum))
                throw new Exception("Estado inválido.");

            container.Status = statusEnum;
            container.IsActive = IsActiveFromStatus(statusEnum);
            container.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _history.AddHistory(container, user);

            return container;
        }

        // -----------------------------
        // DEACTIVATE
        // -----------------------------
        public async Task<bool> DeactivateAsync(int id, string user)
        {
            var container = await _context.Contentores.FindAsync(id);
            if (container == null)
                return false;

            container.IsActive = false;
            container.LastUpdated = DateTime.Now;

            await _context.SaveChangesAsync();
            await _history.AddHistory(container, user);

            return true;
        }

        // -----------------------------
        // HELPERS
        // -----------------------------
        private bool IsActiveFromStatus(Container.ContainerStatus status)
        {
            return status != Container.ContainerStatus.Broken &&
                   status != Container.ContainerStatus.Maintenance;
        }

        private Container.ContainerStatus ParseStatus(string status)
        {
            if (!Enum.TryParse(status, true, out Container.ContainerStatus result))
                throw new Exception("Estado inválido.");

            return result;
        }

        private async Task<string> GenerateCodeAsync()
        {
            var last = await _context.Contentores
                .OrderByDescending(c => c.Id)
                .Select(c => c.Id)
                .FirstOrDefaultAsync();

            return $"CNT-{(last + 1):D3}";
        }
    }
}
