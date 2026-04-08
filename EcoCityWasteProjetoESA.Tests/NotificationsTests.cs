using EcoCityWaste.Controllers;
using EcoCityWaste.Data;
using EcoCityWaste.Models;
using EcoCityWaste.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace EcoCityWasteProjetoESA.Tests
{
    public class NotificationsControllerTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = new AppDbContext(options);

            context.Notifications.AddRange(
                new Notification
                {
                    Id = 1,
                    ContainerId = 1,
                    Message = "Contentor CNT-001 Atingiu 100%",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                },
                new Notification
                {
                    Id = 2,
                    ContainerId = 2,
                    Message = "Contentor CNT-002 Atingiu 95%",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                }
            );

            context.SaveChanges();
            return context;
        }

        [Fact]
        public async Task MarkAsRead_ValidNotification_UpdatesIsRead()
        {
            // Arrange
            using var context = GetDbContext();
            var controller = new NotificationsController(context);

            // Act
            var result = await controller.MarkAsRead(1);

            // Assert
            var notification = await context.Notifications.FindAsync(1);

            Assert.True(notification.IsRead);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }

        // TEST 2
        [Fact]
        public async Task NotificationService_DoesNotCreateDuplicateNotification()
        {
            // Arrange
            using var context = GetDbContext();

            var service = new NotificationService(context);

            var container = new Container
            {
                Id = 1,
                Code = "CNT-001",
                FillLevel = 100
            };

            // ja existe na BD: ContainerId = 1

            // Act
            await service.CreateCriticalLevelNotification(container);

            // Assert
            var notifications = await context.Notifications
                .CountAsync(n => n.ContainerId == 1);

            Assert.Equal(1, notifications);
        }

        [Fact]
        public async Task NotificationService_CreatesNotification_WhenContainerIsCritical()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new AppDbContext(options);

            context.Users.Add(new User
            {
                Id = 1,
                Username = "admin",
                Role = "Admin",
                Email = "admin@teste.com",
                PasswordHash = "passwordHash"
            });

            await context.SaveChangesAsync();

            var service = new NotificationService(context);

            var container = new Container
            {
                Id = 1,
                Code = "CNT-001",
                FillLevel = 100
            };

            // Act
            await service.CreateCriticalLevelNotification(container);

            // Assert
            var notification = await context.Notifications.FirstOrDefaultAsync();

            Assert.NotNull(notification);
            Assert.Equal(container.Id, notification.ContainerId);
            Assert.Contains("CNT-001", notification.Message);
        }
    }
}