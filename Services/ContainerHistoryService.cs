using EcoCityWaste.Data;
using EcoCityWaste.Models;
using Microsoft.EntityFrameworkCore;

namespace EcoCityWaste.Services
{
    public class ContainerHistoryService
    {
        private readonly AppDbContext _context;

        public ContainerHistoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddHistory(Container container, string changedBy)
        {
            var history = new ContainerStatusHistory
            {
                ContainerId = container.Id,
                Status = container.Status,
                FillLevel = container.FillLevel,
                IsActive = container.IsActive,
                ChangedAt = DateTime.Now,
                ChangedBy = changedBy ?? "Sistema"
            };

            _context.ContainerStatusHistories.Add(history);
            await _context.SaveChangesAsync();
        }
        public async Task<List<ContainerStatusHistory>> GetHistoryAsync(int containerId)
        {
            return await _context.ContainerStatusHistories
                .Where(h => h.ContainerId == containerId)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync();
        }

    }
}