using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using FinalProject.Infrastructure.DbContexts;
using FinalProject.Infrastructure.Repository;
using FinalProject.Domain;

namespace FinalProject.Tests.Repository
{
    public class MemberRepositoryTests
    {
        private readonly ApplicationDbContext _context;
        private readonly MemberRepository _repository;

        public MemberRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("InMemoryTestDb")
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new MemberRepository(_context);

            // Seed data
            _context.Members.Add(new Member { MemberID = 1, Name = "John Doe" });
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetAllMembersAsync_ShouldReturnMembers()
        {
            var result = await _repository.GetAllMembersAsync();
            result.Should().HaveCount(1);
            result.First().Name.Should().Be("John Doe");
        }

        [Fact]
        public async Task GetMemberByIdAsync_ShouldReturnMember_WhenMemberExists()
        {
            var result = await _repository.GetMemberByIdAsync(1);
            result.Should().NotBeNull();
            result.Name.Should().Be("John Doe");
        }

        [Fact]
        public async Task AddMemberAsync_ShouldAddMember()
        {
            var member = new Member { Name = "Jane Doe" };
            await _repository.AddMemberAsync(member);

            var result = await _repository.GetAllMembersAsync();
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task UpdateMemberAsync_ShouldUpdateMember()
        {
            var member = await _repository.GetMemberByIdAsync(1);
            member.Name = "Updated Name";

            await _repository.UpdateMemberAsync(member);

            var updatedMember = await _repository.GetMemberByIdAsync(1);
            updatedMember.Name.Should().Be("Updated Name");
        }

        [Fact]
        public async Task DeleteMemberAsync_ShouldDeleteMember()
        {
            await _repository.DeleteMemberAsync(1);

            var result = await _repository.GetAllMembersAsync();
            result.Should().BeEmpty();
        }
    }
}
