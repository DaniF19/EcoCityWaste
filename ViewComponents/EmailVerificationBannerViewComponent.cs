using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Linq;
using EcoCityWaste.Data;

namespace EcoCityWaste.ViewComponents
{
    public class EmailVerificationBannerViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public EmailVerificationBannerViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!User.Identity.IsAuthenticated)
                return Content(string.Empty);

            var claimsPrincipal = User as ClaimsPrincipal;
            var email = claimsPrincipal?.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
                return Content(string.Empty);

            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null || user.EmailVerified)
                return Content(string.Empty);

            // return view with banner
            return View("Default", user.Email);
        }
    }
}
