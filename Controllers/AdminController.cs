using Microsoft.AspNetCore.Mvc;
using EcoCityWaste.Models;
using EcoCityWaste.Data;
using Microsoft.AspNetCore.Authorization;

namespace EcoCityWaste.Controllers
{
    /// <summary>
    /// Controlador reservado exclusivamente para os Administradores.
    /// A anotação de autorização garante que ninguém com outro perfil consegue aceder a estas rotas.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        /// <summary>
        /// Carrega a página principal do painel de administração, 
        /// onde o Admin tem acesso a métricas, gestão de utilizadores e planeamento de rotas.
        /// </summary>
        /// <returns>A vista do Dashboard do Admin.</returns>
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}