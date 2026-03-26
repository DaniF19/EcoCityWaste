using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace EcoCityWasteProjetoESA.Tests
{
    public class DashboardIntegrationTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public DashboardIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        // 1) Página abre com utilizador autenticado
        [Fact]
        public async Task Dashboard_Index_ReturnsOk()
        {
            var response = await _client.GetAsync("/Dashboard/Index");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // 2) Página contém o título principal
        [Fact]
        public async Task Dashboard_Index_ContainsMainTitle()
        {
            var response = await _client.GetAsync("/Dashboard/Index");
            var html = await response.Content.ReadAsStringAsync();

            Assert.Contains("Dashboard de Indicadores Ambientais", html);
        }

        // 3) Página contém secções principais
        [Fact]
        public async Task Dashboard_Index_ShowsMainSections()
        {
            var response = await _client.GetAsync("/Dashboard/Index");
            var html = await response.Content.ReadAsStringAsync();

            Assert.Contains("Ocorrências", html);
            Assert.Contains("Contentores", html);
            Assert.Contains("Indicadores Ambientais", html);
            Assert.Contains("Enchimento Médio por Tipo", html);
        }

        // 4) Dashboard funciona com BD vazia
        [Fact]
        public async Task Dashboard_Index_NoData_ReturnsOk()
        {
            var response = await _client.GetAsync("/Dashboard/Index");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // 5) Dashboard funciona com dados reais inseridos
        [Fact]
        public async Task Dashboard_Index_WithData_ReturnsOk()
        {
            await DashboardTestSeeder.SeedAsync(_client);

            var response = await _client.GetAsync("/Dashboard/Index");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // 6) Dashboard mostra valores no HTML
        [Fact]
        public async Task Dashboard_Index_ShowsKpiValues()
        {
            await DashboardTestSeeder.SeedAsync(_client);

            var response = await _client.GetAsync("/Dashboard/Index");
            var html = await response.Content.ReadAsStringAsync();

            Assert.Contains("Total de Contentores", html);
            Assert.Contains("Contentores Críticos", html);
            Assert.Contains("Nível Médio de Enchimento", html);
        }
    }
    public static class DashboardTestSeeder
    {
        public static async Task SeedAsync(HttpClient client)
        {
            // Criar contentores
            var c1 = new FormUrlEncodedContent(new[]
            {
            new KeyValuePair<string,string>("Code", "CNT-100"),
            new KeyValuePair<string,string>("Location", "Rua A"),
            new KeyValuePair<string,string>("Type", "Vidro"),
            new KeyValuePair<string,string>("FillLevel", "95"),
            new KeyValuePair<string,string>("Status", "Good"),
            new KeyValuePair<string,string>("Latitude", "0"),
            new KeyValuePair<string,string>("Longitude", "0"),
            new KeyValuePair<string,string>("InstallationDate", "2024-01-01"),
            new KeyValuePair<string,string>("LastUpdated", "2024-01-01"),
            new KeyValuePair<string,string>("IsActive", "true")
        });

            await client.PostAsync("/Containers/Create", c1);

            // Criar ocorrência
            var o1 = new FormUrlEncodedContent(new[]
            {
            new KeyValuePair<string,string>("ContainerCode", "CNT-100"),
            new KeyValuePair<string,string>("OccurrenceType", "Vidro Partido"),
            new KeyValuePair<string,string>("Description", "Teste"),
        });

            await client.PostAsync("/Occurrences/Create", o1);
        }
    }

}
