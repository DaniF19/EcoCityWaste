using EcoCityWaste.Controllers;
using EcoCityWaste.Data;
using EcoCityWaste.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EcoCityWaste.Models;

namespace EcoCityWasteProjetoESA.Tests
{
    public class DashboardTests
    {
        [Fact]
        public async Task Dashboard_Deve_Calcular_Contentores_Criticos_Corretamente()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDB_ContentoresCriticos")
                .Options;

            using var context = new AppDbContext(options);

            context.Contentores.AddRange(
                new Container
                {
                    Code = "CNT-001",
                    Location = "Rua A",
                    Type = "Vidro",
                    FillLevel = 95,
                    Status = Container.ContainerStatus.Good,
                    Latitude = 0,
                    Longitude = 0,
                    InstallationDate = DateTime.Today,
                    LastUpdated = DateTime.Today,
                    IsActive = true
                },
                new Container
                {
                    Code = "CNT-002",
                    Location = "Rua B",
                    Type = "Papel",
                    FillLevel = 90,
                    Status = Container.ContainerStatus.Good,
                    Latitude = 0,
                    Longitude = 0,
                    InstallationDate = DateTime.Today,
                    LastUpdated = DateTime.Today,
                    IsActive = true
                },
                new Container
                {
                    Code = "CNT-003",
                    Location = "Rua C",
                    Type = "Vidro",
                    FillLevel = 89,
                    Status = Container.ContainerStatus.Good,
                    Latitude = 0,
                    Longitude = 0,
                    InstallationDate = DateTime.Today,
                    LastUpdated = DateTime.Today,
                    IsActive = true
                }
            );

            await context.SaveChangesAsync();

            var controller = new DashboardController(context);

            // Act
            var result = await controller.Index() as ViewResult;
            var model = result.Model as DashboardViewModel;

            // Assert
            Assert.Equal(2, model.ContentoresCriticos);
        }
        [Fact]
        public async Task Dashboard_Deve_Contar_Ocorrencias_De_Hoje()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("TestDB_OcorrenciasHoje")
                .Options;

            using var context = new AppDbContext(options);

            var hoje = DateTime.Today;

            context.Occurrences.AddRange(
                new Occurrence
                {
                    ContainerCode = "CNT-001",
                    OccurrenceType = "Tipo1",
                    Description = "Desc",
                    ReportDate = hoje,
                    Status = "Pendente",
                    UserId = 1
                },
                new Occurrence
                {
                    ContainerCode = "CNT-002",
                    OccurrenceType = "Tipo1",
                    Description = "Desc",
                    ReportDate = hoje.AddHours(5),
                    Status = "Pendente",
                    UserId = 1
                },
                new Occurrence
                {
                    ContainerCode = "CNT-003",
                    OccurrenceType = "Tipo1",
                    Description = "Desc",
                    ReportDate = hoje.AddDays(-1),
                    Status = "Pendente",
                    UserId = 1
                }
            );

            await context.SaveChangesAsync();

            var controller = new DashboardController(context);

            // Act
            var result = await controller.Index() as ViewResult;
            var model = result.Model as DashboardViewModel;

            // Assert
            Assert.Equal(2, model.OcorrenciasHoje);
        }

        [Fact]
        public async Task Dashboard_Deve_Calcular_Nivel_Medio_Por_Tipo()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("TestDB_MediaPorTipo")
                .Options;

            using var context = new AppDbContext(options);

            context.Contentores.AddRange(
                new Container
                {
                    Code = "C1",
                    Location = "L1",
                    Type = "Vidro",
                    FillLevel = 50,
                    Status = Container.ContainerStatus.Good,
                    Latitude = 0,
                    Longitude = 0,
                    InstallationDate = DateTime.Now,
                    LastUpdated = DateTime.Now,
                    IsActive = true
                },
                new Container
                {
                    Code = "C2",
                    Location = "L2",
                    Type = "Vidro",
                    FillLevel = 100,
                    Status = Container.ContainerStatus.Good,
                    Latitude = 0,
                    Longitude = 0,
                    InstallationDate = DateTime.Now,
                    LastUpdated = DateTime.Now,
                    IsActive = true
                },
                new Container
                {
                    Code = "C3",
                    Location = "L3",
                    Type = "Papel",
                    FillLevel = 80,
                    Status = Container.ContainerStatus.Good,
                    Latitude = 0,
                    Longitude = 0,
                    InstallationDate = DateTime.Now,
                    LastUpdated = DateTime.Now,
                    IsActive = true
                }
            );

            await context.SaveChangesAsync();

            var controller = new DashboardController(context);

            var result = await controller.Index() as ViewResult;
            var model = result.Model as DashboardViewModel;

            Assert.Equal(75, model.NivelMedioPorTipo["Vidro"]);
            Assert.Equal(80, model.NivelMedioPorTipo["Papel"]);
        }


        [Fact]
        public async Task Dashboard_Deve_Calcular_Percentagem_Criticos()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("TestDB_PercentagemCriticos")
                .Options;

            using var context = new AppDbContext(options);

            context.Contentores.AddRange(
                new Container
                {
                    Code = "C1",
                    Location = "L1",
                    Type = "Vidro",
                    FillLevel = 95,
                    Status = Container.ContainerStatus.Good,
                    Latitude = 0,
                    Longitude = 0,
                    InstallationDate = DateTime.Now,
                    LastUpdated = DateTime.Now,
                    IsActive = true
                },
                new Container
                {
                    Code = "C2",
                    Location = "L2",
                    Type = "Vidro",
                    FillLevel = 90,
                    Status = Container.ContainerStatus.Good,
                    Latitude = 0,
                    Longitude = 0,
                    InstallationDate = DateTime.Now,
                    LastUpdated = DateTime.Now,
                    IsActive = true
                },
                new Container
                {
                    Code = "C3",
                    Location = "L3",
                    Type = "Vidro",
                    FillLevel = 20,
                    Status = Container.ContainerStatus.Good,
                    Latitude = 0,
                    Longitude = 0,
                    InstallationDate = DateTime.Now,
                    LastUpdated = DateTime.Now,
                    IsActive = true
                },
                new Container
                {
                    Code = "C4",
                    Location = "L4",
                    Type = "Vidro",
                    FillLevel = 10,
                    Status = Container.ContainerStatus.Good,
                    Latitude = 0,
                    Longitude = 0,
                    InstallationDate = DateTime.Now,
                    LastUpdated = DateTime.Now,
                    IsActive = true
                }
            );

            await context.SaveChangesAsync();

            var controller = new DashboardController(context);

            var result = await controller.Index() as ViewResult;
            var model = result.Model as DashboardViewModel;

            Assert.Equal(50, model.PercentagemCriticos);
        }

    }
}
