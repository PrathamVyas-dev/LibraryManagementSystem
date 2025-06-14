using System;
using System.Linq;
using System.Threading.Tasks;
using FinalProject.Application.Dto.Book;
using FinalProject.Domain;
using FinalProject.Infrastructure.DbContexts;
using FinalProject.Infrastructure.Repository;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinalProject.Tests.Repositories
{
    public class BookRepositoryTests : IDisposable
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;
        private readonly ApplicationDbContext _context;
        private readonly BookRepository _repository;

        public BookRepositoryTests()
        {
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(_options);
            _repository = new BookRepository(_context);

            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Clear any existing data
            _context.Books.RemoveRange(_context.Books);
            _context.SaveChanges();

            // Add test books
            _context.Books.AddRange(new[]
            {
                new Book
                {
                    BookID = 1,
                    Title = "The Great Gatsby",
                    Author = "F. Scott Fitzgerald",
                    Genre = "Classic",
                    ISBN = "9780743273565",
                    YearPublished = 1925,
                    AvailableCopies = 5
                },
                new Book
                {
                    BookID = 2,
                    Title = "To Kill a Mockingbird",
                    Author = "Harper Lee",
                    Genre = "Fiction",
                    ISBN = "9780061120084",
                    YearPublished = 1960,
                    AvailableCopies = 3
                },
                new Book
                {
                    BookID = 3,
                    Title = "1984",
                    Author = "George Orwell",
                    Genre = "Dystopian",
                    ISBN = "9780451524935",
                    YearPublished = 1949,
                    AvailableCopies = 0
                }
            });
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task GetAllBooksAsync_ShouldReturnAllBooks()
        {
            // Act
            var result = await _repository.GetAllBooksAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Select(b => b.Title).Should().Contain(new[] { "The Great Gatsby", "To Kill a Mockingbird", "1984" });
        }

        [Fact]
        public async Task GetBookByIdAsync_ShouldReturnBook_WhenBookExists()
        {
            // Act
            var result = await _repository.GetBookByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.BookID.Should().Be(1);
            result.Title.Should().Be("The Great Gatsby");
        }

        [Fact]
        public async Task GetBookByIdAsync_ShouldReturnNull_WhenBookDoesNotExist()
        {
            // Act
            var result = await _repository.GetBookByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetBookByISBNAsync_ShouldReturnBook_WhenBookWithISBNExists()
        {
            // Act
            var result = await _repository.GetBookByISBNAsync("9780743273565");

            // Assert
            result.Should().NotBeNull();
            result.ISBN.Should().Be("9780743273565");
            result.Title.Should().Be("The Great Gatsby");
        }

        [Fact]
        public async Task GetBookByISBNAsync_ShouldReturnNull_WhenBookWithISBNDoesNotExist()
        {
            // Act
            var result = await _repository.GetBookByISBNAsync("9999999999999");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task AddBookAsync_ShouldAddBook()
        {
            // Arrange
            var newBook = new Book
            {
                Title = "New Book",
                Author = "New Author",
                Genre = "New Genre",
                ISBN = "9781234567890",
                YearPublished = 2023,
                AvailableCopies = 10
            };

            // Act
            await _repository.AddBookAsync(newBook);

            // Assert
            var addedBook = await _context.Books.FirstOrDefaultAsync(b => b.ISBN == "9781234567890");
            addedBook.Should().NotBeNull();
            addedBook.Title.Should().Be("New Book");
            addedBook.Author.Should().Be("New Author");
        }

        [Fact]
        public async Task UpdateBookAsync_ShouldUpdateBook()
        {
            // Arrange
            var book = await _context.Books.FindAsync(2);
            book.Title = "Updated Title";
            book.AvailableCopies = 10;

            // Act
            await _repository.UpdateBookAsync(book);

            // Assert
            var updatedBook = await _context.Books.FindAsync(2);
            updatedBook.Title.Should().Be("Updated Title");
            updatedBook.AvailableCopies.Should().Be(10);
        }

        [Fact]
        public async Task DeleteBookAsync_ShouldDeleteBook()
        {
            // Act
            await _repository.DeleteBookAsync(3);

            // Assert
            var deletedBook = await _context.Books.FindAsync(3);
            deletedBook.Should().BeNull();

            var remainingBooks = await _context.Books.ToListAsync();
            remainingBooks.Should().HaveCount(2);
        }

        [Fact]
        public async Task DeleteBookAsync_ShouldThrowException_WhenBookDoesNotExist()
        {
            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _repository.DeleteBookAsync(999));
        }

        [Fact]
        public async Task SearchBooksAsync_ShouldReturnMatchingBooks_WhenSearchingByTitle()
        {
            // Arrange
            var searchDto = new SearchBooksDto { Title = "Great" };

            // Act
            var result = await _repository.SearchBooksAsync(searchDto);

            // Assert
            result.Should().HaveCount(1);
            result.First().Title.Should().Be("The Great Gatsby");
        }

        [Fact]
        public async Task SearchBooksAsync_ShouldReturnMatchingBooks_WhenSearchingByAuthor()
        {
            // Arrange
            var searchDto = new SearchBooksDto { Author = "Orwell" };

            // Act
            var result = await _repository.SearchBooksAsync(searchDto);

            // Assert
            result.Should().HaveCount(1);
            result.First().Author.Should().Be("George Orwell");
        }

        [Fact]
        public async Task SearchBooksAsync_ShouldReturnMatchingBooks_WhenSearchingByGenre()
        {
            // Arrange
            var searchDto = new SearchBooksDto { Genre = "Fiction" };

            // Act
            var result = await _repository.SearchBooksAsync(searchDto);

            // Assert
            result.Should().HaveCount(1);
            result.First().Genre.Should().Be("Fiction");
        }

        [Fact]
        public async Task SearchBooksAsync_ShouldReturnMatchingBooks_WhenSearchingByISBN()
        {
            // Arrange
            var searchDto = new SearchBooksDto { ISBN = "9780061" };

            // Act
            var result = await _repository.SearchBooksAsync(searchDto);

            // Assert
            result.Should().HaveCount(1);
            result.First().ISBN.Should().Be("9780061120084");
        }

        [Fact]
        public async Task SearchBooksAsync_ShouldReturnMatchingBooks_WhenFilteringByAvailability()
        {
            // Arrange
            var searchDto = new SearchBooksDto { AvailableCopiesGreaterThanZero = true };

            // Act
            var result = await _repository.SearchBooksAsync(searchDto);

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(b => b.AvailableCopies > 0);
            result.Should().NotContain(b => b.Title == "1984"); // This book has 0 available copies
        }

        [Fact]
        public async Task SearchBooksAsync_ShouldReturnMatchingBooks_WhenUsingMultipleFilters()
        {
            // Arrange
            var searchDto = new SearchBooksDto
            {
                Genre = "Classic",
                AvailableCopiesGreaterThanZero = true
            };

            // Act
            var result = await _repository.SearchBooksAsync(searchDto);

            // Assert
            result.Should().HaveCount(1);
            result.First().Title.Should().Be("The Great Gatsby");
        }
    }
}
