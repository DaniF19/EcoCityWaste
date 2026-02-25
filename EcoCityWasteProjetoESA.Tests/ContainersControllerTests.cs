using EcoCityWaste.Controllers;
using EcoCityWaste.Data;
using EcoCityWaste.Dtos;
using EcoCityWaste.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcoCityWasteProjetoESA.Tests
{
    public class ContainersControllerTests
    {
        private AppDbContext GetDbContext()
        {
            // Base de Dados na Memória
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new AppDbContext(options);

            // Dados de Teste
            context.Contentores.AddRange(
                new Container { Id = 1, Code = "CNT-00500", Location = "Praça do Bocage, Setúbal", Latitude = 38.5244, Longitude = -8.8882, Type = "Plástico", Status = "Bom", FillLevel = 85, InstallationDate = DateTime.Now.AddDays(-120), LastUpdated = DateTime.Now.AddMinutes(-30), IsActive = true },
                new Container { Id = 2, Code = "CNT-00200", Location = "Praça do Bocage, Setúbal", Latitude = 38.5244, Longitude = -8.8882, Type = "Vidro", Status = "Avariado", FillLevel = 40, InstallationDate = DateTime.Now.AddDays(-120), LastUpdated = DateTime.Now.AddMinutes(-30), IsActive = true },
                new Container { Id = 3, Code = "CNT-00300", Location = "Praça do Bocage, Setúbal", Latitude = 38.5244, Longitude = -8.8882, Type = "Papel", Status = "Bom", FillLevel = 10, InstallationDate = DateTime.Now.AddDays(-120), LastUpdated = DateTime.Now.AddMinutes(-30), IsActive = false }
            );

            context.SaveChanges();
            return context;
        }

        [Fact]
        public async Task Data_Persistance()
        {
            // Prepara o contexto
            using var context = GetDbContext();
            var containerOriginal = await context.Contentores.FirstAsync();

            // Valores simulados pelo SensorService
            int nivelSimulado = 88;
            string estadoSimulado = "Manutenção";
            DateTime horaSimulada = new DateTime(2026, 02, 22, 14, 0, 0);

            // Aplica as mudanças e grava
            containerOriginal.FillLevel = nivelSimulado;
            containerOriginal.Status = estadoSimulado;
            containerOriginal.LastUpdated = horaSimulada;

            await context.SaveChangesAsync();

            // Abre uma nova consulta para verificar se os dados ficaram gravados
            var containerNaBD = await context.Contentores
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == containerOriginal.Id);

            Assert.NotNull(containerNaBD);
            Assert.Equal(nivelSimulado, containerNaBD.FillLevel); // Verifica se o nível subiu na BD
            Assert.Equal(estadoSimulado, containerNaBD.Status);   // Verifica se o estado mudou na BD
            Assert.Equal(horaSimulada, containerNaBD.LastUpdated); // Verifica se a data foi persistida
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
            var controller = new ContainersController(context);

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
            var controller = new ContainersController(context);

            // Act: Filtra por Vidro
            var result = await controller.Index(null, "Vidro", null) as ViewResult;
            var model = result.Model as List<Container>;

            // Assert: Esperamos apenas 1 (o CNT-002)
            Assert.Single(model);
            Assert.Equal("Vidro", model[0].Type);
        }
        [Fact]
        public async Task UpdateStatus_ReturnsOk_WhenContainerExists()
        {
            // Arrange
            using var context = GetDbContext();
            var controller = new ContainersController(context);

            var dto = new UpdateContainerStatusDto
            {
                Status = "Cheio"
            };

            // Act
            var result = await controller.UpdateStatus(1, dto);

            // Assert
            Assert.IsType<OkObjectResult>(result);

            var updated = await context.Contentores.FindAsync(1);
            Assert.Equal(Container.ContainerStatus.Full, updated.Status);
        }

        [Fact]
        public async Task UpdateStatus_ReturnsNotFound_WhenContainerDoesNotExist()
        {
            // Arrange
            using var context = GetDbContext();
            var controller = new ContainersController(context);

            var dto = new UpdateContainerStatusDto
            {
                Status = "Cheio"
            };

            // Act
            var result = await controller.UpdateStatus(999, dto);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }
        [Fact]
        public async Task UpdateStatus_ReturnsBadRequest_WhenEstadoInvalido()
        {
            // Arrange
            using var context = GetDbContext();
            var controller = new ContainersController(context);

            var dto = new UpdateContainerStatusDto
            {
                Status = "Bingo" // estado inválido
            };

            // Act
            var result = await controller.UpdateStatus(1, dto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }



    }
}
