using Microsoft.AspNetCore.Mvc.Testing;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace EcoCityWasteProjetoESA.Tests
{
    public class RoutesIntegrationTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public RoutesIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        // index
        [Fact]
        public async Task Index_Get_ReturnsRoutesPage()
        {
            var response = await _client.GetAsync("/Routes/Index");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Index_Get_WithStatusFilter_ReturnsFilteredPage()
        {
            var response = await _client.GetAsync("/Routes/Index?statusFilter=Pending");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // create route
        [Fact]
        public async Task Create_Get_ReturnsCreatePage()
        {
            var response = await _client.GetAsync("/Routes/Create");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Create_Post_MissingName_ReturnsCreateViewWithErrors()
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Name", ""),
                new KeyValuePair<string, string>("Description", "No name route"),
                new KeyValuePair<string, string>("ContainerIds", "")
            });

            var response = await _client.PostAsync("/Routes/Create", content);

            // empty Name - stays on Create route page
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // details route
        [Fact]
        public async Task Details_Get_InvalidId_ReturnsNotFound()
        {
            var response = await _client.GetAsync("/Routes/Details/9999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // edit - get / post invalid
        [Fact]
        public async Task Edit_Get_InvalidId_ReturnsNotFound()
        {
            var response = await _client.GetAsync("/Routes/Edit/9999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Edit_Post_InvalidModel_ReturnsView()
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Id", "1"),
                new KeyValuePair<string, string>("Name", ""), // invalid
                new KeyValuePair<string, string>("ContainerIds", "")
            });

            var response = await _client.PostAsync("/Routes/Edit", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // delete route
        [Fact]
        public async Task Delete_InvalidId_ReturnsNotFound()
        {
            var response = await _client.GetAsync("/Routes/Delete/9999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // mark as complete route
        [Fact]
        public async Task Complete_InvalidId_ReturnsNotFound()
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>());
            var response = await _client.PostAsync("/Routes/Complete/9999", content);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // assign route to employee - get / post invalid 
        [Fact]
        public async Task Assign_Get_InvalidId_ReturnsNotFound()
        {
            var response = await _client.GetAsync("/Routes/Assign/9999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Assign_Post_InvalidEmployee_ReturnsNotFound_WhenRouteDoesNotExist()
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("RouteId", "1"),
                new KeyValuePair<string, string>("EmployeeId", "9999")
            });

            var response = await _client.PostAsync("/Routes/Assign", content);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // optimise
        [Fact]
        public async Task Optimise_InvalidId_ReturnsNotFound()
        {
            var response = await _client.GetAsync("/Routes/Optimise/9999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // apply optimization
        [Fact]
        public async Task ApplyOptimisation_InvalidRoute_ReturnsNotFound()
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("routeId", "9999"),
                new KeyValuePair<string, string>("orderedContainerIds", "1")
            });

            var response = await _client.PostAsync("/Routes/ApplyOptimisation", content);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // map
        [Fact]
        public async Task Map_InvalidId_ReturnsNotFound()
        {
            var response = await _client.GetAsync("/Routes/Map/9999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // helper
        private async Task<HttpResponseMessage> CreateRouteAsync(string name, string description)
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Name", name),
                new KeyValuePair<string, string>("Description", description),
                new KeyValuePair<string, string>("ContainerIds", "")
            });
            return await _client.PostAsync("/Routes/Create", content);
        }
    }
}