using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using EcoCityWaste.Models;

namespace EcoCityWaste.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        // Verifica se o utilizador tem a sessão iniciada
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            // Redireciona consoante a Role
            if (User.IsInRole("Cidadao"))
            {
                return RedirectToAction("Dashboard", "Citizen");
            }
            else if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Dashboard", "Admin");
            }
            else if (User.IsInRole("Funcionario"))
            {
                return RedirectToAction("Dashboard", "Funcionario");
            }
        }

        // Se não estiver logado, mostra a Home Page normal
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
    
}
