using Microsoft.AspNetCore.Mvc;
using EcoCityWaste.Models;
using EcoCityWaste.Data;

namespace EcoCityWaste.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }

    }
}
