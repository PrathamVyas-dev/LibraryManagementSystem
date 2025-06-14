using FinalProject.Application.Dto.Notification;
using FinalProject.Application.Interfaces;
using FinalProject.Application.Services;
using FinalProject.Domain;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace FinalProject.Tests.Services
{
    public class NotificationServiceTests
    {
        private readonly Mock<INotificationRepository> _notificationRepositoryMock;
        private readonly Mock<IBorrowingTransactionRepository> _borrowingRepositoryMock;
        private readonly Mock<IFineRepository> _fineRepositoryMock;
        private readonly NotificationService _service;

        public NotificationServiceTests()
        {
            _notificationRepositoryMock = new Mock<INotificationRepository>();
            _borrowingRepositoryMock = new Mock<IBorrowingTransactionRepository>();
            _fineRepositoryMock = new Mock<IFineRepository>();

            _service = new NotificationService(
                _notificationRepositoryMock.Object,
                _borrowingRepositoryMock.Object,
                _fineRepositoryMock.Object);
        }

        [Fact]
        public async Task GetAllNotificationsAsync_ShouldReturn_ListOfNotifications()
        {
            // Arrange
            var notifications = new List<Notification>
            {
                new Notification { NotificationID = 1, MemberID = 1, Message = "Test Message 1", DateSent = DateTime.Now },
                new Notification { NotificationID = 2, MemberID = 2, Message = "Test Message 2", DateSent = DateTime.Now }
            };

            _notificationRepositoryMock.Setup(repo => repo.GetAllNotificationsAsync())
                .ReturnsAsync(notifications);

            // Act
            var result = await _service.GetAllNotificationsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.First().NotificationID.Should().Be(1);
            result.Last().NotificationID.Should().Be(2);
        }

        [Fact]
        public async Task GetNotificationByIdAsync_WithValidId_ShouldReturn_Notification()
        {
            // Arrange
            var notification = new Notification
            {
                NotificationID = 1,
                MemberID = 1,
                Message = "Test Message",
                DateSent = DateTime.Now
            };

            _notificationRepositoryMock.Setup(repo => repo.GetNotificationByIdAsync(1))
                .ReturnsAsync(notification);

            // Act
            var result = await _service.GetNotificationByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.NotificationID.Should().Be(1);
            result.Message.Should().Be("Test Message");
        }

        [Fact]
        public async Task GetNotificationByIdAsync_WithInvalidId_ShouldReturn_Null()
        {
            // Arrange
            _notificationRepositoryMock.Setup(repo => repo.GetNotificationByIdAsync(999))
                .ReturnsAsync((Notification)null);

            // Act
            var result = await _service.GetNotificationByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task AddNotificationAsync_ShouldCall_RepositoryMethod()
        {
            // Arrange
            var createDto = new CreateNotificationDto
            {
                MemberID = 1,
                Message = "Test Message",
                DateSent = DateTime.Now
            };

            // Act
            await _service.AddNotificationAsync(createDto);

            // Assert
            _notificationRepositoryMock.Verify(repo =>
                repo.AddNotificationAsync(It.Is<Notification>(n =>
                    n.MemberID == 1 && n.Message == "Test Message")), Times.Once);
        }

        [Fact]
        public async Task DeleteNotificationAsync_ShouldCall_RepositoryMethod()
        {
            // Act
            await _service.DeleteNotificationAsync(1);

            // Assert
            _notificationRepositoryMock.Verify(repo => repo.DeleteNotificationAsync(1), Times.Once);
        }

        [Fact]
        public async Task GetNotificationsForMemberAsync_ShouldReturn_MemberNotifications()
        {
            // Arrange
            var memberId = 1;
            var notifications = new List<Notification>
            {
                new Notification { NotificationID = 1, MemberID = memberId, Message = "Test Message 1", DateSent = DateTime.Now },
                new Notification { NotificationID = 2, MemberID = memberId, Message = "Test Message 2", DateSent = DateTime.Now }
            };

            _notificationRepositoryMock.Setup(repo => repo.GetNotificationsForMemberAsync(memberId))
                .ReturnsAsync(notifications);

            // Act
            var result = await _service.GetNotificationsForMemberAsync(memberId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(n => n.MemberID == memberId).Should().BeTrue();
        }

        [Fact]
        public async Task GetNotificationsByMemberNameAsync_ShouldReturn_FilteredNotifications()
        {
            // Arrange
            var memberName = "John Doe";
            var member = new Member { MemberID = 1, Name = memberName };
            var notifications = new List<Notification>
            {
                new Notification { NotificationID = 1, MemberID = 1, Message = "Test 1", DateSent = DateTime.Now, Member = member },
                new Notification { NotificationID = 2, MemberID = 2, Message = "Test 2", DateSent = DateTime.Now, Member = new Member { Name = "Jane Smith" } }
            };

            _notificationRepositoryMock.Setup(repo => repo.GetAllNotificationsAsync())
                .ReturnsAsync(notifications);

            // Act
            var result = await _service.GetNotificationsByMemberNameAsync(memberName);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().NotificationID.Should().Be(1);
        }

        [Fact]
        public async Task UpdateNotificationAsync_WithValidData_ShouldUpdateNotification()
        {
            // Arrange
            var notificationId = 1;
            var updateDto = new CreateNotificationDto
            {
                MemberID = 1,
                Message = "Updated Message",
                DateSent = DateTime.Now
            };

            var existingNotification = new Notification
            {
                NotificationID = notificationId,
                MemberID = 1,
                Message = "Original Message",
                DateSent = DateTime.Now.AddDays(-1)
            };

            _notificationRepositoryMock.Setup(repo => repo.GetNotificationByIdAsync(notificationId))
                .ReturnsAsync(existingNotification);

            // Act
            await _service.UpdateNotificationAsync(notificationId, updateDto);

            // Assert
            _notificationRepositoryMock.Verify(repo => repo.UpdateNotificationAsync(It.Is<Notification>(n =>
                n.NotificationID == notificationId && n.Message == "Updated Message")), Times.Once);
        }

        [Fact]
        public async Task UpdateNotificationAsync_WithInvalidId_ShouldThrowException()
        {
            // Arrange
            var notificationId = 999;
            var updateDto = new CreateNotificationDto
            {
                MemberID = 1,
                Message = "Updated Message",
                DateSent = DateTime.Now
            };

            _notificationRepositoryMock.Setup(repo => repo.GetNotificationByIdAsync(notificationId))
                .ReturnsAsync((Notification)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.UpdateNotificationAsync(notificationId, updateDto));
        }

        [Fact]
        public async Task NotifyForDueBooksAsync_ShouldAddNotifications_ForBooksDueSoon()
        {
            // Arrange
            var dueDate = DateTime.Now.AddDays(3);
            var transactions = new List<BorrowingTransaction>
            {
                new BorrowingTransaction
                {
                    TransactionID = 1,
                    MemberID = 1,
                    BookID = 1,
                    BorrowDate = DateTime.Now.AddDays(-10),
                    ReturnDate = dueDate, // due in 3 days
                    Status = "Borrowed"
                },
                new BorrowingTransaction
                {
                    TransactionID = 2,
                    MemberID = 2,
                    BookID = 2,
                    BorrowDate = DateTime.Now.AddDays(-15),
                    ReturnDate = DateTime.Now.AddDays(10), // due in 10 days (should not trigger notification)
                    Status = "Borrowed"
                }
            };

            _borrowingRepositoryMock.Setup(repo => repo.GetAllTransactionsAsync())
                .ReturnsAsync(transactions);

            // Act
            await _service.NotifyForDueBooksAsync();

            // Assert - Only one notification should be added for the book due in 3 days
            _notificationRepositoryMock.Verify(repo => repo.AddNotificationAsync(It.Is<Notification>(n =>
                n.MemberID == 1 && n.Message.Contains("3 days"))), Times.Once);

            // Ensure the notification for the book due in 10 days was not created
            _notificationRepositoryMock.Verify(repo => repo.AddNotificationAsync(It.Is<Notification>(n =>
                n.MemberID == 2)), Times.Never);
        }

        [Fact]
        public async Task NotifyForOverdueBooksAsync_ShouldAddNotifications_ForOverdueBooks()
        {
            // Arrange
            var overdueTransactions = new List<BorrowingTransaction>
            {
                new BorrowingTransaction
                {
                    TransactionID = 1,
                    MemberID = 1,
                    BookID = 1,
                    BorrowDate = DateTime.Now.AddDays(-15),
                    ReturnDate = DateTime.Now.AddDays(-2), // overdue by 2 days
                    Status = "Borrowed",
                    Book = new Book { Title = "Overdue Book" },
                    Member = new Member { MemberID = 1, Name = "John Doe" }
                }
            };

            _borrowingRepositoryMock.Setup(repo => repo.GetOverdueBooksAsync())
                .ReturnsAsync(overdueTransactions);

            // Act
            await _service.NotifyForOverdueBooksAsync();

            // Assert
            _notificationRepositoryMock.Verify(repo => repo.AddNotificationAsync(It.Is<Notification>(n =>
                n.MemberID == 1 && n.Message.Contains("overdue") && n.Message.Contains("Overdue Book"))), Times.Once);
        }

        [Fact]
        public async Task NotifyForFinePaymentAsync_WithPaidFine_ShouldAddNotification()
        {
            // Arrange
            var fineId = 1;
            var fine = new Fine
            {
                FineID = fineId,
                MemberID = 1,
                Amount = 10.50m,
                Status = "Paid",
                TransactionDate = DateTime.Now.AddDays(-1)
            };

            _fineRepositoryMock.Setup(repo => repo.GetFineByIdAsync(fineId))
                .ReturnsAsync(fine);

            // Act
            await _service.NotifyForFinePaymentAsync(fineId);

            // Assert
            _notificationRepositoryMock.Verify(repo => repo.AddNotificationAsync(It.Is<Notification>(n =>
                n.MemberID == 1 && n.Message.Contains("fine") && n.Message.Contains("settled"))), Times.Once);
        }

        [Fact]
        public async Task NotifyForFinePaymentAsync_WithUnpaidFine_ShouldNotAddNotification()
        {
            // Arrange
            var fineId = 1;
            var fine = new Fine
            {
                FineID = fineId,
                MemberID = 1,
                Amount = 10.50m,
                Status = "Pending", // Not paid
                TransactionDate = DateTime.Now.AddDays(-1)
            };

            _fineRepositoryMock.Setup(repo => repo.GetFineByIdAsync(fineId))
                .ReturnsAsync(fine);

            // Act
            await _service.NotifyForFinePaymentAsync(fineId);

            // Assert
            _notificationRepositoryMock.Verify(repo => repo.AddNotificationAsync(It.IsAny<Notification>()), Times.Never);
        }

        [Fact]
        public async Task NotifyForMembershipStatusChangeAsync_ShouldAddNotification()
        {
            // Arrange
            var memberId = 1;
            var newStatus = "Suspended";

            // Act
            await _service.NotifyForMembershipStatusChangeAsync(memberId, newStatus);

            // Assert
            _notificationRepositoryMock.Verify(repo => repo.AddNotificationAsync(It.Is<Notification>(n =>
                n.MemberID == memberId && n.Message.Contains(newStatus))), Times.Once);
        }

        [Fact]
        public async Task PerformPeriodicChecksAsync_ShouldCall_AllPeriodicMethods()
        {
            // Arrange
            _borrowingRepositoryMock.Setup(repo => repo.GetAllTransactionsAsync())
                .ReturnsAsync(new List<BorrowingTransaction>());

            _borrowingRepositoryMock.Setup(repo => repo.GetOverdueBooksAsync())
                .ReturnsAsync(new List<BorrowingTransaction>());

            _fineRepositoryMock.Setup(repo => repo.GetAllFinesAsync())
                .ReturnsAsync(new List<Fine>());

            // Act
            await _service.PerformPeriodicChecksAsync();

            // Assert - Verify that all periodic methods were called
            _borrowingRepositoryMock.Verify(repo => repo.GetAllTransactionsAsync(), Times.Once);
            _borrowingRepositoryMock.Verify(repo => repo.GetOverdueBooksAsync(), Times.Once);
            _fineRepositoryMock.Verify(repo => repo.GetAllFinesAsync(), Times.Once);
        }
    }
}
