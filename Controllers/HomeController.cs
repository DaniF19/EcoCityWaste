using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using EcoCityWaste.ViewModels;

namespace EcoCityWaste.Controllers
{
    /// <summary>
    /// Controlador responsável pela página inicial e páginas consequentes.
    /// Gere também o redirecionamento inteligente de utilizadores já autenticados.
    /// </summary>
    public class HomeController : Controller
    {
        /// <summary>
        /// Carrega a página inicial do site. 
        /// Se o utilizador já tiver uma sessão ativa, verifica o seu perfil (Role) 
        /// e redireciona-o automaticamente para o Dashboard correspondente.
        /// </summary>
        /// <returns>A vista da Home Page ou um redirecionamento para um Dashboard.</returns>
        public IActionResult Index()
        {
            // Verifica se o utilizador tem a sessão iniciada
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                // Redireciona consoante a Role para poupar cliques ao utilizador
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

        /// <summary>
        /// Apresenta a página de Política de Privacidade do sistema.
        /// </summary>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Página genérica para exibição de erros críticos no sistema.
        /// </summary>
        /// <returns>A vista de erro com o ID do pedido para fins de suporte técnico.</returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}