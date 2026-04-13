using EcoCityWaste.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace EcoCityWaste.Controllers
{
    /// <summary>
    /// Controlador responsável por gerir o centro de notificações da plataforma.
    /// Permite que Administradores, Funcionários e Cidadãos visualizem e interajam com os seus alertas.
    /// </summary>
    [Authorize(Roles = "Admin,Funcionario,Cidadao")]
    public class NotificationsController : Controller
    {
        private readonly AppDbContext _context;

        public NotificationsController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lista todas as notificações do utilizador que tem a sessão iniciada.
        /// Utiliza o ClaimTypes.NameIdentifier para filtrar apenas os alertas pertencentes a este ID.
        /// </summary>
        /// <returns>A vista com a lista de notificações ordenada pelas mais recentes.</returns>
        public async Task<IActionResult> Index()
        {
            // Extrai o ID do utilizador a partir dos dados da sessão (Claims)
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(notifications);
        }

        /// <summary>
        /// Marca uma notificação específica como lida.
        /// </summary>
        /// <param name="id">O identificador da notificação a atualizar.</param>
        /// <returns>Redireciona para a página principal de notificações.</returns>
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);

            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Remove permanentemente todas as notificações da caixa de entrada do utilizador atual.
        /// </summary>
        /// <returns>Redireciona para a lista de notificações vazia.</returns>
        [HttpPost]
        public async Task<IActionResult> ClearAll()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .ToListAsync();

            _context.Notifications.RemoveRange(notifications);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Marca todas as notificações não lidas do utilizador como lidas de uma só vez.
        /// Útil para limpar os indicadores de novos alertas rapidamente.
        /// </summary>
        /// <returns>Redireciona para a página de notificações atualizada.</returns>
        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }
            var notifications = await _context.Notifications.ToListAsync();

            _context.Notifications.RemoveRange(notifications);

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}