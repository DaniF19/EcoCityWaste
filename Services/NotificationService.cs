using EcoCityWaste.Data;
using EcoCityWaste.Models;

namespace EcoCityWaste.Services
{
    public class NotificationService
    {
        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateCriticalLevelNotification(Container container)
        {
            // to avoid duplicate notifications
            bool exists = _context.Notifications.Any(n =>
                n.ContainerId == container.Id);

            if (exists)
                return;

            var notification = new Notification
            {
                ContainerId = container.Id,
                Message = $"O Contentor {container.Code} Atingiu um Nível Crítico ({container.FillLevel}%)"
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }
    }
}