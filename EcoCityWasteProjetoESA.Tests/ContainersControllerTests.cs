using EcoCityWaste.Controllers;
using EcoCityWaste.Data;
using EcoCityWaste.Dtos;
using EcoCityWaste.Models;
using EcoCityWaste.ViewModels;
using EcoCityWaste.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;


namespace EcoCityWasteProjetoESA.Tests
{
    public class ContainersControllerTests
    {
        private readonly ITestOutputHelper _output;

        public ContainersControllerTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private AppDbContext GetDbContext()
        {
            // Base de Dados na Memória
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new AppDbContext(options);

            // Dados de Teste
            context.Contentores.AddRange(
                new Container { Id = 1, Code = "CNT-00500", Location = "Praça do Bocage, Setúbal", Latitude = 38.5244, Longitude = -8.8882, Type = "Plástico", Status = Container.ContainerStatus.Good, FillLevel = 85, InstallationDate = DateTime.Now.AddDays(-120), LastUpdated = DateTime.Now.AddMinutes(-30), IsActive = true },
                new Container { Id = 2, Code = "CNT-00200", Location = "Praça do Bocage, Setúbal", Latitude = 38.5244, Longitude = -8.8882, Type = "Vidro", Status = Container.ContainerStatus.Broken, FillLevel = 40, InstallationDate = DateTime.Now.AddDays(-120), LastUpdated = DateTime.Now.AddMinutes(-30), IsActive = true },
                new Container { Id = 3, Code = "CNT-00300", Location = "Praça do Bocage, Setúbal", Latitude = 38.5244, Longitude = -8.8882, Type = "Papel", Status = Container.ContainerStatus.Good, FillLevel = 10, InstallationDate = DateTime.Now.AddDays(-120), LastUpdated = DateTime.Now.AddMinutes(-30), IsActive = false }
            );

            context.SaveChanges();
            return context;
        }

        private GeocodingService GetGeoService()
        {
            var httpClient = new HttpClient(new FakeHttpMessageHandler());
            return new GeocodingService(httpClient);
        }

        private ContainerHistoryService GetHistoryService(AppDbContext context)
        {
            return new ContainerHistoryService(context);
        }

        [Fact]
        public async Task Data_Persistance()
        {
            using var context = GetDbContext();
            var containerOriginal = await context.Contentores.FirstAsync();

            int nivelSimulado = 88;
            var estadoSimulado = Container.ContainerStatus.Maintenance;
            DateTime horaSimulada = new DateTime(2026, 02, 22, 14, 0, 0);

            containerOriginal.FillLevel = nivelSimulado;
            containerOriginal.Status = estadoSimulado;
            containerOriginal.LastUpdated = horaSimulada;

            await context.SaveChangesAsync();

            var containerNaBD = await context.Contentores
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == containerOriginal.Id);

            Assert.NotNull(containerNaBD);
            Assert.Equal(nivelSimulado, containerNaBD.FillLevel);
            Assert.Equal(estadoSimulado, containerNaBD.Status);
            Assert.Equal(horaSimulada, containerNaBD.LastUpdated);
        }


        [Fact]
        public async Task SensorService_LevelLimit()
        {
            // Arrange
            using var context = GetDbContext();

            // Vamos buscar o CNT-001 que começa com 85% de enchimento
            var container = await context.Contentores.FindAsync(1);

            // Simulamos um aumento de 20% (85 + 20 = 105)
            // A lógica deve garantir que o valor não ultrapassa os 100
            container.FillLevel = Math.Min(100, container.FillLevel + 20);
            await context.SaveChangesAsync(); // EC-58: Persistência

            // Assert: O nível deve ter ficado exatamente em 100% e não 105%
            Assert.Equal(100, container.FillLevel);
        }

        [Fact]
        public async Task SensorService_ActiveContainersUpdate()
        {
            // Arrange
            using var context = GetDbContext();

            // Act: Simula a lógica de atualizar apenas ativos
            var list = await context.Contentores.ToListAsync();
            foreach (var c in list.Where(x => x.IsActive)) { c.FillLevel = 100; }
            await context.SaveChangesAsync();

            // Assert
            Assert.Equal(100, (await context.Contentores.FindAsync(1)).FillLevel); // Ativo atualizou
            Assert.Equal(10, (await context.Contentores.FindAsync(3)).FillLevel);  // Inativo manteve-se
        }

        [Fact]
        public async Task Index_FilterMediumLevel()
        {
            // Arrange
            using var context = GetDbContext();
            var controller = new ContainersController(context, GetGeoService(), GetHistoryService(context));

            // Act: Filtro "Medio"
            var result = await controller.Index(null, "Medio", null) as ViewResult;
            var model = result.Model as List<Container>;

            // Assert: Esperamos apenas 1 (o CNT-00500 de 85%)
            Assert.Single(model);
            Assert.Equal("CNT-00500", model[0].Code);
        }

