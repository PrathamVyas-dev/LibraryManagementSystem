using FinalProject.Application.Interfaces;
using FinalProject.Infrastructure.DbContexts;
using FinalProject.Domain;
using Microsoft.EntityFrameworkCore;
using FinalProject.Application.Dto.Book;

namespace FinalProject.Infrastructure.Repository
{
    public class BookRepository : IBookRepository
    {
        private readonly ApplicationDbContext _context;

        public BookRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Book>> GetAllBooksAsync()
        {
            return await _context.Books.ToListAsync();
        }

        public async Task<Book> GetBookByIdAsync(int bookId)
        {
            return await _context.Books.FindAsync(bookId);
        }

        public async Task AddBookAsync(Book book)
        {
            await _context.Books.AddAsync(book);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateBookAsync(Book book)
        {
            _context.Books.Update(book);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteBookAsync(int bookId)
        {
            var book = await _context.Books.FindAsync(bookId);
            if (book != null)
            {
                _context.Books.Remove(book);
                await _context.SaveChangesAsync();
            }
            if (book == null)
            {
                throw new Exception("Book not found");
            }
        }

        public async Task<Book> GetBookByISBNAsync(string isbn)
        {
            return await _context.Books.FirstOrDefaultAsync(b => b.ISBN == isbn);
        }

        public async Task<IEnumerable<Book>> SearchBooksAsync(SearchBooksDto searchBooksDto)
        {
            var query = _context.Books.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchBooksDto.Title))
            {
                query = query.Where(b => b.Title.Contains(searchBooksDto.Title, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(searchBooksDto.Author))
            {
                query = query.Where(b => b.Author.Contains(searchBooksDto.Author, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(searchBooksDto.Genre))
            {
                query = query.Where(b => b.Genre.Contains(searchBooksDto.Genre, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(searchBooksDto.ISBN))
            {
                query = query.Where(b => b.ISBN.Contains(searchBooksDto.ISBN, StringComparison.OrdinalIgnoreCase));
            }

            if (searchBooksDto.AvailableCopiesGreaterThanZero == true)
            {
                query = query.Where(b => b.AvailableCopies > 0);
            }

            return await query.ToListAsync();
        }
    }
}
