using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EcoCityWasteProjetoESA.Tests
{
    public class ContainersIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public ContainersIntegrationTests(CustomWebApplicationFactory factory)
        {
            //_client = factory.CreateClient();
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        [Fact]
        public async Task Register_Post_CreatesContainer()
        {
            // Arrange
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("Location","Integration Street"),
                new KeyValuePair<string,string>("Type","Vidro"),
                new KeyValuePair<string,string>("Status","Bom")
            });

            // Act
            var response = await _client.PostAsync("/Containers/Register", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Edit_Post_UpdatesContainer()
        {
            // First create container
            var createContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("Location","Before Edit"),
                new KeyValuePair<string,string>("Type","Vidro"),
                new KeyValuePair<string,string>("Status","Bom")
            });

            await _client.PostAsync("/Containers/Register", createContent);

            // Edit container with Id=1
            var editContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("Id","1"),
                new KeyValuePair<string,string>("Location","After Edit"),
                new KeyValuePair<string,string>("Type","Plástico"),
                new KeyValuePair<string,string>("Status","Manutenção")
            });

            var response = await _client.PostAsync("/Containers/Edit", editContent);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }
    }
}
