using FinalProject.Application.Interfaces;
using FinalProject.Domain;
using FinalProject.Application.Dto.Fine;

namespace FinalProject.Application.Services
{
    public class FineService : IFineService
    {
        private readonly IFineRepository _fineRepository;
        private readonly IBorrowingTransactionRepository _borrowingRepository;
        private readonly INotificationService _notificationService;

        public FineService(
            IFineRepository fineRepository,
            IBorrowingTransactionRepository borrowingRepository,
            INotificationService notificationService) 
        {
            _fineRepository = fineRepository;
            _borrowingRepository = borrowingRepository;
            _notificationService = notificationService;
        }

        public async Task<IEnumerable<Fine>> GetAllFinesAsync()
        {
            return await _fineRepository.GetAllFinesAsync();
        }

        public async Task<Fine> GetFineByIdAsync(int fineId)
        {
            return await _fineRepository.GetFineByIdAsync(fineId);
        }

        public async Task<IEnumerable<Fine>> GetFinesForMemberAsync(int memberId)
        {
            return await _fineRepository.GetFinesForMemberAsync(memberId);
        }

        public async Task AddFineAsync(int memberId, CreateFineDto createDto)
        {
            var existingFines = await _fineRepository.GetFinesForMemberAsync(memberId);
            var existingFine = existingFines?.FirstOrDefault();

            if (existingFine != null)
            {
                // Update existing fine amount  
                existingFine.Amount = Math.Min(existingFine.Amount + createDto.Amount, 300);
                existingFine.Status = createDto.Status;
                existingFine.TransactionDate = createDto.TransactionDate;
                await _fineRepository.UpdateFineAsync(existingFine);
            }
            else
            {
                // Add new fine  
                var fine = new Fine
                {
                    MemberID = memberId,
                    Amount = Math.Min(createDto.Amount, 300), // Cap the fine at 300  
                    Status = createDto.Status,
                    TransactionDate = createDto.TransactionDate
                };
                await _fineRepository.AddFineAsync(fine);
            }
        }

        public async Task UpdateFineForMemberAsync(int memberId, UpdateFineDto updateDto)
        {
            var fine = new Fine
            {
                FineID = updateDto.FineID,
                MemberID = memberId,
                Amount = Math.Min(updateDto.Amount, 300), // Cap the fine at 300
                Status = updateDto.Status,
                TransactionDate = updateDto.TransactionDate
            };
            await _fineRepository.UpdateFineAsync(fine);
        }

        public async Task UpdateFineByIdAsync(UpdateFineDto updateDto)
        {
            var fine = new Fine
            {
                FineID = updateDto.FineID,
                MemberID = updateDto.MemberID,
                Amount = Math.Min(updateDto.Amount, 300), // Cap the fine at 300
                Status = updateDto.Status,
                TransactionDate = updateDto.TransactionDate
            };
            await _fineRepository.UpdateFineAsync(fine);
        }

        public async Task DeleteFineByIdAsync(int fineId)
        {
            var fine = await _fineRepository.GetFineByIdAsync(fineId);
            if (fine != null && fine.Status == "Paid")
            {
                await _fineRepository.DeleteFineAsync(fineId);
            }
            else
            {
                throw new InvalidOperationException("Fine can only be deleted if its status is 'Paid'.");
            }
        }

        public async Task<IEnumerable<FineDetailsDto>> GetFinesByMemberNameAsync(string name)
        {
            var fines = await _fineRepository.GetFinesByMemberNameAsync(name);
            return fines.Select(f => new FineDetailsDto
            {
                FineID = f.FineID,
                MemberID = f.MemberID,
                Amount = f.Amount,
                Status = f.Status,
                TransactionDate = f.TransactionDate
            });
        }

        public async Task DetectAndApplyOverdueFinesAsync()
        {
            var overdueTransactions = await _borrowingRepository.GetOverdueBooksAsync();

            foreach (var transaction in overdueTransactions)
            {
                var overdueDays = (DateTime.Now - transaction.ReturnDate).Days;
                if (overdueDays > 0)
                {
                    var fineAmount = Math.Min(overdueDays * 10, 300); // 10 rupees per day, capped at 300

                    if (overdueDays > 30)
                    {
                        // Suspend membership and add extra fine
                        var member = transaction.Member;
                        member.MembershipStatus = "Suspended";
                        fineAmount += 200; // Extra fine for suspended members
                    }

                    var fine = new Fine
                    {
                        MemberID = transaction.MemberID,
                        Amount = fineAmount,
                        Status = "Pending",
                        TransactionDate = DateTime.Now
                    };

                    await _fineRepository.AddFineAsync(fine);
                }
            }
        }

        public async Task PayFineAsync(PayFineDto payFineDto)
        {
            var fine = await _fineRepository.GetFineByIdAsync(payFineDto.FineID);
            if (fine == null || fine.Status == "Paid")
                throw new InvalidOperationException("Fine does not exist or is already paid.");

            if (payFineDto.Amount != fine.Amount)
                throw new InvalidOperationException("Payment amount must match the fine amount.");

            fine.Status = "Paid";
            await _fineRepository.UpdateFineAsync(fine);

            // Send notification for fine payment
            await _notificationService.NotifyForFinePaymentAsync(fine.FineID);
        }
    }
}
