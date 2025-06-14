using Moq;
using Xunit;
using FluentAssertions;
using FinalProject.Application.Services;
using FinalProject.Application.Interfaces;
using FinalProject.Application.Dto.BorrowingTransaction;
using FinalProject.Domain;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace FinalProject.Tests.Services
{
    public class BorrowingServiceTests
    {
        private readonly Mock<IBorrowingTransactionRepository> _repositoryMock;
        private readonly Mock<IBookRepository> _bookRepositoryMock;
        private readonly Mock<IMemberRepository> _memberRepositoryMock;
        private readonly Mock<IFineRepository> _fineRepositoryMock;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly BorrowingTransactionService _service;

        public BorrowingServiceTests()
        {
            _repositoryMock = new Mock<IBorrowingTransactionRepository>();
            _bookRepositoryMock = new Mock<IBookRepository>();
            _memberRepositoryMock = new Mock<IMemberRepository>();
            _fineRepositoryMock = new Mock<IFineRepository>();
            _notificationServiceMock = new Mock<INotificationService>();

            _service = new BorrowingTransactionService(
                _repositoryMock.Object,
                _bookRepositoryMock.Object,
                _memberRepositoryMock.Object,
                _fineRepositoryMock.Object,
                _notificationServiceMock.Object
            );
        }

        [Fact]
        public async Task GetAllTransactionsAsync_ShouldReturnAllTransactions()
        {
            // Arrange
            var transactions = new List<BorrowingTransaction>
            {
                new BorrowingTransaction { TransactionID = 1, BookID = 1, MemberID = 1, Status = "Borrowed" },
                new BorrowingTransaction { TransactionID = 2, BookID = 2, MemberID = 2, Status = "Returned" }
            };
            var books = new List<Book>
            {
                new Book { BookID = 1, Title = "Book 1" },
                new Book { BookID = 2, Title = "Book 2" }
            };

            _repositoryMock.Setup(r => r.GetAllTransactionsAsync()).ReturnsAsync(transactions);
            _bookRepositoryMock.Setup(b => b.GetAllBooksAsync()).ReturnsAsync(books);

            // Act
            var result = await _service.GetAllTransactionsAsync();

            // Assert
            result.Should().HaveCount(2);
            result.First().BookName.Should().Be("Book 1");
        }

        [Fact]
        public async Task GetTransactionByIdAsync_ShouldReturnTransaction()
        {
            // Arrange
            var transaction = new BorrowingTransaction { TransactionID = 1, BookID = 1, MemberID = 1 };
            var book = new Book { BookID = 1, Title = "Book 1" };

            _repositoryMock.Setup(r => r.GetTransactionByIdAsync(1)).ReturnsAsync(transaction);
            _bookRepositoryMock.Setup(b => b.GetBookByIdAsync(1)).ReturnsAsync(book);

            // Act
            var result = await _service.GetTransactionByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.BookName.Should().Be("Book 1");
        }

        [Fact]
        public async Task AddTransactionAsync_ShouldThrowException_WhenMemberIsInactive()
        {
            // Arrange
            var borrowDto = new BorrowBookDto { BookID = 1, MemberID = 1, BorrowDate = DateTime.Today };
            _memberRepositoryMock.Setup(m => m.GetMemberByIdAsync(1)).ReturnsAsync(new Member { MembershipStatus = "Inactive" });

            // Act
            var act = async () => await _service.AddTransactionAsync(borrowDto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Member is not active or does not exist.");
        }

        [Fact]
        public async Task ReturnBookAsync_ShouldUpdateTransactionStatus()
        {
            // Arrange
            var returnDto = new ReturnBookDto { TransactionID = 1, ReturnDate = DateTime.Today };
            var transaction = new BorrowingTransaction { TransactionID = 1, Status = "Borrowed", ReturnDate = DateTime.Today.AddDays(-1) };

            _repositoryMock.Setup(r => r.GetTransactionByIdAsync(1)).ReturnsAsync(transaction);

            // Act
            await _service.ReturnBookAsync(returnDto);

            // Assert
            transaction.Status.Should().Be("Returned");
        }

        [Fact]
        public async Task GetOverdueBooksAsync_ShouldReturnOverdueBooks()
        {
            // Arrange
            var overdueTransactions = new List<BorrowingTransaction>
            {
                new BorrowingTransaction { TransactionID = 1, BookID = 1, MemberID = 1, ReturnDate = DateTime.Today.AddDays(-5), Status = "Borrowed" }
            };
            var books = new List<Book>
            {
                new Book { BookID = 1, Title = "Book 1" }
            };

            _repositoryMock.Setup(r => r.GetOverdueBooksAsync()).ReturnsAsync(overdueTransactions);
            _bookRepositoryMock.Setup(b => b.GetAllBooksAsync()).ReturnsAsync(books);

            // Act
            var result = await _service.GetOverdueBooksAsync();

            // Assert
            result.Should().HaveCount(1);
            result.First().BookName.Should().Be("Book 1");
        }

        [Fact]
        public async Task UpdateTransactionAsync_ShouldUpdateTransaction()
        {
            // Arrange
            var updateDto = new UpdateBorrowingTransactionDto
            {
                TransactionID = 1,
                BookID = 2,
                MemberID = 2,
                BorrowDate = DateTime.Today,
                ReturnDate = DateTime.Today.AddDays(14),
                Status = "Returned"
            };
            var transaction = new BorrowingTransaction { TransactionID = 1, BookID = 1, MemberID = 1 };

            _repositoryMock.Setup(r => r.GetTransactionByIdAsync(1)).ReturnsAsync(transaction);

            // Act
            await _service.UpdateTransactionAsync(1, updateDto);

            // Assert
            transaction.BookID.Should().Be(2);
            transaction.Status.Should().Be("Returned");
        }

        [Fact]
        public async Task DeleteTransactionAsync_ShouldDeleteTransaction()
        {
            // Arrange
            var transaction = new BorrowingTransaction { TransactionID = 1 };
            _repositoryMock.Setup(r => r.GetTransactionByIdAsync(1)).ReturnsAsync(transaction);

            // Act
            await _service.DeleteTransactionAsync(1);

            // Assert
            _repositoryMock.Verify(r => r.DeleteTransactionAsync(1), Times.Once);
        }
    }
}
