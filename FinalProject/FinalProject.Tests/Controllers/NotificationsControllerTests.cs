using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FinalProject.Application.Dto.Notification;
using FinalProject.Tests.Helpers;
using FinalProject.API;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Newtonsoft.Json;
using System.Linq;

namespace FinalProject.Tests.Controllers
{
    public class NotificationsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory<Program> _factory;
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public NotificationsControllerIntegrationTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetAllNotifications_ShouldReturn_ListOfNotifications()
        {
            // Act
            var response = await _client.GetAsync("/api/notifications");
            var content = await response.Content.ReadAsStringAsync();
            var notifications = JsonConvert.DeserializeObject<List<NotificationDetailsDto>>(content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotEmpty(notifications);
        }

        [Fact]
        public async Task GetNotificationById_WithValidId_ShouldReturn_Notification()
        {
            // Arrange
            var validId = 1;

            // Act
            var response = await _client.GetAsync($"/api/notifications/{validId}");
            var content = await response.Content.ReadAsStringAsync();
            var notification = JsonConvert.DeserializeObject<NotificationDetailsDto>(content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(validId, notification.NotificationID);
        }

        [Fact]
        public async Task GetNotificationById_WithInvalidId_ShouldReturn_NotFound()
        {
            // Arrange
            var invalidId = 999;

            // Act
            var response = await _client.GetAsync($"/api/notifications/{invalidId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateNotification_WithValidData_ShouldReturn_CreatedResponse()
        {
            // Arrange
            var createDto = new CreateNotificationDto
            {
                MemberID = 1,
                Message = "Test notification from integration test",
                DateSent = DateTime.Now
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/notifications", createDto);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task DeleteNotification_WithValidId_ShouldReturn_NoContent()
        {
            // Arrange - First create a notification to delete
            var createDto = new CreateNotificationDto
            {
                MemberID = 1,
                Message = "Notification to delete",
                DateSent = DateTime.Now
            };

            var createResponse = await _client.PostAsJsonAsync("/api/notifications", createDto);
            var createContent = await createResponse.Content.ReadAsStringAsync();

            // Get all notifications and find the one we just created
            var getAllResponse = await _client.GetAsync("/api/notifications");
            var getAllContent = await getAllResponse.Content.ReadAsStringAsync();
            var allNotifications = JsonConvert.DeserializeObject<List<NotificationDetailsDto>>(getAllContent);
            var notificationToDelete = allNotifications.LastOrDefault();

            // Act
            var deleteResponse = await _client.DeleteAsync($"/api/notifications/{notificationToDelete.NotificationID}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        }

        [Fact]
        public async Task GetNotificationsForMember_WithValidId_ShouldReturn_MemberNotifications()
        {
            // Arrange
            var memberId = 1;

            // Act
            var response = await _client.GetAsync($"/api/notifications/member/{memberId}");
            var content = await response.Content.ReadAsStringAsync();
            var notifications = JsonConvert.DeserializeObject<List<NotificationDetailsDto>>(content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotEmpty(notifications);
            Assert.All(notifications, n => Assert.Equal(memberId, n.MemberID));
        }

        [Fact]
        public async Task GetNotificationsByMemberName_WithValidName_ShouldReturn_MemberNotifications()
        {
            // Arrange
            var memberName = "John Doe";

            // Act
            var response = await _client.GetAsync($"/api/notifications/member/name/{memberName}");
            var content = await response.Content.ReadAsStringAsync();
            var notifications = JsonConvert.DeserializeObject<List<NotificationDetailsDto>>(content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotEmpty(notifications);
        }

        [Fact]
        public async Task GetNotificationsByMemberName_WithInvalidName_ShouldReturn_NotFound()
        {
            // Arrange
            var invalidName = "Nonexistent User";

            // Act
            var response = await _client.GetAsync($"/api/notifications/member/name/{invalidName}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task NotifyForDueBooks_ShouldReturn_SuccessMessage()
        {
            // Act
            var response = await _client.PostAsync("/api/notifications/notify-due-books", null);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<dynamic>(content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Notifications for due books", (string)result.Message);
        }

        [Fact]
        public async Task NotifyForOverdueBooks_ShouldReturn_SuccessMessage()
        {
            // Act
            var response = await _client.PostAsync("/api/notifications/notify-overdue-books", null);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<dynamic>(content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Notifications for overdue books", (string)result.Message);
        }

        [Fact]
        public async Task NotifyForFinePayment_WithValidId_ShouldReturn_SuccessMessage()
        {
            // Arrange
            var fineId = 2; // ID of a paid fine from our seed data

            // Act
            var response = await _client.PostAsync($"/api/notifications/notify-fine-payment/{fineId}", null);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<dynamic>(content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Notification for fine payment", (string)result.Message);
        }

        [Fact]
        public async Task PerformPeriodicChecks_ShouldReturn_SuccessMessage()
        {
            // Act
            var response = await _client.PostAsync("/api/notifications/perform-periodic-checks", null);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<dynamic>(content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Periodic checks completed", (string)result.Message);
        }
    }
}
