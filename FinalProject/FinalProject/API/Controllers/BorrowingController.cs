using Microsoft.AspNetCore.Mvc;
using FinalProject.Application.Interfaces;
using FinalProject.Application.Dto.BorrowingTransaction;
using Microsoft.AspNetCore.Authorization;

namespace FinalProject.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BorrowingController : ControllerBase
    {
        private readonly IBorrowingTransactionService _borrowingService;

        public BorrowingController(IBorrowingTransactionService borrowingService)
        {
            _borrowingService = borrowingService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Librarian")] // Only staff can view all transactions

        public async Task<IActionResult> GetAllTransactions()
        {
            var transactions = await _borrowingService.GetAllTransactionsAsync();
            return Ok(transactions);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Librarian,User")] // Users can view their own transactions

        public async Task<IActionResult> GetTransactionById(int id)
        {
            var transaction = await _borrowingService.GetTransactionByIdAsync(id);
            if (transaction == null) return NotFound();
            return Ok(transaction);
        }

        [HttpPost("borrow")]
        [Authorize(Roles = "Admin,Librarian")] // Only staff can create borrowing transactions

        public async Task<IActionResult> BorrowBook([FromBody] BorrowBookDto borrowDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _borrowingService.AddTransactionAsync(borrowDto);
            return Ok(new { Message = "Book borrowed successfully." });
        }

        [HttpPost("return")]
        [Authorize(Roles = "Admin,Librarian")] // Only staff can process returns

        public async Task<IActionResult> ReturnBook([FromBody] ReturnBookDto returnDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _borrowingService.ReturnBookAsync(returnDto);
            return Ok(new { Message = "Book returned successfully." });
        }

        [HttpGet("overdue")]
        [Authorize(Roles = "Admin,Librarian")] // Only staff can view all overdue books

        public async Task<IActionResult> GetOverdueBooks()
        {
            var overdueBooks = await _borrowingService.GetOverdueBooksAsync();
            return Ok(overdueBooks);
        }


        [HttpGet("member/{memberId}")]
        [Authorize(Roles = "Admin,Librarian,User")] // Users can view their own history

        public async Task<IActionResult> GetMemberBorrowHistory(int memberId)
        {
            var borrowHistory = await _borrowingService.GetMemberBorrowHistoryAsync(memberId);
            return Ok(borrowHistory);
        }

        [HttpGet("member/name/{name}")]
        public async Task<IActionResult> GetTransactionsByMemberName(string name)
        {
            var transactions = await _borrowingService.GetTransactionsByMemberNameAsync(name);
            if (transactions == null || !transactions.Any()) return NotFound(new { Message = "No transactions found for the specified member name." });
            return Ok(transactions);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")] // Only staff can update transactions
        public async Task<IActionResult> UpdateTransaction(int id, [FromBody] UpdateBorrowingTransactionDto updateDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _borrowingService.UpdateTransactionAsync(id, updateDto);
            return Ok(new { Message = "Transaction updated successfully." });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // Only staff can delete transactions
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            await _borrowingService.DeleteTransactionAsync(id);
            return Ok(new { Message = "Transaction deleted successfully." });
        }


    }
}