using FinalProject.Domain;

namespace FinalProject.Application.Interfaces
{
    public interface IBorrowingTransactionRepository
    {
        Task<IEnumerable<BorrowingTransaction>> GetAllTransactionsAsync();
        Task<BorrowingTransaction> GetTransactionByIdAsync(int transactionId);
        Task AddTransactionAsync(BorrowingTransaction transaction);
        Task<IEnumerable<BorrowingTransaction>> GetOverdueBooksAsync();
        Task<IEnumerable<BorrowingTransaction>> GetMemberBorrowHistoryAsync(int memberId);
        Task<int> GetMemberBorrowCountAsync(int memberId);
        Task UpdateTransactionAsync(BorrowingTransaction transaction);
        Task DeleteTransactionAsync(int transactionId); 

        Task<BorrowingTransaction> GetActiveBorrowingTransactionAsync(int memberID, int bookID);
    }
}
