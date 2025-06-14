using Microsoft.AspNetCore.Mvc;
using FinalProject.Application.Interfaces;
using FinalProject.Application.Dto.Book;
using Microsoft.AspNetCore.Authorization;

namespace FinalProject.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly IBookService _bookService;

        public BooksController(IBookService bookService)
        {
            _bookService = bookService;
        }

        [HttpGet]
        [AllowAnonymous] // Everyone can browse books
        public async Task<IActionResult> GetAllBooks()
        {
            var books = await _bookService.GetAllBooksAsync();
            return Ok(books);
        }

        [HttpGet("{id}")]
        [AllowAnonymous] // Everyone can view book details
        public async Task<IActionResult> GetBookById(int id)
        {
            var book = await _bookService.GetBookByIdAsync(id);
            if (book == null) return NotFound();
            return Ok(book);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Librarian")] // Only staff can add books

        public async Task<IActionResult> AddBook([FromBody] CreateBookDto createBookDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _bookService.AddBookAsync(createBookDto);
            return Ok(createBookDto); 
        }

        [HttpPut]
        [Authorize(Roles = "Admin,Librarian")] // Only staff can update books

        public async Task<IActionResult> UpdateBook([FromBody] UpdateBookDto updateBookDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _bookService.UpdateBookAsync(updateBookDto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Librarian")] // Only staff can delete books

        public async Task<IActionResult> DeleteBook(int id)
        {
            await _bookService.DeleteBookAsync(id);
            return NoContent();
        }

        [HttpGet("search")]
        [AllowAnonymous] // Everyone can search books

        public async Task<IActionResult> SearchBooks([FromQuery] SearchBooksDto searchBooksDto)
        {
            var books = await _bookService.SearchBooksAsync(searchBooksDto);
            return Ok(books);
        }
    }
}
