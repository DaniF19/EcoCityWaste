using EcoCityWaste.Controllers;
using EcoCityWaste.Data;
using EcoCityWaste.Models;
using EcoCityWaste.ViewModels;
using EcoCityWaste.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Security.Claims;

namespace EcoCityWasteProjetoESA.Tests
{
    public class OccurrencesControllerTests : IDisposable
    {
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
        private OccurrencesController SetupController(AppDbContext context, string userRole = "Cidadao")
        {
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            mockEnvironment.Setup(m => m.WebRootPath).Returns(_tempWebRootPath);

            // IConfiguration real com valores em memória para evitar NullReferenceException
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "EmailSettings:SmtpHost", "localhost" },
                    { "EmailSettings:SmtpPort", "25" },
                    { "EmailSettings:SenderEmail", "test@test.com" },
                    { "EmailSettings:SenderPassword", "" }
                })
                .Build();

            var notificationService = new NotificationService(context);

            var controller = new OccurrencesController(
                context,
                mockEnvironment.Object,
                configuration,
                notificationService
            );

            // Criar o utilizador autenticado com a Role correta
            if (!context.Users.Any(u => u.Id == 1))
            {
                context.Users.Add(new User { Id = 1, Role = userRole, Username = "User Teste", Email = "teste@ecocity.com", PasswordHash = "hash_teste" });
                context.SaveChanges();
            }

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
            var context = GetDbContext();
            var controller = SetupController(context);

            context.Occurrences.Add(new Occurrence { Id = 1, UserId = 1, ContainerCode = "CNT-00500", Status = "Pendente", OccurrenceType = "Lixo no Chão", Description = "Teste", ImagePath = "/uploads/teste.jpg" });
            context.Occurrences.Add(new Occurrence { Id = 2, UserId = 2, ContainerCode = "CNT-00200", Status = "Resolvido", OccurrenceType = "Contentor Partido", Description = "Teste", ImagePath = "/uploads/teste.jpg" });
            await context.SaveChangesAsync();

            var result = await controller.Status() as ViewResult;
            var model = result?.Model as IEnumerable<Occurrence>;

            Assert.NotNull(result);
            Assert.NotNull(model);
            Assert.Single(model);
            Assert.Equal("CNT-00500", model.First().ContainerCode);
        }

        [Fact]
        public async Task Report_InvalidModel_ReturnsView()
        {
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

            var result = await controller.Report(invalidModel) as ViewResult;

            Assert.NotNull(result);
            var returnedModel = result.Model as ReportOccurrenceViewModel;
            Assert.NotNull(returnedModel);
            Assert.Equal(invalidModel.ContainerCode, returnedModel.ContainerCode);
            Assert.Equal(invalidModel.OccurrenceType, returnedModel.OccurrenceType);
            Assert.Equal(invalidModel.Description, returnedModel.Description);
            Assert.Null(returnedModel.Photo);
            Assert.Empty(context.Occurrences);
        }

        [Fact]
        public async Task Report_ValidSubmission_SavesAndShowsSuccess()
        {
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

            var result = await controller.Report(validModel);

            if (result is ViewResult && controller.ViewBag.Error != null)
            {
                Assert.Fail($"O Controller devolveu a View com o erro: {controller.ViewBag.Error}");
            }

            Assert.IsType<ViewResult>(result);
            Assert.NotNull(controller.ViewBag.Success);

            var savedOccurrence = await context.Occurrences.FirstOrDefaultAsync();
            Assert.NotNull(savedOccurrence);
            Assert.Equal("CNT-00500", savedOccurrence.ContainerCode);
            Assert.Equal(1, savedOccurrence.UserId);
            Assert.Contains("lixo_no_chao.jpg", savedOccurrence.ImagePath);
        }

        // Testes Sprint 4

        [Fact]
        public async Task Status_HidesResolvedOccurrencesOlderThan30Days()
        {
            var context = GetDbContext();
            var controller = SetupController(context); // Utilizador Cidadão (Id 1)

            var oldResolved = new Occurrence { Id = 101, UserId = 1, ContainerCode = "CNT-00500", Description = "Teste", Status = "Resolvido", OccurrenceType = "Lixo no Chão", LastUpdatedAt = DateTime.Now.AddDays(-31) };
            var recentResolved = new Occurrence { Id = 102, UserId = 1, ContainerCode = "CNT-00500", Description = "Teste", Status = "Resolvido", OccurrenceType = "Lixo no Chão", LastUpdatedAt = DateTime.Now.AddDays(-29) };
            var pending = new Occurrence { Id = 103, UserId = 1, ContainerCode = "CNT-00500", Description = "Teste", Status = "Pendente", OccurrenceType = "Lixo no Chão", ReportDate = DateTime.Now.AddDays(-40) };

            context.Occurrences.AddRange(oldResolved, recentResolved, pending);
            await context.SaveChangesAsync();

            var result = await controller.Status() as ViewResult;
            var model = result?.Model as IEnumerable<Occurrence>;

            Assert.NotNull(model);
            Assert.Equal(2, model.Count());
            Assert.DoesNotContain(model, o => o.Id == 101);
            Assert.Contains(model, o => o.Id == 102);
            Assert.Contains(model, o => o.Id == 103);
        }

        [Fact]
        public async Task AssignedIncidents_FiltersAndSorting_ReturnsCorrectResults()
        {
            var context = GetDbContext();
            var controller = SetupController(context, "Funcionario"); // ID 1 é Funcionário

            var occ1 = new Occurrence { Id = 201, AssignedEmployeeId = 1, ContainerCode = "CNT-00500", Description = "Teste", Status = "Pendente", OccurrenceType = "Lixo no Chão", ReportDate = DateTime.Now.AddDays(-2) };
            var occ2 = new Occurrence { Id = 202, AssignedEmployeeId = 1, ContainerCode = "CNT-00200", Description = "Teste", Status = "EmResolucao", OccurrenceType = "Queimado", ReportDate = DateTime.Now.AddDays(-5) };
            var occ3 = new Occurrence { Id = 203, AssignedEmployeeId = 1, ContainerCode = "CNT-00500", Description = "Teste", Status = "EmResolucao", OccurrenceType = "Vandalismo", ReportDate = DateTime.Now.AddDays(-1) };

            context.Occurrences.AddRange(occ1, occ2, occ3);
            await context.SaveChangesAsync();

            // Filtro de Data e Ordenação
            var resultDate = await controller.AssignedIncidents(null, null, DateTime.Now.AddDays(-3), null) as ViewResult;
            var modelDate = resultDate?.Model as IEnumerable<Occurrence>;

            Assert.NotNull(modelDate);
            Assert.Equal(2, modelDate.Count());
            Assert.Equal(203, modelDate.First().Id); // A mais recente primeiro
            Assert.Equal(201, modelDate.Last().Id);

            // Filtros Combinados (Estado + Tipo)
            var resultCombined = await controller.AssignedIncidents("EmResolucao", "Queimado", null, null) as ViewResult;
            var modelCombined = resultCombined?.Model as IEnumerable<Occurrence>;

            Assert.NotNull(modelCombined);
            Assert.Single(modelCombined);
            Assert.Equal(202, modelCombined.First().Id);
        }

        [Fact]
        public async Task Assign_UpdatesLastUpdatedAt_Successfully()
        {
            var context = GetDbContext();
            var controller = SetupController(context, "Admin"); // Quem atribui é o Admin (ID 1)

            context.Users.Add(new User { Id = 4, Role = "Cidadao", Username = "Ana", Email = "ana@ecocity.com", PasswordHash = "hash_ana" });
            context.Users.Add(new User { Id = 5, Role = "Funcionario", Username = "Carlos", Email = "carlos@ecocity.com", PasswordHash = "hash_carlos" });

            var dataAntiga = DateTime.Now.AddDays(-5);
            var occurrence = new Occurrence
            {
                Id = 300,
                UserId = 4,
                ContainerCode = "CNT-00500",
                Description = "Teste de atribuição",
                Status = "Pendente",
                OccurrenceType = "Lixo",
                ReportDate = dataAntiga,
                LastUpdatedAt = dataAntiga
            };

            context.Occurrences.Add(occurrence);
            await context.SaveChangesAsync();

            var assignModel = new AssignOccurrenceViewModel
            {
                SelectedOccurrenceId = 300,
                SelectedEmployeeId = 5
            };

            await controller.Assign(assignModel);

            var updatedOcc = await context.Occurrences.FindAsync(300);

            Assert.NotNull(updatedOcc.LastUpdatedAt);
            Assert.True(updatedOcc.LastUpdatedAt > dataAntiga, "A data LastUpdatedAt não avançou.");

            var diferencaTempo = DateTime.Now - updatedOcc.LastUpdatedAt.Value;
            Assert.True(diferencaTempo.TotalMinutes < 1, "A data LastUpdatedAt não corresponde ao momento atual.");
        }
    }
}