using FinalProject.Application.Interfaces;
using FinalProject.Application.Dto.Member;
using FinalProject.Domain;
using Microsoft.AspNetCore.Identity;

namespace FinalProject.Application.Services
{
    public class MemberService : IMemberService
    {
        private readonly IMemberRepository _memberRepository;
        private readonly UserManager<IdentityUser> _userManager;

        public MemberService(IMemberRepository memberRepository, UserManager<IdentityUser> userManager)
        {
            _memberRepository = memberRepository;
            _userManager = userManager;
        }

        public async Task<IEnumerable<MemberResponseDto>> GetAllMembersAsync()
        {
            var members = await _memberRepository.GetAllMembersAsync();
            return members.Select(m => new MemberResponseDto
            {
                MemberID = m.MemberID,
                Name = m.Name,
                Email = m.Email,
                Phone = m.Phone,
                Address = m.Address,
                MembershipStatus = m.MembershipStatus
            });
        }

        public async Task<MemberResponseDto> GetMemberByIdAsync(int memberId)
        {
            var member = await _memberRepository.GetMemberByIdAsync(memberId);
            if (member == null) return null;

            return new MemberResponseDto
            {
                MemberID = member.MemberID,
                Name = member.Name,
                Email = member.Email,
                Phone = member.Phone,
                Address = member.Address,
                MembershipStatus = member.MembershipStatus
            };
        }

        public async Task<MemberResponseDto> GetMemberByEmailAsync(string email)
        {
            var member = await _memberRepository.GetMemberByEmailAsync(email);
            if (member == null) return null;

            return new MemberResponseDto
            {
                MemberID = member.MemberID,
                Name = member.Name,
                Email = member.Email,
                Phone = member.Phone,
                Address = member.Address,
                MembershipStatus = member.MembershipStatus
            };
        }

        public async Task RegisterMemberAsync(RegisterMemberDto registerDto)
        {
            // Create the IdentityUser for authentication
            var user = new IdentityUser
            {
                UserName = registerDto.Email,
                Email = registerDto.Email
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

            // Add the user to the "User" role
            await _userManager.AddToRoleAsync(user, "User");

            // Create the Member instance for application-specific data
            var member = new Member
            {
                Name = registerDto.Name,
                Email = registerDto.Email,
                Phone = registerDto.Phone,
                Address = registerDto.Address,
                MembershipStatus = "Active"
            };

            // Save the Member instance to the database
            await _memberRepository.AddMemberAsync(member);
        }

        public async Task UpdateMemberAsync(int memberId, UpdateMemberDto updateDto)
        {
            var member = await _memberRepository.GetMemberByIdAsync(memberId);
            if (member == null) throw new InvalidOperationException("Member not found.");
           
            member.Name = updateDto.Name ?? member.Name;
            member.Phone = updateDto.Phone ?? member.Phone;
            member.Address = updateDto.Address ?? member.Address;

            await _memberRepository.UpdateMemberAsync(member);
        }


        public async Task DeleteMemberAsync(int memberId)
        {
            var member = await _memberRepository.GetMemberByIdAsync(memberId);
            if (member == null) throw new InvalidOperationException("Member not found.");

            // Check for overdue fines
            var overdueFines = await _memberRepository.GetUnpaidFinesOlderThan30DaysAsync(memberId);
            if (overdueFines.Any())
            {
                throw new InvalidOperationException("Cannot delete member with overdue fines.");
            }

            // Check for active borrowings
            var activeBorrowings = await _memberRepository.GetBorrowingsForMemberAsync(memberId);
            if (activeBorrowings.Any(b => b.Status == "Borrowed"))
            {
                throw new InvalidOperationException("Cannot delete member with active borrowings.");
            }

            // Delete the Member from the database
            await _memberRepository.DeleteMemberAsync(memberId);

            // Optionally, delete the associated IdentityUser
            var user = await _userManager.FindByEmailAsync(member.Email);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }
        }

        public async Task<IEnumerable<BorrowingTransaction>> GetBorrowingsForMemberAsync(int memberId)
        {
            return await _memberRepository.GetBorrowingsForMemberAsync(memberId);
        }

        public async Task<IEnumerable<Fine>> GetOutstandingFinesForMemberAsync(int memberId)
        {
            return await _memberRepository.GetOutstandingFinesForMemberAsync(memberId);
        }

        public async Task CheckAndUpdateMembershipStatusAsync()
        {
            var members = await _memberRepository.GetAllMembersAsync();

            foreach (var member in members)
            {
                var unpaidFines = await _memberRepository.GetUnpaidFinesOlderThan30DaysAsync(member.MemberID);
                if (unpaidFines.Any())
                {
                    member.MembershipStatus = "Suspended";
                }
                else
                {
                    member.MembershipStatus = "Active";
                }

                await _memberRepository.UpdateMemberAsync(member);
            }
        }
    }
}
