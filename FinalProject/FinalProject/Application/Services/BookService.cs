using FinalProject.Application.Interfaces;
using FinalProject.Application.Dto.Book;
using FinalProject.Domain;

namespace FinalProject.Application.Services
{
    public class BookService : IBookService
    {
        private readonly IBookRepository _bookRepository;

        public BookService(IBookRepository bookRepository)
        {
            _bookRepository = bookRepository;
        }

        public async Task<IEnumerable<BookDetailsDto>> GetAllBooksAsync()
        {
            var books = await _bookRepository.GetAllBooksAsync();
            return books.Select(b => new BookDetailsDto
            {
                BookID = b.BookID,
                Title = b.Title,
                Author = b.Author,
                Genre = b.Genre,
                ISBN = b.ISBN,
                YearPublished = b.YearPublished,
                AvailableCopies = b.AvailableCopies
            });
        }

        public async Task<BookDetailsDto> GetBookByIdAsync(int bookId)
        {
            var book = await _bookRepository.GetBookByIdAsync(bookId);
            if (book == null) return null;

            return new BookDetailsDto
            {
                BookID = book.BookID,
                Title = book.Title,
                Author = book.Author,
                Genre = book.Genre,
                ISBN = book.ISBN,
                YearPublished = book.YearPublished,
                AvailableCopies = book.AvailableCopies
            };
        }


        //Updated Add Book Method To Update Existing Copies 
        public async Task AddBookAsync(CreateBookDto createBookDto)
        {
            var existingBook = await _bookRepository.GetBookByISBNAsync(createBookDto.ISBN);

            if (existingBook != null)
            {
                // Check if the existing book details match the new book details
                if (existingBook.Title == createBookDto.Title && existingBook.Author == createBookDto.Author &&
                    existingBook.Genre == createBookDto.Genre && existingBook.YearPublished == createBookDto.YearPublished)
                {
                    // If details match, increase the available copies
                    existingBook.AvailableCopies += createBookDto.AvailableCopies;
                    await _bookRepository.UpdateBookAsync(existingBook);
                }
                else
                {
                    // If details do not match, throw an exception
                    throw new InvalidOperationException("A book with the same ISBN but different details already exists.");
                }
            }
            else
            {
                // If no existing book, add the new book
                var newBook = new Book
                {
                    Title = createBookDto.Title,
                    Author = createBookDto.Author,
                    Genre = createBookDto.Genre,
                    ISBN = createBookDto.ISBN,
                    YearPublished = createBookDto.YearPublished,
                    AvailableCopies = createBookDto.AvailableCopies
                };

                await _bookRepository.AddBookAsync(newBook);
            }
        }

        public async Task UpdateBookAsync(UpdateBookDto updateBookDto)
        {
            var book = await _bookRepository.GetBookByIdAsync(updateBookDto.BookID);
            if (book == null) return;

            book.Title = updateBookDto.Title ?? book.Title;
            book.Author = updateBookDto.Author ?? book.Author;
            book.Genre = updateBookDto.Genre ?? book.Genre;
            book.ISBN = updateBookDto.ISBN ?? book.ISBN;
            book.YearPublished = updateBookDto.YearPublished;
            book.AvailableCopies = updateBookDto.AvailableCopies;

            await _bookRepository.UpdateBookAsync(book);
        }

        // Updated DeleteBookAsync method to handle null BorrowingTransactions
        public async Task DeleteBookAsync(int bookId)
        {
            var book = await _bookRepository.GetBookByIdAsync(bookId);

            if (book == null)
            {
                throw new InvalidOperationException("Book not found.");
            }

            // Check for pending borrowing transactions, handle null BorrowingTransactions
            var hasPendingBorrowingTransactions = book.BorrowingTransactions?.Any(bt => bt.Status == "Pending") ?? false;

            if (hasPendingBorrowingTransactions)
            {
                throw new InvalidOperationException("Cannot delete the book as it has pending borrowing transactions.");
            }

            await _bookRepository.DeleteBookAsync(bookId);
        }


        // Updated SearchBooksAsync method to search based on a single field
        public async Task<IEnumerable<BookDetailsDto>> SearchBooksAsync(SearchBooksDto searchBooksDto)
        {
            var allBooks = await _bookRepository.GetAllBooksAsync();

            // Filter based on a single field provided by the user
            var filteredBooks = allBooks.Where(b =>
                (!string.IsNullOrWhiteSpace(searchBooksDto.Title) && b.Title.Contains(searchBooksDto.Title, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(searchBooksDto.Author) && b.Author.Contains(searchBooksDto.Author, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(searchBooksDto.Genre) && b.Genre.Contains(searchBooksDto.Genre, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(searchBooksDto.ISBN) && b.ISBN.Contains(searchBooksDto.ISBN, StringComparison.OrdinalIgnoreCase)));

            return filteredBooks.Select(b => new BookDetailsDto
            {
                BookID = b.BookID,
                Title = b.Title,
                Author = b.Author,
                Genre = b.Genre,
                ISBN = b.ISBN,
                YearPublished = b.YearPublished,
                AvailableCopies = b.AvailableCopies
            });
        }
    }
}
