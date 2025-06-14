using Xunit;
using Moq;
using FluentAssertions;
using FinalProject.Application.Interfaces;
using FinalProject.Application.Services;
using FinalProject.Application.Dto.Member;
using FinalProject.Domain;

namespace FinalProject.Tests.Services
{
    public class MemberServiceTests
    {
        private readonly Mock<IMemberRepository> _memberRepositoryMock;
        private readonly MemberService _memberService;

        public MemberServiceTests()
        {
            _memberRepositoryMock = new Mock<IMemberRepository>();
            _memberService = new MemberService(_memberRepositoryMock.Object, null);
        }

        [Fact]
        public async Task GetAllMembersAsync_ShouldReturnMembers()
        {
            // Arrange
            var members = new List<Member>
            {
                new Member { MemberID = 1, Name = "John Doe" }
            };
            _memberRepositoryMock.Setup(repo => repo.GetAllMembersAsync()).ReturnsAsync(members);

            // Act
            var result = await _memberService.GetAllMembersAsync();

            // Assert
            result.Should().HaveCount(1);
            result.First().Name.Should().Be("John Doe");
        }

        [Fact]
        public async Task GetMemberByIdAsync_ShouldReturnMember_WhenMemberExists()
        {
            // Arrange
            var member = new Member { MemberID = 1, Name = "John Doe" };
            _memberRepositoryMock.Setup(repo => repo.GetMemberByIdAsync(1)).ReturnsAsync(member);

            // Act
            var result = await _memberService.GetMemberByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("John Doe");
        }

        [Fact]
        public async Task RegisterMemberAsync_ShouldAddMember()
        {
            // Arrange
            var registerDto = new RegisterMemberDto
            {
                Name = "John Doe",
                Email = "johndoe@example.com",
                Password = "Password123!",
                Phone = "1234567890",
                Address = "123 Main St"
            };

            // Act
            await _memberService.RegisterMemberAsync(registerDto);

            // Assert
            _memberRepositoryMock.Verify(repo => repo.AddMemberAsync(It.IsAny<Member>()), Times.Once);
        }

        [Fact]
        public async Task UpdateMemberAsync_ShouldUpdateMember()
        {
            // Arrange
            var member = new Member { MemberID = 1, Name = "John Doe" };
            _memberRepositoryMock.Setup(repo => repo.GetMemberByIdAsync(1)).ReturnsAsync(member);

            var updateDto = new UpdateMemberDto
            {
                MemberID = 1,
                Name = "Updated Name"
            };

            // Act
            await _memberService.UpdateMemberAsync(1, updateDto);

            // Assert
            _memberRepositoryMock.Verify(repo => repo.UpdateMemberAsync(It.IsAny<Member>()), Times.Once);
        }

        [Fact]
        public async Task DeleteMemberAsync_ShouldDeleteMember()
        {
            // Arrange
            var member = new Member { MemberID = 1, Name = "John Doe" };
            _memberRepositoryMock.Setup(repo => repo.GetMemberByIdAsync(1)).ReturnsAsync(member);

            // Act
            await _memberService.DeleteMemberAsync(1);

            // Assert
            _memberRepositoryMock.Verify(repo => repo.DeleteMemberAsync(1), Times.Once);
        }
    }
}
