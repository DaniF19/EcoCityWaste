using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace EcoCityWaste.Controllers
{
    [Authorize(Roles = "Funcionario")]
    public class FuncionarioController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }

    }
}