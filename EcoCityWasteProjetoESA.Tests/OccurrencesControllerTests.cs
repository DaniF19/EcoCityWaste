using EcoCityWaste.Controllers;
using EcoCityWaste.Data;
using EcoCityWaste.Models;
using EcoCityWaste.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EcoCityWasteProjetoESA.Tests
{
    public class OccurrencesControllerTests : IDisposable
    {
        // Pasta temporária criada para cada instância de teste
        private readonly string _tempWebRootPath;

        public OccurrencesControllerTests()
        {
            _tempWebRootPath = Path.Combine(
                Path.GetTempPath(),
                "EcoCityWasteTests_" + Guid.NewGuid().ToString("N")
            );
            Directory.CreateDirectory(_tempWebRootPath);
        }

        public void Dispose()
        {
            // Limpa a pasta temporária após cada teste
            if (Directory.Exists(_tempWebRootPath))
                Directory.Delete(_tempWebRootPath, recursive: true);
        }

        private AppDbContext GetDbContext()
        {
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

        // Preparar o Controlador
        private OccurrencesController SetupController(AppDbContext context)
        {
            var mockEnvironment = new Mock<IWebHostEnvironment>();

            //Usa uma pasta temporária
            // O controller cria sub-pastas (ex: "uploads/") com Directory.CreateDirectory,
            // que lança exceção quando o caminho base não existe.
            mockEnvironment.Setup(m => m.WebRootPath).Returns(_tempWebRootPath);

            var controller = new OccurrencesController(context, mockEnvironment.Object);

            // Simula um utilizador com o ID autenticado
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            }, "mock_auth"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            var tempData = new TempDataDictionary(
                controller.HttpContext,
                Mock.Of<ITempDataProvider>()
            );
            controller.TempData = tempData;

            return controller;
        }

        // Método para criar um mock de IFormFile válido e completo
        private Mock<IFormFile> CreateMockFile(string fileName, string contentType = "image/jpeg")
        {
            var mockFile = new Mock<IFormFile>();
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write("ConteudoFalsoDaImagem");
            writer.Flush();
            ms.Position = 0;

            mockFile.Setup(f => f.OpenReadStream()).Returns(ms);
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(ms.Length);
            mockFile.Setup(f => f.Name).Returns("Photo");

            // ContentType é validado pelo controller para
            // garantir que o ficheiro é uma imagem. Senão, retorna null
            // e pode lançar uma exceção ou falhar a validação.
            mockFile.Setup(f => f.ContentType).Returns(contentType);

            mockFile
                .Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns<Stream, CancellationToken>(async (stream, _) =>
                {
                    ms.Position = 0;
                    await ms.CopyToAsync(stream);
                });

            return mockFile;
        }

        [Fact]
        public async Task Status_ReturnsUserOccurrences()
        {
            // Arrange
            var context = GetDbContext();
            var controller = SetupController(context);

            // Adiciona ocorrências à BD
            context.Occurrences.Add(new Occurrence { Id = 1, UserId = 1, ContainerCode = "CNT-00500", Status = "Pendente", OccurrenceType = "Lixo no Chão", Description = "Teste", ImagePath = "/uploads/teste.jpg" });
            context.Occurrences.Add(new Occurrence { Id = 2, UserId = 2, ContainerCode = "CNT-00200", Status = "Resolvido", OccurrenceType = "Contentor Partido", Description = "Teste", ImagePath = "/uploads/teste.jpg" });
            await context.SaveChangesAsync();

            // Act
            var result = await controller.Status() as ViewResult;
            var model = result?.Model as IEnumerable<Occurrence>;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(model);
            Assert.Single(model); // Garante que só vem a do user autenticado
            Assert.Equal("CNT-00500", model.First().ContainerCode);
        }

        [Fact]
        public async Task Report_InvalidModel_ReturnsView()
        {
            // Arrange
            var context = GetDbContext();
            var controller = SetupController(context);

            var invalidModel = new ReportOccurrenceViewModel
            {
                ContainerCode = "",
                OccurrenceType = "",
                Description = "",
                Photo = null
            };
            controller.ModelState.AddModelError("ContainerCode", "Required");

            // Act
            var result = await controller.Report(invalidModel) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(invalidModel, result.Model);
            Assert.Empty(context.Occurrences); // Garante que nada foi gravado
        }

        [Fact]
        // O controller após sucesso faz return View() com ViewBag.Success,
        public async Task Report_ValidSubmission_SavesAndShowsSuccess()
        {
            // Arrange
            var context = GetDbContext();
            var controller = SetupController(context);

            var mockFile = CreateMockFile("lixo_no_chao.jpg");

            var validModel = new ReportOccurrenceViewModel
            {
                ContainerCode = "CNT-00500",
                OccurrenceType = "Lixo no Chão",
                Description = "Sacos fora do contentor na Praça do Bocage",
                Photo = mockFile.Object
            };

            // Inspecionar o resultado do Controller para obter detalhes de erros caso haja falha
            var result = await controller.Report(validModel);

            // Se o controlador devolveu uma View com erro
            if (result is ViewResult && controller.ViewBag.Error != null)
            {
                Assert.Fail($"O Controller devolveu a View com o erro: {controller.ViewBag.Error}");
            }

            // O controller devolve View() com ViewBag.Success, não um redirect
            Assert.IsType<ViewResult>(result);

            // A mensagem de sucesso está no ViewBag.Success, não em TempData["SuccessMessage"]
            Assert.NotNull(controller.ViewBag.Success);

            var savedOccurrence = await context.Occurrences.FirstOrDefaultAsync();
            Assert.NotNull(savedOccurrence);
            Assert.Equal("CNT-00500", savedOccurrence.ContainerCode);
            Assert.Equal(1, savedOccurrence.UserId);
            Assert.Contains("lixo_no_chao.jpg", savedOccurrence.ImagePath);
        }
    }
}