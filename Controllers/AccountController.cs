using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EcoCityWaste.Controllers
{
	public class AccountController : Controller
	{
		// GET: /Account/Login
		public IActionResult Login()
		{
            return User.Identity.IsAuthenticated ? RedirectToAction("Index", "Home") : View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {

            // Verifica se os campos vieram vazios
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Erro = "Por favor, preencha todos os campos.";
                return View();
            }

            // Verifica se é um email válido
            if (!email.Contains("@") || !email.Contains("."))
            {
                ViewBag.Erro = "O email inserido não é válido.";
                return View();
            }

            // Aqui depois é a consulta a BD 
            if (email == "admin@ecocity.com" && password == "123456")
            {
                // Criar a Identidade do Utilizador
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, email),
                    new Claim(ClaimTypes.Email, email)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true, // Mantém o login mesmo se fechar o browser
                    ExpiresUtc = DateTime.UtcNow.AddMinutes(30)
                };

                // Login Propriamente dito
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return RedirectToAction("Index", "Home");
            }

            // Se a password ou email não corresponderem ao utilizador de teste
            ViewBag.Erro = "Email ou Palavra-passe incorretos.";
            return View();
        }

        public IActionResult LoginGoogle()
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> GoogleResponse()
        {
            // Verifica o resultado da autenticação
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (result.Succeeded)
            {
                // O google já cria o cookie
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ViewBag.Erro = "Erro ao autenticar com o Google.";
                return RedirectToAction("Login");
            }
        }

        public async Task<IActionResult> Logout()
        {
            // Isto apaga o Cookie e termina a sessão
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Login", "Account");
        }

    }
}