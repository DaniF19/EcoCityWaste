using Microsoft.AspNetCore.Mvc;

namespace EcoCityWaste.Controllers
{
    public class CitizenController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}