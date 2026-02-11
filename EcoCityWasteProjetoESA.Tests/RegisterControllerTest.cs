using EcoCityWaste.Controllers;
using EcoCityWaste.Data;
using EcoCityWaste.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Xunit;

public class AccountControllerRegisterTests
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
    public void Register_Get_ReturnsViewWithModel()
    {
        // Arrange
        var context = GetInMemoryDb();
        var controller = new AccountController(null, context);

        // Act
        var result = controller.Register() as ViewResult;

        // Assert
        Assert.NotNull(result);
        Assert.IsType<RegisterViewModel>(result.Model);
    }

    [Fact]
    public async Task Register_Post_ValidModel_RedirectsToLogin()
    {
        // Arrange
        var context = GetInMemoryDb();
        var controller = new AccountController(null, context);

        var model = new RegisterViewModel
        {
            Name = "Test User",
            Email = "test@ecocity.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        // Act
        var result = await controller.Register(model) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Login", result.ActionName);
        Assert.True(controller.TempData.ContainsKey("SuccessMessage"));
        Assert.Equal("Conta criada com sucesso!", controller.TempData["SuccessMessage"]);
    }

    [Fact]
    public async Task Register_Post_InvalidModel_ReturnsViewWithModel()
    {
        // Arrange
        var context = GetInMemoryDb();
        var controller = new AccountController(null, context);

        var model = new RegisterViewModel
        {
            Name = "", // inválido
            Email = "invalid-email",
            Password = "123",
            ConfirmPassword = "321"
        };

        // Força ModelState inválido
        controller.ModelState.AddModelError("Name", "O nome é obrigatório.");
        controller.ModelState.AddModelError("Email", "Email inválido.");
        controller.ModelState.AddModelError("Password", "A password é inválida.");
        controller.ModelState.AddModelError("ConfirmPassword", "As passwords não coincidem.");

        // Act
        var result = await controller.Register(model) as ViewResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(model, result.Model);
        Assert.False(controller.ModelState.IsValid);
    }
}
