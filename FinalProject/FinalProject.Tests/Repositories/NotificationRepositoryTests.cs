using FinalProject.Domain;
using FinalProject.Infrastructure.DbContexts;
using FinalProject.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace FinalProject.Tests.Repository
{
    public class NotificationRepositoryTests
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationRepository _repository;

        public NotificationRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"NotificationDb_{Guid.NewGuid()}")
                .Options;

            _context = new ApplicationDbContext(options);
            SeedDatabase();

            _repository = new NotificationRepository(_context);
        }

        private void SeedDatabase()
        {
            // Clear existing data
            _context.Notifications.RemoveRange(_context.Notifications);
            _context.SaveChanges();

            // Add test members
            var member1 = new Member { MemberID = 1, Name = "John Doe" };
            var member2 = new Member { MemberID = 2, Name = "Jane Smith" };
            _context.Members.AddRange(member1, member2);

            // Add test notifications
            _context.Notifications.AddRange(
                new Notification
                {
                    NotificationID = 1,
                    MemberID = 1,
                    Message = "Test notification 1",
                    DateSent = DateTime.Now.AddDays(-1),
                    Member = member1
                },
                new Notification
                {
                    NotificationID = 2,
                    MemberID = 1,
                    Message = "Test notification 2",
                    DateSent = DateTime.Now,
                    Member = member1
                },
                new Notification
                {
                    NotificationID = 3,
                    MemberID = 2,
                    Message = "Test notification 3",
                    DateSent = DateTime.Now,
                    Member = member2
                }
            );

            _context.SaveChanges();
        }

        [Fact]
        public async Task GetAllNotificationsAsync_ShouldReturn_AllNotifications()
        {
            // Act
            var result = await _repository.GetAllNotificationsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
        }

        [Fact]
        public async Task GetNotificationByIdAsync_WithValidId_ShouldReturn_Notification()
        {
            // Act
            var result = await _repository.GetNotificationByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.NotificationID.Should().Be(1);
            result.Message.Should().Be("Test notification 1");
        }

        [Fact]
        public async Task GetNotificationByIdAsync_WithInvalidId_ShouldReturn_Null()
        {
            // Act
            var result = await _repository.GetNotificationByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task AddNotificationAsync_ShouldAdd_Notification()
        {
            // Arrange
            var notification = new Notification
            {
                MemberID = 1,
                Message = "New test notification",
                DateSent = DateTime.Now
            };

            // Act
            await _repository.AddNotificationAsync(notification);
            var allNotifications = await _repository.GetAllNotificationsAsync();

            // Assert
            allNotifications.Should().HaveCount(4);
            allNotifications.Should().Contain(n => n.Message == "New test notification");
        }

        [Fact]
        public async Task DeleteNotificationAsync_WithValidId_ShouldDelete_Notification()
        {
            // Act
            await _repository.DeleteNotificationAsync(1);
            var allNotifications = await _repository.GetAllNotificationsAsync();

            // Assert
            allNotifications.Should().HaveCount(2);
            allNotifications.Should().NotContain(n => n.NotificationID == 1);
        }

        [Fact]
        public async Task DeleteNotificationAsync_WithInvalidId_ShouldNot_ThrowException()
        {
            // Act & Assert - Should not throw
            await _repository.DeleteNotificationAsync(999);
        }

        [Fact]
        public async Task GetNotificationsForMemberAsync_ShouldReturn_MemberNotifications()
        {
            // Arrange
            var memberId = 1;

            // Act
            var result = await _repository.GetNotificationsForMemberAsync(memberId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(n => n.MemberID == memberId).Should().BeTrue();
        }

        [Fact]
        public async Task GetNotificationsForMemberAsync_WithNoNotifications_ShouldReturn_EmptyList()
        {
            // Arrange
            var memberId = 999; // Member doesn't exist

            // Act
            var result = await _repository.GetNotificationsForMemberAsync(memberId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task UpdateNotificationAsync_ShouldUpdate_Notification()
        {
            // Arrange
            var notification = await _repository.GetNotificationByIdAsync(1);
            notification.Message = "Updated notification message";

            // Act
            await _repository.UpdateNotificationAsync(notification);
            var updatedNotification = await _repository.GetNotificationByIdAsync(1);

            // Assert
            updatedNotification.Should().NotBeNull();
            updatedNotification.Message.Should().Be("Updated notification message");
        }
    }
}
