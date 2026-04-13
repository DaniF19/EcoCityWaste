using EcoCityWaste.Data;
using EcoCityWaste.Models;

namespace EcoCityWaste.Services
{
    /// <summary>
    /// Serviço responsável pela gestão e distribuição de alertas dentro da plataforma.
    /// Permite a criação de notificações automáticas baseadas em eventos do sistema ou ações de utilizadores.
    /// </summary>
    public class NotificationService
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Injeta o contexto da base de dados para persistência das notificações.
        /// </summary>
        public NotificationService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gera alertas automáticos para todos os Administradores quando um contentor atinge um nível de enchimento crítico.
        /// Possui uma verificação para evitar a duplicação de notificações não lidas sobre o mesmo contentor.
        /// </summary>
        /// <param name="container">O objeto do contentor que disparou o alerta de nível.</param>
        /// <returns>Uma Task assíncrona que representa o processo de criação dos alertas.</returns>
        public async Task CreateCriticalLevelNotification(Container container)
        {
            // Verifica se já existe uma notificação pendente (não lida) para este contentor, para não inundar o Admin com alertas iguais
            bool exists = _context.Notifications.Any(n => n.ContainerId == container.Id && !n.IsRead);
            if (exists) return;

            // Obtém a lista de todos os administradores registados no sistema
            var admins = _context.Users.Where(u => u.Role == "Admin").ToList();

            foreach (var admin in admins)
            {
                var notification = new Notification
                {
                    ContainerId = container.Id,
                    Message = $"O Contentor {container.Code} Atingiu um Nível Crítico ({container.FillLevel}%)",
                    UserId = admin.Id,
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    LinkUrl = $"/Containers/Details/{container.Id}",
                    NotificationType = "container"
                };
                
                _context.Notifications.Add(notification);
            }
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Método genérico e versátil para enviar notificações personalizadas a qualquer utilizador do sistema.
        /// Pode ser usado para informar cidadãos sobre o estado de ocorrências ou funcionários sobre novas rotas.
        /// </summary>
        /// <param name="message">O texto informativo da notificação.</param>
        /// <param name="userId">O identificador do utilizador destinatário.</param>
        /// <param name="linkUrl">URL opcional para onde o utilizador será redirecionado ao clicar na notificação.</param>
        /// <param name="notificationType">Categoria da notificação para fins de estilização.</param>
        /// <returns>Uma Task assíncrona.</returns>
        public async Task CreateNotificationAsync(string message, int userId, string? linkUrl = null, string? notificationType = null)
        {
            var notification = new Notification
            {
                Message = message,
                UserId = userId,
                CreatedAt = DateTime.Now,
                IsRead = false,
                LinkUrl = linkUrl,
                NotificationType = notificationType
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }
    }
}