using FinalProject.Application.Dto.Fine;
using FinalProject.Domain;

namespace FinalProject.Application.Interfaces
{
    public interface IFineService
    {
        Task<IEnumerable<Fine>> GetAllFinesAsync();
        Task<Fine> GetFineByIdAsync(int fineId);
        Task AddFineAsync(int memberId, CreateFineDto createDto);
        Task UpdateFineByIdAsync(UpdateFineDto updateDto);
        Task UpdateFineForMemberAsync(int memberId, UpdateFineDto updateDto);
        Task<IEnumerable<Fine>> GetFinesForMemberAsync(int memberId);
        Task DeleteFineByIdAsync(int fineId);
        Task<IEnumerable<FineDetailsDto>> GetFinesByMemberNameAsync(string name);

        // New method to detect and apply overdue fines
        Task DetectAndApplyOverdueFinesAsync();

        // Add method for paying fines
        Task PayFineAsync(PayFineDto payFineDto);
    }
}
