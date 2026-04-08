using EcoCityWaste.Controllers;
using EcoCityWaste.Data;
using EcoCityWaste.Models;
using EcoCityWaste.Models.ViewModels;
using EcoCityWaste.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace EcoCityWasteProjetoESA.Tests
{
    public class RoutesControllerTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new AppDbContext(options);

            context.Contentores.AddRange(
                new Container { Id = 10, Code = "C1", Location = "Praça do Bocage", Type = "Vidro", IsActive = true, Latitude = 38.5, Longitude = -8.8 },
                new Container { Id = 11, Code = "C2", Location = "Avenida Luísa Todi", Type = "Papel", IsActive = true, Latitude = 38.6, Longitude = -8.9 }
            );

            context.Users.Add(new User { Id = 1, Username = "worker1", Email = "worker1@gmail.com", PasswordHash="hash", Role = "Funcionario" });

            context.SaveChanges();
            return context;
        }

        private RoutesController GetController(AppDbContext context, string username = "admin", string role = "Admin")
        {
            var optimiser = new RouteOptimisationService();
            var historyService = new ContainerHistoryService(context);

            var controller = new RoutesController(context, optimiser, historyService);

            // simular user identity
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role)
            }, "mock"));

            // tempdata
            var mockTempDataProvider = new Mock<ITempDataProvider>();
            var tempData = new TempDataDictionary(new DefaultHttpContext(), mockTempDataProvider.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            };

            controller.TempData = tempData;

            return controller;
        }

        [Fact]
        public async Task Create_Post_ValidModel_PersistsRoute()
        {
            // Arrange
            using var context = GetDbContext();
            var controller = GetController(context);
            var model = new RouteCreateViewModel
            {
                Name = "Morning Route",
                Description = "Daily pick up",
                ContainerIds = new List<int> { 10, 11 }
            };

            // Act
            var result = await controller.Create(model);

            // Assert
            var route = await context.Routes.Include(r => r.RouteContainers).FirstOrDefaultAsync(r => r.Name == "Morning Route");
            Assert.NotNull(route);
            Assert.Equal(2, route.RouteContainers.Count);
            Assert.IsType<RedirectToActionResult>(result);
        }

        [Fact]
        public async Task Edit_Post_UpdatesRouteNameAndContainers()
        {
            // Arrange
            using var context = GetDbContext();
            var route = new EcoCityWaste.Models.Route { Id = 1, Name = "Old Name", Code = "RT-001" };
            context.Routes.Add(route);
            await context.SaveChangesAsync();

            var controller = GetController(context);
            var model = new RouteEditViewModel
            {
                Id = 1,
                Name = "Updated Name",
                ContainerIds = new List<int> { 10 }
            };

            // Act
            await controller.Edit(model);

            // Assert
            var updated = await context.Routes.Include(r => r.RouteContainers).FirstAsync(r => r.Id == 1);
            Assert.Equal("Updated Name", updated.Name);
            Assert.Single(updated.RouteContainers);
        }

        [Fact]
        public async Task Assign_Post_ChangesStatusAndEmployee()
        {
            // Arrange
            using var context = GetDbContext();
            var route = new EcoCityWaste.Models.Route { Id = 1, Name = "Route 1", Status = EcoCityWaste.Models.Route.RouteStatus.Pending };
            context.Routes.Add(route);
            await context.SaveChangesAsync();

            var controller = GetController(context);
            var model = new RouteAssignViewModel { RouteId = 1, EmployeeId = 1 };

            // Act
            await controller.Assign(model);

            // Assert
            var updated = await context.Routes.FindAsync(1);
            Assert.Equal(1, updated.AssignedEmployeeId);
            Assert.Equal(EcoCityWaste.Models.Route.RouteStatus.InProgress, updated.Status);
        }

        [Fact]
        public async Task Complete_Post_SetsStatusToCompleted()
        {
            // Arrange
            using var context = GetDbContext();
            var route = new EcoCityWaste.Models.Route { Id = 5, Status = EcoCityWaste.Models.Route.RouteStatus.InProgress };
            context.Routes.Add(route);
            await context.SaveChangesAsync();

            var controller = GetController(context);

            // Act
            await controller.Complete(5);

            // Assert
            var updated = await context.Routes.FindAsync(5);
            Assert.Equal(EcoCityWaste.Models.Route.RouteStatus.Completed, updated.Status);
            Assert.NotNull(updated.CompletedAt);
        }

        [Fact]
        public async Task Delete_RemovesRouteAndAssociations()
        {
            // Arrange
            using var context = GetDbContext();
            var route = new EcoCityWaste.Models.Route { Id = 9, Name = "To Delete" };
            route.RouteContainers.Add(new RouteContainer { ContainerId = 10, PickupOrder = 1 });
            context.Routes.Add(route);
            await context.SaveChangesAsync();

            var controller = GetController(context);

            // Act
            await controller.Delete(9);

            // Assert
            Assert.Null(await context.Routes.FindAsync(9));
            var containersInRoute = await context.RouteContainers.Where(rc => rc.RouteId == 9).ToListAsync();
            Assert.Empty(containersInRoute);
        }

        [Fact]
        public async Task Index_FuncionarioRole_FiltersByAssignedUser()
        {
            // Arrange
            using var context = GetDbContext();
            // atribuir route - worker1
            context.Routes.Add(new EcoCityWaste.Models.Route
            {
                Id = 1,
                Name = "Worker Route",
                AssignedEmployee = await context.Users.FirstAsync() 
            });
            // Route not assigned
            context.Routes.Add(new EcoCityWaste.Models.Route { Id = 2, Name = "Admin Route" });
            await context.SaveChangesAsync();

            // Act as "worker1"
            var controller = GetController(context, "worker1", "Funcionario");
            var result = await controller.Index(null) as ViewResult;
            var model = result.Model as List<EcoCityWaste.Models.Route>;

            // Assert
            Assert.Single(model);
            Assert.Equal("Worker Route", model[0].Name);
        }
    }
}