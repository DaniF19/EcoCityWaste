using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace EcoCityWasteProjetoESA.Tests
{
    public class NotificationsIntegrationTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public NotificationsIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        [Fact]
        public async Task Index_ReturnsNotificationsPage()
        {
            // Act
            var response = await _client.GetAsync("/Notifications");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task MarkAsRead_Post_RedirectsToIndex()
        {
            // Arrange
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("id","1")
            });

            // Act
            var response = await _client.PostAsync("/Notifications/MarkAsRead", content);

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }

        [Fact]
        public async Task ClearAll_Post_RedirectsToIndex()
        {
            // Act
            var response = await _client.PostAsync("/Notifications/ClearAll", null);

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }

        [Fact]
        public async Task Index_DisplaysNotificationDetails()
        {
            // Act
            var response = await _client.GetAsync("/Notifications");
            var htmlContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Verify the page structure exists
            Assert.Contains("notif-list", htmlContent);
            Assert.Contains("Detalhes da notificação", htmlContent); // The side panel title
        }

    }
}