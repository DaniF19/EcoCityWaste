using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EcoCityWaste.Models;
using BCrypt.Net;
using EcoCityWaste.Data;
using EcoCityWaste.Services;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Security.Claims;


namespace EcoCityWaste.Controllers
{
	public class AccountController : Controller
	{
        private readonly IEmailService _emailService;
        private readonly AppDbContext _context;
        private readonly IConfiguration? _config;

        // IConfiguration is optional (defaults to null) to keep tests/simple DI usage working
        public AccountController(IEmailService emailService, AppDbContext context, IConfiguration? config = null)
        {
            _emailService = emailService;
            _context = context;
            _config = config;
        }

        // AJAX endpoint for verification (returns JSON)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyAjax([FromBody] VerifyAjaxRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Code))
                return Json(new { success = false, message = "Código inválido." });

            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, needsLogin = true, loginUrl = Url.Action("Login", "Account") });
            }

            var claimsPrincipal = User as ClaimsPrincipal;
            var email = claimsPrincipal?.FindFirstValue(ClaimTypes.Email);
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
                return Json(new { success = false, message = "Conta não encontrada." });

            if (user.EmailVerified)
                return Json(new { success = true, message = "Email já verificado.", redirectUrl = Url.Action("Index", "Home") });

            if (user.EmailVerificationBlockedUntil.HasValue && user.EmailVerificationBlockedUntil.Value > DateTime.Now)
                return Json(new { success = false, message = "Muitas tentativas falhadas. Tente novamente mais tarde." });

            if (!user.EmailVerificationExpiry.HasValue || user.EmailVerificationExpiry.Value < DateTime.Now)
                return Json(new { success = false, message = "O código expirou. Peça um novo código." });

            var providedHash = ComputeVerificationHash(request.Code.Trim());
            if (user.EmailVerificationCodeHash == providedHash)
            {
                user.EmailVerified = true;
                user.EmailVerificationCodeHash = null;
                user.EmailVerificationExpiry = null;
                user.EmailVerificationSentAt = null;
                user.EmailVerificationAttempts = 0;
                user.EmailVerificationBlockedUntil = null;
                _context.SaveChanges();

                return Json(new { success = true, message = "Email verificado com sucesso.", redirectUrl = Url.Action("Index", "Home") });
            }

            user.EmailVerificationAttempts++;
            if (user.EmailVerificationAttempts >= 5)
            {
                user.EmailVerificationBlockedUntil = DateTime.Now.AddMinutes(15);
            }
            _context.SaveChanges();

            return Json(new { success = false, message = "Código inválido." });
        }

        public class VerifyAjaxRequest { public string Code { get; set; } = string.Empty; }

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
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, user.Role)
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


        // register
        public IActionResult Register()
        {
            return User.Identity.IsAuthenticated 
                ? RedirectToAction("Index", "Home") 
                : View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Check if email already exists
            var existingUser = _context.Users
                .FirstOrDefault(u => u.Email == model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Este email já está registado.");
                return View(model);
            }

            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = "Cidadao",
                Token = null,
                TokenExpiry = null,
                EmailVerified = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Generate email verification code (plain for sending) and store hashed
            var code = GenerateVerificationCode();
            user.EmailVerificationCodeHash = ComputeVerificationHash(code);
            user.EmailVerificationExpiry = DateTime.Now.AddMinutes(15);
            user.EmailVerificationSentAt = DateTime.Now;
            _context.SaveChanges();

            var verifyLink = Url.Action(
                "ConfirmEmail",
                "Account",
                new { email = user.Email, code = code },
                Request.Scheme
            );

            string body = $@"
                <h2>Verificação de Email</h2>
                <p>O seu código de verificação é: <strong>{code}</strong></p>
                <p>Ou clique no link para verificar: <a href='{verifyLink}'>Verificar conta</a></p>
                <p>O código expira em 15 minutos.</p>
            ";

            _emailService.SendEmail(
                user.Email,
                "EcoCityWaste - Verificação de Conta",
                body
            );

            // If user clicks the verification link in the email, they can confirm without being logged in

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied() // to block users with no specific roles to access certain pages
        {
            return View();
        }

        // Generate a 6-digit verification code
        private static string GenerateVerificationCode()
        {
            int value = RandomNumberGenerator.GetInt32(0, 1000000);
            return value.ToString("D6");
        }

        private string ComputeVerificationHash(string code)
        {
            var defaultKey = "default_verification_key_change_in_production";
            var key = defaultKey;
            try
            {
                if (_config != null)
                {
                    var cfg = _config["AppSettings:VerificationKey"];
                    if (!string.IsNullOrEmpty(cfg)) key = cfg;
                }
            }
            catch
            {
                // ignore and use defaultKey
            }

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(code));
            return Convert.ToBase64String(hash);
        }

        // GET: /Account/Verify
        [HttpGet]
        public IActionResult Verify()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login");

            var claimsPrincipal = User as ClaimsPrincipal;
            var email = claimsPrincipal?.FindFirstValue(ClaimTypes.Email);
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
                return RedirectToAction("Login");

            ViewBag.Email = user.Email;
            var remaining = 0;
            if (user.EmailVerificationExpiry.HasValue)
            {
                var rem = user.EmailVerificationExpiry.Value - DateTime.Now;
                if (rem.TotalSeconds > 0)
                    remaining = (int)rem.TotalSeconds;
            }
            ViewBag.Remaining = remaining;

            return View();
        }

        // POST: /Account/Verify
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Verify(string code)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login");

            var claimsPrincipal = User as ClaimsPrincipal;
            var email = claimsPrincipal?.FindFirstValue(ClaimTypes.Email);
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
                return RedirectToAction("Login");

            if (user.EmailVerified)
            {
                TempData["Info"] = "Email já verificado.";
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                ModelState.AddModelError(string.Empty, "Insira o código de verificação.");
                return View();
            }

            // Check block
            if (user.EmailVerificationBlockedUntil.HasValue && user.EmailVerificationBlockedUntil.Value > DateTime.Now)
            {
                ModelState.AddModelError(string.Empty, "Muitas tentativas falhadas. Tente novamente mais tarde.");
                return View();
            }

            // Check expiry
            if (!user.EmailVerificationExpiry.HasValue || user.EmailVerificationExpiry.Value < DateTime.Now)
            {
                ModelState.AddModelError(string.Empty, "O código expirou. Peça um novo código.");
                return View();
            }

            var providedHash = ComputeVerificationHash(code.Trim());
            if (user.EmailVerificationCodeHash == providedHash)
            {
                user.EmailVerified = true;
                user.EmailVerificationCodeHash = null;
                user.EmailVerificationExpiry = null;
                user.EmailVerificationSentAt = null;
                user.EmailVerificationAttempts = 0;
                user.EmailVerificationBlockedUntil = null;
                _context.SaveChanges();

                TempData["Success"] = "Email verificado com sucesso.";
                return RedirectToAction("Index", "Home");
            }

            // invalid code
            user.EmailVerificationAttempts++;
            if (user.EmailVerificationAttempts >= 5)
            {
                user.EmailVerificationBlockedUntil = DateTime.Now.AddMinutes(15);
            }
            _context.SaveChanges();
            ModelState.AddModelError(string.Empty, "Código inválido.");
            return View();
        }

        // POST: /Account/Resend
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Resend()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login");

            var claimsPrincipal = User as ClaimsPrincipal;
            var email = claimsPrincipal?.FindFirstValue(ClaimTypes.Email);
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
                return RedirectToAction("Login");

            // Check block
            if (user.EmailVerificationBlockedUntil.HasValue && user.EmailVerificationBlockedUntil.Value > DateTime.Now)
            {
                TempData["Error"] = "Conta temporariamente bloqueada por muitas tentativas falhadas.";
                return RedirectToAction("Verify");
            }

            // Cooldown: disallow resend within 60 seconds of last send
            if (user.EmailVerificationSentAt.HasValue)
            {
                var since = DateTime.Now - user.EmailVerificationSentAt.Value;
                if (since.TotalSeconds < 60)
                {
                    TempData["Error"] = "Aguarde antes de reenviar o código (60s).";
                    return RedirectToAction("Verify");
                }
            }

            var code = GenerateVerificationCode();
            user.EmailVerificationCodeHash = ComputeVerificationHash(code);
            user.EmailVerificationExpiry = DateTime.Now.AddMinutes(15);
            user.EmailVerificationSentAt = DateTime.Now;
            user.EmailVerificationAttempts = 0;
            _context.SaveChanges();

            string body = $@"
                <h2>Verificação de Email</h2>
                <p>O seu novo código de verificação é: <strong>{code}</strong></p>
                <p>O código expira em 15 minutos.</p>
            ";

            _emailService.SendEmail(user.Email, "EcoCityWaste - Verificação de Conta", body);

            TempData["Info"] = "Código reenviado (se a conta existir).";
            return RedirectToAction("Verify");
        }

        // AJAX resend endpoint
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResendAjax()
        {
            if (!User.Identity.IsAuthenticated)
                return Json(new { success = false, needsLogin = true, loginUrl = Url.Action("Login", "Account") });

            var claimsPrincipal = User as ClaimsPrincipal;
            var email = claimsPrincipal?.FindFirstValue(ClaimTypes.Email);
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
                return Json(new { success = false, message = "Se a conta existir, enviámos o código." });

            if (user.EmailVerificationBlockedUntil.HasValue && user.EmailVerificationBlockedUntil.Value > DateTime.Now)
                return Json(new { success = false, message = "Conta temporariamente bloqueada por muitas tentativas falhadas." });

            if (user.EmailVerificationSentAt.HasValue)
            {
                var since = DateTime.Now - user.EmailVerificationSentAt.Value;
                if (since.TotalSeconds < 60)
                {
                    return Json(new { success = false, message = "Aguarde antes de reenviar o código (60s).", remaining = 60 - (int)since.TotalSeconds });
                }
            }

            var code = GenerateVerificationCode();
            user.EmailVerificationCodeHash = ComputeVerificationHash(code);
            user.EmailVerificationExpiry = DateTime.Now.AddMinutes(15);
            user.EmailVerificationSentAt = DateTime.Now;
            user.EmailVerificationAttempts = 0;
            _context.SaveChanges();

            string body = $@"
                <h2>Verificação de Email</h2>
                <p>O seu novo código de verificação é: <strong>{code}</strong></p>
                <p>O código expira em 15 minutos.</p>
            ";

            _emailService.SendEmail(user.Email, "EcoCityWaste - Verificação de Conta", body);

            return Json(new { success = true, message = "Código reenviado.", remaining = 60 });
        }

        // Public endpoint to confirm email via link in email (no authentication required)
        [HttpGet]
        public IActionResult ConfirmEmail(string email, string code)
        {
            ViewBag.Email = email;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
            {
                ViewBag.Success = false;
                ViewBag.Message = "Link inválido.";
                return View("ConfirmEmail");
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                // Do not reveal account existence
                ViewBag.Success = true;
                ViewBag.Message = "Se a conta existir, foi verificada. Pode iniciar sessão.";
                return View("ConfirmEmail");
            }

            // Check expiry
            if (!user.EmailVerificationExpiry.HasValue || user.EmailVerificationExpiry.Value < DateTime.Now)
            {
                ViewBag.Success = false;
                ViewBag.Message = "O código expirou. Peça um novo código.";
                return View("ConfirmEmail");
            }

            var providedHash = ComputeVerificationHash(code.Trim());
            if (user.EmailVerificationCodeHash == providedHash)
            {
                user.EmailVerified = true;
                user.EmailVerificationCodeHash = null;
                user.EmailVerificationExpiry = null;
                user.EmailVerificationSentAt = null;
                user.EmailVerificationAttempts = 0;
                user.EmailVerificationBlockedUntil = null;
                _context.SaveChanges();

                ViewBag.Success = true;
                ViewBag.Message = "Email verificado com sucesso. Pode iniciar sessão.";
                return View("ConfirmEmail");
            }

            ViewBag.Success = false;
            ViewBag.Message = "Código inválido.";
            return View("ConfirmEmail");
        }

    }

}