        [Fact]
        public async Task Index_FilterTypeResidual()
        {
            // Arrange
            using var context = GetDbContext();
            var controller = new ContainersController(context, GetGeoService(), GetHistoryService(context));

            // Act: Filtra por Vidro
            var result = await controller.Index(null, "Vidro", null) as ViewResult;
            var model = result.Model as List<Container>;

            // Assert: Esperamos apenas 1 (o CNT-002)
            Assert.Single(model);
            Assert.Equal("Vidro", model[0].Type);
        }

        // funcionario edit

        [Fact]
        public async Task ListStatus_ReturnsAllContainers()
        {
            // Arrange
            using var context = GetDbContext();

            context.Contentores.AddRange(
                new Container
                {
                    Code = "CNT-900",
                    Location = "Centro",
                    Type = "Vidro",
                    Status = Container.ContainerStatus.Good,
                    IsActive = true
                },
                new Container
                {
                    Code = "CNT-901",
                    Location = "Bairro",
                    Type = "Papel",
                    Status = Container.ContainerStatus.Full,
                    IsActive = false
                }
            );

            await context.SaveChangesAsync();

            var controller = new ContainersController(context, GetGeoService(), GetHistoryService(context));

            // Act
            var result = await controller.ListStatus() as ViewResult;
            var model = result?.Model as List<Container>;

            // Assert
            Assert.NotNull(model);

            Assert.Equal(5, model.Count);

            Assert.Contains(model, c => c.IsActive);
            Assert.Contains(model, c => !c.IsActive);
        }

        [Fact]
        public async Task UpdateStatus_Post_ValidStatus_UpdatesContainer()
        {
            // Arrange
            using var context = GetDbContext();

            var container = new Container
            {
                Code = "CNT-001",
                Location = "Centro",
                Type = "Vidro",
                Status = Container.ContainerStatus.Good,
                IsActive = true
            };

            context.Contentores.Add(container);
            await context.SaveChangesAsync();

            var controller = new ContainersController(context, GetGeoService(), GetHistoryService(context));

            var dto = new UpdateContainerStatusDto
            {
                Id = container.Id,
                Status = "Broken"
            };

            // Act
            var result = await controller.UpdateStatus(container.Id, dto);

            // Assert
            var updated = await context.Contentores.FindAsync(container.Id);

            Assert.Equal(Container.ContainerStatus.Broken, updated.Status);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ListStatus", redirect.ActionName);
        }

        [Fact]
        public async Task Register_Post_ValidModel_AddsContainer()
        {
            // Arrange
            using var context = GetDbContext();
            var controller = new ContainersController(context, GetGeoService(), GetHistoryService(context));

            var model = new ContainerRegisterViewModel
            {
                Location = "Avenida Luísa Todi",
                Type = "Plástico",
                Status = "Good"
            };

            // Act
            var result = await controller.Register(model) as ViewResult;

            // Assert
            var containers = await context.Contentores.ToListAsync();

            Assert.Equal(4, containers.Count); // 3 originais + 1 novo

            var newContainer = containers.Last();

            Assert.Equal("Avenida Luísa Todi", newContainer.Location);
            Assert.Equal("Plástico", newContainer.Type);
            Assert.Equal(Container.ContainerStatus.Good, newContainer.Status);
            Assert.True(newContainer.IsActive);
            Assert.Equal(0, newContainer.FillLevel);
            Assert.NotNull(result);
            Assert.True(controller.ViewBag.Success != null);
        }

        [Fact]
        public async Task Register_Post_InvalidModel_ReturnsView_WithoutAdding()
        {
            // Arrange
            using var context = GetDbContext();
            var controller = new ContainersController(context, GetGeoService(), GetHistoryService(context));

            controller.ModelState.AddModelError("Location", "Required");

            var model = new ContainerRegisterViewModel();

            // Act
            var result = await controller.Register(model) as ViewResult;

            // Assert
            var containers = await context.Contentores.ToListAsync();

            Assert.Equal(3, containers.Count);
            Assert.NotNull(result);
            Assert.Equal(model, result.Model);
        }

        [Fact]
        public async Task Edit_Get_ValidId_ReturnsView()
        {
            // Arrange
            using var context = GetDbContext();
            var controller = new ContainersController(context, GetGeoService(), GetHistoryService(context));

            // Act
            var result = await controller.Edit(1) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<Container>(result.Model);
            Assert.Equal(1, model.Id);
        }

