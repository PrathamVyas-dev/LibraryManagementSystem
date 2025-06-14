using Microsoft.AspNetCore.Mvc;
using FinalProject.Application.Interfaces;
using FinalProject.Application.Dto.Fine;
using Microsoft.AspNetCore.Authorization;

namespace FinalProject.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FinesController : ControllerBase
    {
        private readonly IFineService _fineService;

        public FinesController(IFineService fineService)
        {
            _fineService = fineService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Librarian")] // Only staff can view all fines

        public async Task<IActionResult> GetAllFines()
        {
            var fines = await _fineService.GetAllFinesAsync();
            return Ok(fines);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Librarian,User")] // Users can view their own fines

        public async Task<IActionResult> GetFineById(int id)
        {
            var fine = await _fineService.GetFineByIdAsync(id);
            if (fine == null) return NotFound();
            return Ok(fine);
        }

        [HttpGet("member/{memberId}")]
        [Authorize(Roles = "Admin,Librarian,User")] // Users can view their own fines
        public async Task<IActionResult> GetFinesForMember(int memberId)
        {
            var fines = await _fineService.GetFinesForMemberAsync(memberId);
            return Ok(fines);
        }

        [HttpGet("member/name/{name}")]
        public async Task<IActionResult> GetFineByMemberName(string name)
        {
            // Assuming the service layer has a method to fetch fines by member name
            var fines = await _fineService.GetFinesByMemberNameAsync(name);
            if (fines == null || !fines.Any()) return NotFound(new { Message = "No fines found for the specified member name." });
            return Ok(fines);
        }

        [HttpPost("apply-overdue-fines")]
        [Authorize(Roles = "Admin,Librarian")] // Only staff can apply overdue fines
        public async Task<IActionResult> ApplyOverdueFines()
        {
            await _fineService.DetectAndApplyOverdueFinesAsync();
            return Ok(new { Message = "Overdue fines applied successfully." });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Librarian")] // Only staff can add fines
        public async Task<IActionResult> AddFine([FromBody] CreateFineDto createDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _fineService.AddFineAsync(createDto.MemberID, createDto);
            return Ok(new { Message = "Fine added successfully." });
        }

        [HttpPut]
        [Authorize(Roles = "Admin,Librarian")] // Only staff can update fines
        public async Task<IActionResult> UpdateFine([FromBody] UpdateFineDto updateDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _fineService.UpdateFineByIdAsync(updateDto);
            return Ok(new { Message = "Fine updated successfully." });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Librarian")] // Only staff can delete fines
        public async Task<IActionResult> DeleteFine(int id)
        {
            await _fineService.DeleteFineByIdAsync(id);
            return Ok(new { Message = "Fine deleted successfully." });
        }

        [HttpPost("pay")]
        [Authorize(Roles = "Admin,Librarian,User")] // Users can pay their own fines
        public async Task<IActionResult> PayFine([FromBody] PayFineDto payFineDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _fineService.PayFineAsync(payFineDto);
            return Ok(new { Message = "Fine payment successful. Notification sent to the member." });
        }
    }
}
