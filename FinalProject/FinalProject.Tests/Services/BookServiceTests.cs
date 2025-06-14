using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinalProject.Application.Dto.Book;
using FinalProject.Application.Interfaces;
using FinalProject.Application.Services;
using FinalProject.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace FinalProject.Tests.Services
{
    public class BookServiceTests
    {
        private readonly Mock<IBookRepository> _mockBookRepository;
        private readonly BookService _bookService;

        public BookServiceTests()
        {
            _mockBookRepository = new Mock<IBookRepository>();
            _bookService = new BookService(_mockBookRepository.Object);
        }

        [Fact]
        public async Task GetAllBooksAsync_ShouldReturnAllBooks()
        {
            // Arrange
            var books = new List<Book>
            {
                new Book { BookID = 1, Title = "Book 1", Author = "Author 1", Genre = "Genre 1", ISBN = "1234567890123", YearPublished = 2000, AvailableCopies = 5 },
                new Book { BookID = 2, Title = "Book 2", Author = "Author 2", Genre = "Genre 2", ISBN = "3210987654321", YearPublished = 2010, AvailableCopies = 3 }
            };

            _mockBookRepository.Setup(repo => repo.GetAllBooksAsync()).ReturnsAsync(books);

            // Act
            var result = await _bookService.GetAllBooksAsync();

            // Assert
            result.Should().HaveCount(2);
            result.First().BookID.Should().Be(1);
            result.Last().BookID.Should().Be(2);
            _mockBookRepository.Verify(repo => repo.GetAllBooksAsync(), Times.Once);
        }

        [Fact]
        public async Task GetBookByIdAsync_ShouldReturnBook_WhenBookExists()
        {
            // Arrange
            var book = new Book { BookID = 1, Title = "Test Book", Author = "Test Author", Genre = "Test Genre", ISBN = "1234567890123", YearPublished = 2020, AvailableCopies = 10 };
            _mockBookRepository.Setup(repo => repo.GetBookByIdAsync(1)).ReturnsAsync(book);

            // Act
            var result = await _bookService.GetBookByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.BookID.Should().Be(1);
            result.Title.Should().Be("Test Book");
            _mockBookRepository.Verify(repo => repo.GetBookByIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task GetBookByIdAsync_ShouldReturnNull_WhenBookDoesNotExist()
        {
            // Arrange
            _mockBookRepository.Setup(repo => repo.GetBookByIdAsync(999)).ReturnsAsync((Book)null);

            // Act
            var result = await _bookService.GetBookByIdAsync(999);

            // Assert
            result.Should().BeNull();
            _mockBookRepository.Verify(repo => repo.GetBookByIdAsync(999), Times.Once);
        }

        [Fact]
        public async Task AddBookAsync_ShouldAddNewBook_WhenISBNDoesNotExist()
        {
            // Arrange
            var createDto = new CreateBookDto
            {
                Title = "New Book",
                Author = "New Author",
                Genre = "New Genre",
                ISBN = "1234567890123",
                YearPublished = 2022,
                AvailableCopies = 5
            };

            _mockBookRepository.Setup(repo => repo.GetBookByISBNAsync(createDto.ISBN)).ReturnsAsync((Book)null);

            // Act
            await _bookService.AddBookAsync(createDto);

            // Assert
            _mockBookRepository.Verify(repo => repo.AddBookAsync(It.Is<Book>(b =>
                b.Title == createDto.Title &&
                b.Author == createDto.Author &&
                b.ISBN == createDto.ISBN &&
                b.AvailableCopies == createDto.AvailableCopies)),
                Times.Once);
        }

        [Fact]
        public async Task AddBookAsync_ShouldUpdateExistingCopies_WhenISBNExistsWithMatchingDetails()
        {
            // Arrange
            var createDto = new CreateBookDto
            {
                Title = "Existing Book",
                Author = "Existing Author",
                Genre = "Existing Genre",
                ISBN = "1234567890123",
                YearPublished = 2015,
                AvailableCopies = 5
            };

            var existingBook = new Book
            {
                BookID = 1,
                Title = "Existing Book",
                Author = "Existing Author",
                Genre = "Existing Genre",
                ISBN = "1234567890123",
                YearPublished = 2015,
                AvailableCopies = 3
            };

            _mockBookRepository.Setup(repo => repo.GetBookByISBNAsync(createDto.ISBN)).ReturnsAsync(existingBook);

            // Act
            await _bookService.AddBookAsync(createDto);

            // Assert
            _mockBookRepository.Verify(repo => repo.UpdateBookAsync(It.Is<Book>(b =>
                b.BookID == existingBook.BookID &&
                b.AvailableCopies == existingBook.AvailableCopies + createDto.AvailableCopies)),
                Times.Once);
            _mockBookRepository.Verify(repo => repo.AddBookAsync(It.IsAny<Book>()), Times.Never);
        }

        [Fact]
        public async Task AddBookAsync_ShouldThrowException_WhenISBNExistsWithDifferentDetails()
        {
            // Arrange
            var createDto = new CreateBookDto
            {
                Title = "Different Title",
                Author = "Existing Author",
                Genre = "Existing Genre",
                ISBN = "1234567890123",
                YearPublished = 2015,
                AvailableCopies = 5
            };

            var existingBook = new Book
            {
                BookID = 1,
                Title = "Existing Book",  // Different title
                Author = "Existing Author",
                Genre = "Existing Genre",
                ISBN = "1234567890123",
                YearPublished = 2015,
                AvailableCopies = 3
            };

            _mockBookRepository.Setup(repo => repo.GetBookByISBNAsync(createDto.ISBN)).ReturnsAsync(existingBook);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _bookService.AddBookAsync(createDto));
            _mockBookRepository.Verify(repo => repo.UpdateBookAsync(It.IsAny<Book>()), Times.Never);
            _mockBookRepository.Verify(repo => repo.AddBookAsync(It.IsAny<Book>()), Times.Never);
        }

        [Fact]
        public async Task UpdateBookAsync_ShouldUpdateBook_WhenBookExists()
        {
            // Arrange
            var updateDto = new UpdateBookDto
            {
                BookID = 1,
                Title = "Updated Title",
                Author = "Updated Author",
                Genre = "Updated Genre",
                ISBN = "9876543210987",
                YearPublished = 2021,
                AvailableCopies = 10
            };

            var existingBook = new Book
            {
                BookID = 1,
                Title = "Original Title",
                Author = "Original Author",
                Genre = "Original Genre",
                ISBN = "1234567890123",
                YearPublished = 2010,
                AvailableCopies = 5
            };

            _mockBookRepository.Setup(repo => repo.GetBookByIdAsync(updateDto.BookID)).ReturnsAsync(existingBook);

            // Act
            await _bookService.UpdateBookAsync(updateDto);

            // Assert
            _mockBookRepository.Verify(repo => repo.UpdateBookAsync(It.Is<Book>(b =>
                b.BookID == updateDto.BookID &&
                b.Title == updateDto.Title &&
                b.Author == updateDto.Author &&
                b.ISBN == updateDto.ISBN &&
                b.YearPublished == updateDto.YearPublished &&
                b.AvailableCopies == updateDto.AvailableCopies)),
                Times.Once);
        }

        [Fact]
        public async Task UpdateBookAsync_ShouldDoNothing_WhenBookDoesNotExist()
        {
            // Arrange
            var updateDto = new UpdateBookDto
            {
                BookID = 999,
                Title = "Updated Title",
                Author = "Updated Author"
            };

            _mockBookRepository.Setup(repo => repo.GetBookByIdAsync(updateDto.BookID)).ReturnsAsync((Book)null);

            // Act
            await _bookService.UpdateBookAsync(updateDto);

            // Assert
            _mockBookRepository.Verify(repo => repo.UpdateBookAsync(It.IsAny<Book>()), Times.Never);
        }

        [Fact]
        public async Task DeleteBookAsync_ShouldDeleteBook_WhenBookExistsWithNoBorrowings()
        {
            // Arrange
            var book = new Book
            {
                BookID = 1,
                Title = "Test Book",
                Author = "Test Author",
                BorrowingTransactions = new List<BorrowingTransaction>()
            };

            _mockBookRepository.Setup(repo => repo.GetBookByIdAsync(1)).ReturnsAsync(book);

            // Act
            await _bookService.DeleteBookAsync(1);

            // Assert
            _mockBookRepository.Verify(repo => repo.DeleteBookAsync(1), Times.Once);
        }

        [Fact]
        public async Task DeleteBookAsync_ShouldThrowException_WhenBookHasPendingTransactions()
        {
            // Arrange
            var book = new Book
            {
                BookID = 1,
                Title = "Test Book",
                Author = "Test Author",
                BorrowingTransactions = new List<BorrowingTransaction>
                {
                    new BorrowingTransaction { Status = "Pending" }
                }
            };

            _mockBookRepository.Setup(repo => repo.GetBookByIdAsync(1)).ReturnsAsync(book);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _bookService.DeleteBookAsync(1));
            _mockBookRepository.Verify(repo => repo.DeleteBookAsync(1), Times.Never);
        }

        [Fact]
        public async Task DeleteBookAsync_ShouldThrowException_WhenBookDoesNotExist()
        {
            // Arrange
            _mockBookRepository.Setup(repo => repo.GetBookByIdAsync(999)).ReturnsAsync((Book)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _bookService.DeleteBookAsync(999));
            _mockBookRepository.Verify(repo => repo.DeleteBookAsync(999), Times.Never);
        }

        [Fact]
        public async Task SearchBooksAsync_ShouldReturnMatchingBooks_WhenSearchingByTitle()
        {
            // Arrange
            var searchDto = new SearchBooksDto { Title = "Book 1" };

            var allBooks = new List<Book>
            {
                new Book { BookID = 1, Title = "Book 1", Author = "Author 1" },
                new Book { BookID = 2, Title = "Book 2", Author = "Author 2" }
            };

            _mockBookRepository.Setup(repo => repo.GetAllBooksAsync()).ReturnsAsync(allBooks);

            // Act
            var result = await _bookService.SearchBooksAsync(searchDto);

            // Assert
            result.Should().HaveCount(1);
            result.First().Title.Should().Be("Book 1");
            _mockBookRepository.Verify(repo => repo.GetAllBooksAsync(), Times.Once);
        }

        [Fact]
        public async Task SearchBooksAsync_ShouldReturnMatchingBooks_WhenSearchingByAuthor()
        {
            // Arrange
            var searchDto = new SearchBooksDto { Author = "Author 2" };

            var allBooks = new List<Book>
            {
                new Book { BookID = 1, Title = "Book 1", Author = "Author 1" },
                new Book { BookID = 2, Title = "Book 2", Author = "Author 2" }
            };

            _mockBookRepository.Setup(repo => repo.GetAllBooksAsync()).ReturnsAsync(allBooks);

            // Act
            var result = await _bookService.SearchBooksAsync(searchDto);

            // Assert
            result.Should().HaveCount(1);
            result.First().Author.Should().Be("Author 2");
            _mockBookRepository.Verify(repo => repo.GetAllBooksAsync(), Times.Once);
        }

        [Fact]
        public async Task SearchBooksAsync_ShouldReturnMatchingBooks_WhenSearchingByGenre()
        {
            // Arrange
            var searchDto = new SearchBooksDto { Genre = "Fiction" };

            var allBooks = new List<Book>
            {
                new Book { BookID = 1, Title = "Book 1", Author = "Author 1", Genre = "Non-fiction" },
                new Book { BookID = 2, Title = "Book 2", Author = "Author 2", Genre = "Fiction" }
            };

            _mockBookRepository.Setup(repo => repo.GetAllBooksAsync()).ReturnsAsync(allBooks);

            // Act
            var result = await _bookService.SearchBooksAsync(searchDto);

            // Assert
            result.Should().HaveCount(1);
            result.First().Genre.Should().Be("Fiction");
            _mockBookRepository.Verify(repo => repo.GetAllBooksAsync(), Times.Once);
        }

        [Fact]
        public async Task SearchBooksAsync_ShouldReturnMatchingBooks_WhenFilteringByAvailability()
        {
            // Arrange
            var searchDto = new SearchBooksDto { AvailableCopiesGreaterThanZero = true };

            var allBooks = new List<Book>
            {
                new Book { BookID = 1, Title = "Book 1", AvailableCopies = 0 },
                new Book { BookID = 2, Title = "Book 2", AvailableCopies = 5 }
            };

            _mockBookRepository.Setup(repo => repo.GetAllBooksAsync()).ReturnsAsync(allBooks);

            // Act
            var result = await _bookService.SearchBooksAsync(searchDto);

            // Assert
            result.Should().HaveCount(1);
            result.First().AvailableCopies.Should().BeGreaterThan(0);
            _mockBookRepository.Verify(repo => repo.GetAllBooksAsync(), Times.Once);
        }
    }
}