        [Fact]
        public async Task Edit_Get_InvalidId_ReturnsNotFound()
        {
            using var context = GetDbContext();
            var controller = new ContainersController(context, GetGeoService(), GetHistoryService(context));

            var result = await controller.Edit(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Post_ValidModel_UpdatesContainer()
        {
            // Arrange
            using var context = GetDbContext();
            var controller = new ContainersController(context, GetGeoService(), GetHistoryService(context));

            var model = new ContainerEditViewModel
            {
                Id = 1,
                Location = "Nova Localização",
                Type = "Vidro",
                Status = Container.ContainerStatus.Maintenance
            };

            // Act
            var result = await controller.Edit(model);

            // Assert
            var updated = await context.Contentores.FindAsync(1);

            Assert.Equal("Nova Localização", updated.Location);
            Assert.Equal("Vidro", updated.Type);
            Assert.Equal(Container.ContainerStatus.Maintenance, updated.Status);

            Assert.IsType<RedirectToActionResult>(result);
        }

        [Fact]
        public async Task Edit_Post_InvalidModel_ReturnsView()
        {
            using var context = GetDbContext();
            var controller = new ContainersController(context, GetGeoService(), GetHistoryService(context));

            controller.ModelState.AddModelError("Location", "Required");

            var model = new ContainerEditViewModel
            {
                Id = 1
            };

            var result = await controller.Edit(model);

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Edit_Post_InvalidId_ReturnsNotFound()
        {
            using var context = GetDbContext();
            var controller = new ContainersController(context, GetGeoService(), GetHistoryService(context));

            var model = new ContainerEditViewModel
            {
                Id = 999,
                Location = "X",
                Type = "Vidro",
                Status = Container.ContainerStatus.Good
            };

            var result = await controller.Edit(model);

            Assert.IsType<NotFoundResult>(result);
        }

        // history status containers tests

        [Fact]
        public async Task UpdateStatus_CreatesHistoryRecord()
        {
            // Arrange
            using var context = GetDbContext();

            context.ContainerStatusHistories = context.Set<ContainerStatusHistory>();

            var container = new Container
            {
                Code = "CNT-100",
                Location = "Centro",
                Type = "Vidro",
                Status = Container.ContainerStatus.Good,
                IsActive = true
            };

            context.Contentores.Add(container);
            await context.SaveChangesAsync();

            var controller = new ContainersController(context, GetGeoService(), GetHistoryService(context));

            var dto = new UpdateContainerStatusDto
            {
                Id = container.Id,
                Status = "Broken"
            };

            // Act
            await controller.UpdateStatus(container.Id, dto);

            // Assert
            var history = await context.ContainerStatusHistories
                .Where(h => h.ContainerId == container.Id)
                .ToListAsync();

            Assert.Single(history);
            Assert.Equal(Container.ContainerStatus.Broken, history[0].Status);
        }

        [Fact]
        public async Task History_ReturnsContainerHistory()
        {
            // Arrange
            using var context = GetDbContext();

            context.ContainerStatusHistories = context.Set<ContainerStatusHistory>();

            context.ContainerStatusHistories.AddRange(
                new ContainerStatusHistory
                {
                    ContainerId = 1,
                    Status = Container.ContainerStatus.Good,
                    FillLevel = 50,
                    IsActive = true,
                    ChangedAt = DateTime.Now,
                    ChangedBy = "TestUser"
                },
                new ContainerStatusHistory
                {
                    ContainerId = 1,
                    Status = Container.ContainerStatus.Full,
                    FillLevel = 95,
                    IsActive = true,
                    ChangedAt = DateTime.Now,
                    ChangedBy = "TestUser"
                }
            );

            await context.SaveChangesAsync();

            var controller = new ContainersController(context, GetGeoService(), GetHistoryService(context));

            // Act
            var result = await controller.History(1) as ViewResult;
            var model = result.Model as List<ContainerStatusHistory>;

            // Assert
            Assert.NotNull(model);
            Assert.Equal(2, model.Count);
        }

        //[Fact]
        //public async Task GetCoordinates_ValidAddress_ReturnsCoordinates()
        //{
        //    // Arrange
        //    var httpClient = new HttpClient();
        //    var service = new GeocodingService(httpClient);

        //    // Act
        //    var result = await service.GetCoordinates("avenida de angola");

        //    // Assert
        //    Assert.NotEqual(0, result.lat);
        //    Assert.NotEqual(0, result.lon);
        //}

        [Fact]
        public async Task GetCoordinates_ValidAddress_ReturnsCoordinates()
        {
            // Arrange
            var httpClient = new HttpClient();
            var service = new GeocodingService(httpClient);

            // Act
            var result = await service.GetCoordinates("Barreiro");

            _output.WriteLine($"Latitude: {result.lat}");
            _output.WriteLine($"Longitude: {result.lon}");

            Console.WriteLine($"Lat: {result.lat}, Lon: {result.lon}");

            // Assert
            Assert.NotEqual(0, result.lat);
            Assert.NotEqual(0, result.lon);
        }

    }
}
