using Microsoft.AspNetCore.Mvc;
using FinalProject.Application.Interfaces;
using FinalProject.Application.Dto.Member;
using Microsoft.AspNetCore.Authorization;

namespace FinalProject.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MemberController : ControllerBase
    {
        private readonly IMemberService _memberService;

        public MemberController(IMemberService memberService)
        {
            _memberService = memberService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Librarian")] // Only staff can view all members

        public async Task<IActionResult> GetAllMembers()
        {
            var members = await _memberService.GetAllMembersAsync();
            return Ok(members);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Librarian,User")] // Users can view their own profile

        public async Task<IActionResult> GetMemberById(int id)
        {
            var member = await _memberService.GetMemberByIdAsync(id);
            if (member == null) return NotFound();
            return Ok(member);
        }

        [HttpGet("by-email")]
        [Authorize(Roles = "Admin,Librarian,User")] // Users can view their own profile
        public async Task<IActionResult> GetMemberByEmail([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { Message = "Email is required." });

            var member = await _memberService.GetMemberByEmailAsync(email);
            if (member == null)
                return NotFound(new { Message = "Member not found." });

            return Ok(member);
        }

        [HttpPost("register")]
        [AllowAnonymous] // Registration is open to everyone
        public async Task<IActionResult> RegisterMember([FromBody] RegisterMemberDto registerDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _memberService.RegisterMemberAsync(registerDto);
            return Ok(new { Message = "Member registered successfully." });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Librarian,User")] // Users can update their own profile

        public async Task<IActionResult> UpdateMember(int id, [FromBody] UpdateMemberDto updateDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _memberService.UpdateMemberAsync(id, updateDto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // Only admin can delete members
        public async Task<IActionResult> DeleteMember(int id)
        {
            try
            {
                await _memberService.DeleteMemberAsync(id);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("{id}/borrowings")]
        [Authorize(Roles = "Admin,Librarian,User")] // Users can view their own borrowings
        public async Task<IActionResult> GetBorrowingsForMember(int id)
        {
            var borrowings = await _memberService.GetBorrowingsForMemberAsync(id);
            return Ok(borrowings);
        }

        [HttpGet("{id}/fines")]
        [Authorize(Roles = "Admin,Librarian,User")] // Users can view their own fines
        public async Task<IActionResult> GetOutstandingFinesForMember(int id)
        {
            var fines = await _memberService.GetOutstandingFinesForMemberAsync(id);
            return Ok(fines);
        }

        [HttpPost("check-membership-status")]
        [Authorize(Roles = "Admin,Librarian")] // Only staff can check and update membership statuses
        public async Task<IActionResult> CheckAndUpdateMembershipStatus()
        {
            await _memberService.CheckAndUpdateMembershipStatusAsync();
            return Ok(new { Message = "Membership statuses updated successfully." });
        }
    }
}

