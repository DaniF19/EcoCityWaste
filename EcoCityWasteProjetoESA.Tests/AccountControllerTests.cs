using EcoCityWaste.Controllers;
using EcoCityWaste.Data;
using EcoCityWaste.Models;
using EcoCityWaste.Services;
using EcoCityWaste.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;   
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace EcoCityWasteProjetoESA.Tests
{
    public class AccountControllerTests
    {
        private AppDbContext GetInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public void ForgotPassword_EmailDoesNotExist_DoesNothing()
        {
            // Arrange
            var context = GetInMemoryDb();
            var emailMock = new Mock<IEmailService>();
            var controller = new AccountController(emailMock.Object, context);

            var model = new ForgotPasswordViewModel
            {
                Email = "missing@test.com"
            };

            // Act
            var result = controller.ForgotPassword(model) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Empty(context.Users);
            Assert.True((bool)controller.ViewBag.ShowModal);
        }

        [Fact]
        public void ForgotPassword_ExistingEmail_CreatesToken()
        {
            // Arrange
            var context = GetInMemoryDb();
            context.Users.Add(new User
            {
                Username = "TestUser",
                Email = "user@test.com",
                PasswordHash = "hash",
                Token = null,
                TokenExpiry = null
            });
            context.SaveChanges();

            var emailMock = new Mock<IEmailService>();
            emailMock.Setup(e => e.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

            var controller = new AccountController(emailMock.Object, context);
            controller.ViewBag.ShowModal = false;

            // Mock url action
            var urlHelperMock = new Mock<IUrlHelper>();
            urlHelperMock
                .Setup(x => x.Action(It.IsAny<UrlActionContext>()))
                .Returns("http://localhost/reset-link");
            controller.Url = urlHelperMock.Object;

            // Mock request scheme
            var httpContextMock = new Mock<HttpContext>();
            var requestMock = new Mock<HttpRequest>();
            requestMock.Setup(r => r.Scheme).Returns("http");
            requestMock.Setup(r => r.Host).Returns(new HostString("localhost"));
            httpContextMock.Setup(x => x.Request).Returns(requestMock.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContextMock.Object
            };

            // Act
            var result = controller.ForgotPassword(new ForgotPasswordViewModel
            {
                Email = "user@test.com"
            }) as ViewResult;

            // Assert
            var user = context.Users.First();
            Assert.NotNull(user.Token);
            Assert.NotNull(user.TokenExpiry);
            Assert.True((bool)controller.ViewBag.ShowModal);
            emailMock.Verify(e => e.SendEmail("user@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void ResetPassword_ValidToken_UpdatesPassword()
        {
            // Arrange
            var context = GetInMemoryDb();

            context.Users.Add(new User
            {
                Username = "TestUser",
                Email = "user@test.com",
                PasswordHash = "oldhash",
                Token = "token123",
                TokenExpiry = DateTime.Now.AddMinutes(5)
            });
            context.SaveChanges();

            var emailMock = new Mock<IEmailService>();
            var controller = new AccountController(emailMock.Object, context);

            // Act
            var result = controller.ResetPassword(new ResetPasswordViewModel
            {
                Token = "token123",
                NewPassword = "NewPassword123!",
                ConfirmPassword = "NewPassword123!"
            }) as RedirectToActionResult;

            // Assert
            var user = context.Users.First();
            Assert.NotNull(result);
            Assert.NotEqual("oldhash", user.PasswordHash);
            Assert.Null(user.Token);
            Assert.Null(user.TokenExpiry);
            Assert.Equal("Login", result!.ActionName);
        }

        [Fact]
        public async Task Login_ComCredenciaisValidas_DeveRedirecionarParaHome()
        {
            // Arrange
            var context = GetInMemoryDb();
            var controller = new AccountController(null, context);

            // criar utilizador na BD 
            var password = "123456";
            context.Users.Add(new User
            {
                Email = "admin@ecocity.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Username = "Admin"
            });
            await context.SaveChangesAsync();

            var model = new LoginViewModel { Email = "admin@ecocity.com", Password = password };

            // Mock de autentica��o e URL helper
            var authServiceMock = new Mock<IAuthenticationService>();
            var urlHelperMock = new Mock<IUrlHelper>();
            var serviceProviderMock = new Mock<IServiceProvider>();

            serviceProviderMock.Setup(s => s.GetService(typeof(IAuthenticationService))).Returns(authServiceMock.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = serviceProviderMock.Object }
            };
            controller.Url = urlHelperMock.Object;

            // Act
            var result = await controller.Login(model) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            Assert.Equal("Home", result.ControllerName);
        }

        [Fact]
        public void LoginGoogle_DeveRetornarChallenge()
        {
            // Arrange
            var context = GetInMemoryDb();
            var controller = new AccountController(null, context);

            // Mock do UrlHelper
            var urlHelperMock = new Mock<IUrlHelper>();
            controller.Url = urlHelperMock.Object;

            // Act
            var result = controller.LoginGoogle();

            // Assert
            Assert.IsType<ChallengeResult>(result);
        }

        [Fact]
        public async Task Logout_DeveRedirecionarParaLogin()
        {
            // Arrange
            var context = GetInMemoryDb();
            var controller = new AccountController(null, context);

            // Mocks para as Cookies e as Rotas
            var authServiceMock = new Mock<IAuthenticationService>();
            var urlHelperMock = new Mock<IUrlHelper>();
            var serviceProviderMock = new Mock<IServiceProvider>();

            serviceProviderMock.Setup(s => s.GetService(typeof(IAuthenticationService))).Returns(authServiceMock.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = serviceProviderMock.Object }
            };
            controller.Url = urlHelperMock.Object;

            // Act
            var result = await controller.Logout() as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Login", result.ActionName);
        }

        [Fact]
        public async Task Register_ValidUser_CreatesUserAndRedirects()
        {
            // Arrange
            var context = GetInMemoryDb();
            var emailMock = new Mock<IEmailService>();

            var controller = new AccountController(emailMock.Object, context);

            var model = new RegisterViewModel
            {
                Username = "NewUser",
                Email = "new@test.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            // Mock authentication service (needed for SignInAsync)
            var authServiceMock = new Mock<IAuthenticationService>();
            var serviceProviderMock = new Mock<IServiceProvider>();

            serviceProviderMock
                .Setup(s => s.GetService(typeof(IAuthenticationService)))
                .Returns(authServiceMock.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestServices = serviceProviderMock.Object
                }
            };

            var urlHelperMock = new Mock<IUrlHelper>();
            controller.Url = urlHelperMock.Object;

            // Act
            var result = await controller.Register(model) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            Assert.Equal("Home", result.ControllerName);

            var user = context.Users.FirstOrDefault(u => u.Email == "new@test.com");
            Assert.NotNull(user);
            Assert.Equal("NewUser", user!.Username);
            Assert.True(BCrypt.Net.BCrypt.Verify("Password123!", user.PasswordHash));
        }

        [Fact]
        public async Task Register_EmailAlreadyExists_ReturnsViewWithModelError()
        {
            // Arrange
            var context = GetInMemoryDb();

            context.Users.Add(new User
            {
                Username = "Existing",
                Email = "existing@test.com",
                PasswordHash = "hash"
            });
            context.SaveChanges();

            var emailMock = new Mock<IEmailService>();
            var controller = new AccountController(emailMock.Object, context);

            var model = new RegisterViewModel
            {
                Username = "NewUser",
                Email = "existing@test.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            // Act
            var result = await controller.Register(model) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("Email"));
            Assert.Single(context.Users); // no new user created
        }


    }
}

