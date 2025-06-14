using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FinalProject.API;
using FinalProject.Application.Dto.Book;
using FinalProject.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace FinalProject.Tests.Controllers
{
    public class BooksControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public BooksControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetAllBooks_ShouldReturnAllBooks()
        {
            // Act
            var response = await _client.GetAsync("/api/Books");

            // Assert
            response.EnsureSuccessStatusCode();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var books = await response.Content.ReadFromJsonAsync<IEnumerable<BookDetailsDto>>();
            books.Should().NotBeNull();
            books.Should().HaveCount(3); // Based on seeded data
        }

        [Fact]
        public async Task GetBookById_ShouldReturnBook_WhenBookExists()
        {
            // Act
            var response = await _client.GetAsync("/api/Books/1");

            // Assert
            response.EnsureSuccessStatusCode();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var book = await response.Content.ReadFromJsonAsync<BookDetailsDto>();
            book.Should().NotBeNull();
            book.BookID.Should().Be(1);
            book.Title.Should().Be("The Great Gatsby");
        }

        [Fact]
        public async Task GetBookById_ShouldReturnNotFound_WhenBookDoesNotExist()
        {
            // Act
            var response = await _client.GetAsync("/api/Books/999");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task AddBook_ShouldAddBookAndReturnCreated()
        {
            // Arrange
            var newBook = new CreateBookDto
            {
                Title = "New Test Book",
                Author = "Test Author",
                Genre = "Test Genre",
                ISBN = "9781234567890",
                YearPublished = 2023,
                AvailableCopies = 10
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Books", newBook);

            // Assert
            response.EnsureSuccessStatusCode();
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            // Verify the book was added by getting all books
            var getAllResponse = await _client.GetAsync("/api/Books");
            var books = await getAllResponse.Content.ReadFromJsonAsync<IEnumerable<BookDetailsDto>>();
            books.Should().Contain(b => b.Title == "New Test Book");
        }

        [Fact]
        public async Task UpdateBook_ShouldUpdateBookAndReturnNoContent()
        {
            // Arrange
            var updateBook = new UpdateBookDto
            {
                BookID = 2,
                Title = "Updated To Kill a Mockingbird",
                Author = "Harper Lee",
                Genre = "Classic Fiction",
                ISBN = "9780061120084",
                YearPublished = 1960,
                AvailableCopies = 7
            };

            // Act
            var response = await _client.PutAsJsonAsync("/api/Books", updateBook);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify the book was updated
            var getResponse = await _client.GetAsync("/api/Books/2");
            var updatedBook = await getResponse.Content.ReadFromJsonAsync<BookDetailsDto>();
            updatedBook.Should().NotBeNull();
            updatedBook.Title.Should().Be("Updated To Kill a Mockingbird");
            updatedBook.AvailableCopies.Should().Be(7);
        }

        [Fact]
        public async Task DeleteBook_ShouldDeleteBookAndReturnNoContent()
        {
            // Act
            var response = await _client.DeleteAsync("/api/Books/3");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify the book was deleted
            var getResponse = await _client.GetAsync("/api/Books/3");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task SearchBooks_ShouldReturnMatchingBooks_WhenSearchingByTitle()
        {
            // Act
            var response = await _client.GetAsync("/api/Books/search?Title=Great");

            // Assert
            response.EnsureSuccessStatusCode();

            var books = await response.Content.ReadFromJsonAsync<IEnumerable<BookDetailsDto>>();
            books.Should().NotBeNull();
            books.Should().HaveCount(1);
            books.First().Title.Should().Be("The Great Gatsby");
        }

        [Fact]
        public async Task SearchBooks_ShouldReturnMatchingBooks_WhenSearchingByAuthor()
        {
            // Act
            var response = await _client.GetAsync("/api/Books/search?Author=Orwell");

            // Assert
            response.EnsureSuccessStatusCode();

            var books = await response.Content.ReadFromJsonAsync<IEnumerable<BookDetailsDto>>();
            books.Should().NotBeNull();
            books.Should().Contain(b => b.Author == "George Orwell");
        }

        [Fact]
        public async Task SearchBooks_ShouldReturnMatchingBooks_WhenSearchingByGenre()
        {
            // Act
            var response = await _client.GetAsync("/api/Books/search?Genre=Fiction");

            // Assert
            response.EnsureSuccessStatusCode();

            var books = await response.Content.ReadFromJsonAsync<IEnumerable<BookDetailsDto>>();
            books.Should().NotBeNull();
            books.Should().Contain(b => b.Genre == "Fiction");
        }

        [Fact]
        public async Task SearchBooks_ShouldReturnMatchingBooks_WhenFilteringByAvailableCopies()
        {
            // Act
            var response = await _client.GetAsync("/api/Books/search?AvailableCopiesGreaterThanZero=true");

            // Assert
            response.EnsureSuccessStatusCode();

            var books = await response.Content.ReadFromJsonAsync<IEnumerable<BookDetailsDto>>();
            books.Should().NotBeNull();
            books.Should().NotContain(b => b.AvailableCopies == 0);
            books.Should().HaveCountGreaterThan(0);
        }
    }
}
