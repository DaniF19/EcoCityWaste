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
            // evitar duplicados
            //bool exists = _context.Notifications.Any(n =>
            //  n.ContainerId == container.Id);

            bool exists = _context.Notifications.Any(n =>
                n.ContainerId == container.Id &&
                !n.IsRead);

            if (exists)
                return;

            var admins = _context.Users
                .Where(u => u.Role == "Admin")
                .ToList();

            foreach (var admin in admins)
            {
                var notification = new Notification
                {
                    ContainerId = container.Id,
                    Message = $"O Contentor {container.Code} Atingiu um Nível Crítico ({container.FillLevel}%)",
                    UserId = admin.Id,
                    CreatedAt = DateTime.Now,
                    IsRead = false
                };

                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
        }

        // Método genérico para enviar notificações para qualquer utilizador
        public async Task CreateNotificationAsync(string message, int userId)
        {
            var notification = new Notification
            {
                Message = message,
                UserId = userId,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }
    }
}