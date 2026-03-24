using EcoCityWaste.Controllers;
using EcoCityWaste.Data;
using EcoCityWaste.Models;
using EcoCityWaste.ViewModels;
using EcoCityWaste.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace EcoCityWasteProjetoESA.Tests
{
    public class ContainersListAndUpdateIntegrationTests
    {
        // Criação de uma base de dados limpa na memória para garantir cada teste
        private AppDbContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
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
        public async Task List_Integration_ReturnsAllContainers()
        {
            // Arrange
            using var context = GetDatabaseContext();
            context.Contentores.AddRange(
                new Container { Id = 1, Code = "CNT-001", Location = "Praça do Bocage", Type = "Vidro", Status = Container.ContainerStatus.Good, IsActive = true },
                new Container { Id = 2, Code = "CNT-002", Location = "Avenida Luísa Todi", Type = "Papel", Status = Container.ContainerStatus.Full, IsActive = false }
            );
            await context.SaveChangesAsync();

            var controller = new ContainersController(context, GetGeoService(), GetHistoryService(context));

            // Act
            var result = await controller.List() as ViewResult;
            var model = result?.Model as List<Container>;

            // Assert
            Assert.NotNull(model);
            // Confirma que a ação List retorna todos os contentores (ativos e inativos)l
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task Edit_Post_Integration_UpdatesContainerFieldsInDatabase()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var initialContainer = new Container
            {
                Id = 1,
                Code = "CNT-001",
                Location = "Localização Antiga",
                Type = "Vidro",
                Status = Container.ContainerStatus.Good,
                LastUpdated = DateTime.Now.AddDays(-2)
            };
            context.Contentores.Add(initialContainer);
            await context.SaveChangesAsync();

            var controller = new ContainersController(context, GetGeoService(), GetHistoryService(context));
            var editModel = new ContainerEditViewModel
            {
                Id = 1,
                Location = "Localização Nova",
                Type = "Plástico",
                Status = Container.ContainerStatus.Broken
            };

            // Act
            var result = await controller.Edit(editModel);

            // Assert
            var updatedContainer = await context.Contentores.FindAsync(1);
            Assert.NotNull(updatedContainer);

            // Valida se os campos persistem na base de dados
            Assert.Equal("Localização Nova", updatedContainer.Location);
            Assert.Equal("Plástico", updatedContainer.Type);
            Assert.Equal(Container.ContainerStatus.Broken, updatedContainer.Status);

            // Verifica se a data de atualização foi atualizada
            Assert.True(updatedContainer.LastUpdated > DateTime.Now.AddMinutes(-1));

            // Verifica se o redirecionamento foi efetuado corretamente
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("List", redirectResult.ActionName);
        }

        [Fact]
        public async Task Deactivate_Integration_SetsIsActiveToFalseInDatabase()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var container = new Container
            {
                Id = 10,
                Code = "CNT-010",
                Location = "Localização Antiga",
                Type = "Vidro",
                IsActive = true,
                Status = Container.ContainerStatus.Good
            };
            context.Contentores.Add(container);
            await context.SaveChangesAsync();

            var controller = new ContainersController(context, GetGeoService(), GetHistoryService(context));

            // Act
            var result = await controller.Deactivate(10);

            // Assert
            var deactivatedContainer = await context.Contentores.FindAsync(10);
            Assert.NotNull(deactivatedContainer);

            // Valida se o estado passou a false
            Assert.False(deactivatedContainer.IsActive);

            // Valida o redirecionamento para a lista
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("List", redirectResult.ActionName);
        }
    }
}
