using System.ComponentModel.DataAnnotations;

namespace FinalProject.Application.Dto.Book
{
    public class SearchBooksDto
    {
        [MaxLength(50)]
        public string? Title { get; set; } // Optional, for searching by title.
        [MaxLength(50)]
        public string? Author { get; set; } // Optional, for searching by author.

        [MaxLength(50)]
        public string? Genre { get; set; } // Optional, for searching by genre 
        
        [MaxLength(15)]
        public string? ISBN { get; set; } // Optional, for searching by ISBN.

        public bool? AvailableCopiesGreaterThanZero { get; set; } // Optional, for filtering books with available copies > 0.
    }
}
