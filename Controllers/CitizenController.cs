using Microsoft.AspNetCore.Mvc;

namespace EcoCityWaste.Controllers
{
    /// <summary>
    /// Controlador dedicado ao portal do Cidadão. 
    /// Agrupa as páginas onde os munícipes interagem com o sistema de gestão de resíduos da autarquia.
    /// </summary>
    public class CitizenController : Controller
    {
        /// <summary>
        /// Carrega a página inicial (Dashboard) do perfil do cidadão.
        /// A partir deste ecrã, o utilizador tem atalhos para reportar anomalias na via pública ou consultar o estado das suas ocorrências.
        /// </summary>
        /// <returns>A vista principal do Dashboard do Cidadão.</returns>
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}