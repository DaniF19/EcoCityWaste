using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace EcoCityWaste.Controllers
{
    /// <summary>
    /// Controlador exclusivo para os funcionários da autarquia.
    /// A anotação [Authorize] garante que cidadãos ou utilizadores não registados não conseguem aceder a estas páginas.
    /// </summary>
    [Authorize(Roles = "Funcionario")]
    public class FuncionarioController : Controller
    {
        /// <summary>
        /// Carrega o ecrã principal do funcionário.
        /// É a partir daqui que o trabalhador consegue aceder às rotas de recolha ou ocorrências que lhe foram atribuídas.
        /// </summary>
        /// <returns>A vista do Dashboard do Funcionário.</returns>
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}