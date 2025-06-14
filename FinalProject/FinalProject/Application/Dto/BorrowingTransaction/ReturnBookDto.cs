using System.ComponentModel.DataAnnotations;

namespace FinalProject.Application.Dto.BorrowingTransaction
{
    public class ReturnBookDto
    {
        [Required]
        public int TransactionID { get; set; } // Required to identify the borrowing transaction.

        [Required]
        [DataType(DataType.Date)]
        public DateTime ReturnDate { get; set; } = DateTime.Today; // Default to today if not provided.
    }
}
