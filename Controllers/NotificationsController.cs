using EcoCityWaste.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace EcoCityWaste.Controllers
{
    [Authorize(Roles = "Admin,Funcionario")]
    public class NotificationsController : Controller
    {
        private readonly AppDbContext _context;

        public NotificationsController(AppDbContext context)
        {
            _context = context;
        }
        /*
        public async Task<IActionResult> Index()
        {
            var notifications = await _context.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(notifications);
        }*/

        public async Task<IActionResult> Index()
        {
            var username = User.Identity!.Name;

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            var notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(notifications);
        }



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

        [HttpPost]
        public async Task<IActionResult> ClearAll()
        {
            var notifications = await _context.Notifications.ToListAsync();

            _context.Notifications.RemoveRange(notifications);

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}