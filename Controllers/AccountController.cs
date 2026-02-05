using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EcoCityWaste.Models;
using BCrypt.Net;
using EcoCityWaste.Data;
using EcoCityWaste.Services;

namespace EcoCityWaste.Controllers
{
	public class AccountController : Controller
	{
        private readonly IEmailService _emailService;
        private readonly AppDbContext _context;

        public AccountController(IEmailService emailService, AppDbContext context)
        {
            _emailService = emailService;
            _context = context;
        }

		// GET: /Account/Login
		public IActionResult Login()
		{
            return User.Identity.IsAuthenticated ? RedirectToAction("Index", "Home") : View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Procura user pelo email na BD
            var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);

            // Verifica se o user existe e se a password corresponde
            if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true, // Mantem o login mesmo se fechar o browser
                    ExpiresUtc = DateTime.UtcNow.AddMinutes(30)
                };

                // Login Propriamente dito
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return RedirectToAction("Index", "Home");
            }

            // Se a password ou email não corresponderem ao utilizador
            ViewBag.Erro = "Email ou Palavra-passe incorretos.";
            return View(model);
        }

        public IActionResult LoginGoogle()
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> GoogleResponse()
        {
            // Verifica o resultado da autenticacao 
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (result.Succeeded)
            {
                // Extrai o email e o nome vindos do Google através das Claims
                var email = result.Principal.FindFirstValue(ClaimTypes.Email);
                var name = result.Principal.FindFirstValue(ClaimTypes.Name);

                // Verifica se o utilizador já existe na tabela Users
                var user = _context.Users.FirstOrDefault(u => u.Email == email);

                if (user == null)
                {
                    // Se não existir, cria um novo registo na base de dados
                    user = new User
                    {
                        Email = email,
                        Username = name, // Usa o nome do Google como username
                        PasswordHash = "GOOGLE_AUTH", // Identificador para utilizadores sem password local
                        Token = null,
                        TokenExpiry = null
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }
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
            // Isto apaga o Cookie e termina a sess�o
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Login", "Account");
        }

        // Recover Password Functionality

        public ActionResult ForgotPassword()
        {
            return View();
        }

        // POST
        [HttpPost]
        public ActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = _context.Users
                .FirstOrDefault(u => u.Email == model.Email);

            if (user != null)
            {
                user.Token = Guid.NewGuid().ToString();
                user.TokenExpiry = DateTime.Now.AddMinutes(30);

                _context.SaveChanges();

                var resetLink = Url.Action(
                    "ResetPassword",
                    "Account",
                    new { token = user.Token },
                    Request.Scheme
                );

                string body = $@"
                    <h2>Password Reset</h2>
                    <p>Click the link below to reset your password:</p>
                    <p><a href='{resetLink}'>Reset Password</a></p>
                    <p>This link expires in 30 minutes.</p>
                ";

                _emailService.SendEmail(
                    user.Email,
                    "EcoCityWaste - Password Reset",
                    body
                );
            }
            ViewBag.ShowModal = true; // flag para abrir o modal do pop up
            return View(model);
        }

        // GET
        public ActionResult ResetPassword(string token)
        {
            var user = _context.Users.FirstOrDefault(u =>
                u.Token == token &&
                u.TokenExpiry > DateTime.Now);

            if (user == null)
                return View("InvalidToken");

            return View(new ResetPasswordViewModel { Token = token });
        }

        // POST
        [HttpPost]
        public ActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = _context.Users.FirstOrDefault(u =>
                u.Token == model.Token &&
                u.TokenExpiry > DateTime.Now);

            if (user == null)
                return View("InvalidToken");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            user.Token = null;
            user.TokenExpiry = null;

            _context.SaveChanges();

            return RedirectToAction("Login");
        }

    }

}