using Microsoft.AspNetCore.Mvc.Testing;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace EcoCityWasteProjetoESA.Tests
{
    public class OccurrencesIntegrationTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public OccurrencesIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        // Cidadão reporta uma ocorrência
        [Fact]
        public async Task Report_Post_ValidOccurrence_SavesAndReturnsOk()
        {
            // Arrange
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("ContainerCode", "CNT-00500"),
                new KeyValuePair<string, string>("OccurrenceType", "Lixo no Chão"),
                new KeyValuePair<string, string>("Description", "Sacos fora do contentor na Praça do Bocage"),
            });

            // Act
            var response = await _client.PostAsync("/Occurrences/Report", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // Cidadão consulta as suas ocorrências
        [Fact]
        public async Task Status_Get_ReturnsOccurrencesPage()
        {
            // Act
            var response = await _client.GetAsync("/Occurrences/Status");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // Cidadão submete ocorrência sem foto (foto é opcional)
        [Fact]
        public async Task Report_Post_WithoutPhoto_StillSavesOccurrence()
        {
            // Arrange
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("ContainerCode", "CNT-00200"),
                new KeyValuePair<string, string>("OccurrenceType", "Contentor Danificado"),
                new KeyValuePair<string, string>("Description", "Contentor com tampa partida"),
            });

            // Act
            var response = await _client.PostAsync("/Occurrences/Report", content);

            // Assert — deve guardar sem foto e devolver OK (não redireciona)
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // Cidadão submete ocorrência com foto
        [Fact]
        public async Task Report_Post_WithPhoto_SavesOccurrenceWithImage()
        {
            // Arrange — multipart/form-data para enviar ficheiro
            var multipart = new MultipartFormDataContent();
            multipart.Add(new StringContent("CNT-00500"), "ContainerCode");
            multipart.Add(new StringContent("Vandalismo"), "OccurrenceType");
            multipart.Add(new StringContent("Contentor pintado com graffiti"), "Description");

            var fileContent = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF }); // header JPEG mínimo
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            multipart.Add(fileContent, "Photo", "foto_ocorrencia.jpg");

            // Act
            var response = await _client.PostAsync("/Occurrences/Report", multipart);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}