using Microsoft.AspNetCore.Mvc;
using EcoCityWaste.Models;
using EcoCityWaste.Data;
using Microsoft.AspNetCore.Authorization;

namespace EcoCityWaste.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }

    }
}
