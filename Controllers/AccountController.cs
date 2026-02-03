using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EcoCityWaste.Models;
using BCrypt.Net;
using EcoCityWaste.Data;

namespace EcoCityWaste.Controllers
{
	public class AccountController : Controller
	{
        private readonly EmailService _emailService;
        private readonly AppDbContext _context;

        public AccountController(EmailService emailService, AppDbContext context)
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
        public async Task<IActionResult> Login(string email, string password)
        {

            // Verifica se os campos vieram vazios
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Erro = "Por favor, preencha todos os campos.";
                return View();
            }

            // Verifica se � um email v�lido
            if (!email.Contains("@") || !email.Contains("."))
            {
                ViewBag.Erro = "O email inserido n�o � v�lido.";
                return View();
            }

            // Aqui depois � a consulta a BD 
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
                    IsPersistent = true, // Mant�m o login mesmo se fechar o browser
                    ExpiresUtc = DateTime.UtcNow.AddMinutes(30)
                };

                // Login Propriamente dito
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return RedirectToAction("Index", "Home");
            }

            // Se a password ou email n�o corresponderem ao utilizador de teste
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
            // Verifica o resultado da autentica��o
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (result.Succeeded)
            {
                // O google j� cria o cookie
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

        // RECOVER PASSWORD ------------------------------------------------

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

            /*var user = FakeDatabase.Users
                .FirstOrDefault(u => u.Email == model.Email);*/

            var user = _context.Users
                .FirstOrDefault(u => u.Email == model.Email);

            if (user != null)
            {
                user.Token = Guid.NewGuid().ToString();
                user.TokenExpiry = DateTime.Now.AddMinutes(30);

                _context.SaveChanges(); // novo

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
            /*var user = FakeDatabase.Users.FirstOrDefault(u =>
                u.ResetPasswordToken == token &&
                u.ResetPasswordExpiry > DateTime.Now);*/

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

            /*var user = FakeDatabase.Users.FirstOrDefault(u =>
                u.ResetPasswordToken == model.Token &&
                u.ResetPasswordExpiry > DateTime.Now);*/

            var user = _context.Users.FirstOrDefault(u =>
                u.Token == model.Token &&
                u.TokenExpiry > DateTime.Now);

            if (user == null)
                return View("InvalidToken");

            /*user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            user.ResetPasswordToken = null;
            user.ResetPasswordExpiry = null;*/

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            user.Token = null;
            user.TokenExpiry = null;

            _context.SaveChanges();

            return RedirectToAction("Loginteste");
        }

        // login teste
        public ActionResult Loginteste()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Loginteste(LogintesteViewModel model)
        {
            /*var user = FakeDatabase.Users
                .FirstOrDefault(u => u.Email == model.Email);

            if (user == null)
            {
                ViewBag.Error = "Invalid email or password";
                return View();
            }

            bool passwordOk = BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);
            
            if (!passwordOk)
            {
                ViewBag.Error = "Invalid email or password";
                return View();
            }*/

            // teste apenas
            return Content("LOGIN SUCCESS ! Password is correct.");
        }
    
    
    }

    
    
}