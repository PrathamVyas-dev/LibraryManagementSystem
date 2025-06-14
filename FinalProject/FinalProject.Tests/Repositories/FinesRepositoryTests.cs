using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinalProject.Domain;
using FinalProject.Infrastructure.DbContexts;
using FinalProject.Infrastructure.Repository;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinalProject.Tests.Repositories
{
    public class FinesRepositoryTests
    {
        private readonly ApplicationDbContext _context;
        private readonly FinesRepository _repository;

        public FinesRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new FinesRepository(_context);

            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Add members
            _context.Members.AddRange(new List<Member>
            {
                new Member { MemberID = 1, Name = "John Doe" },
                new Member { MemberID = 2, Name = "Jane Smith" }
            });

            // Add fines
            _context.Fines.AddRange(new List<Fine>
            {
                new Fine { FineID = 1, MemberID = 1, Amount = 50.00M, Status = "Pending", TransactionDate = DateTime.Now.AddDays(-5) },
                new Fine { FineID = 2, MemberID = 2, Amount = 25.00M, Status = "Paid", TransactionDate = DateTime.Now.AddDays(-10) }
            });

            _context.SaveChanges();
        }

        [Fact]
        public async Task GetAllFinesAsync_ShouldReturnAllFines()
        {
            // Act
            var result = await _repository.GetAllFinesAsync();

            // Assert
            result.Should().HaveCount(2);
            result.Should().ContainSingle(f => f.FineID == 1);
            result.Should().ContainSingle(f => f.FineID == 2);
        }

        [Fact]
        public async Task GetFineByIdAsync_ShouldReturnFine_WhenFineExists()
        {
            // Act
            var result = await _repository.GetFineByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.FineID.Should().Be(1);
            result.MemberID.Should().Be(1);
            result.Amount.Should().Be(50.00M);
            result.Status.Should().Be("Pending");
        }

        [Fact]
        public async Task GetFineByIdAsync_ShouldReturnNull_WhenFineDoesNotExist()
        {
            // Act
            var result = await _repository.GetFineByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task AddFineAsync_ShouldAddFine()
        {
            // Arrange
            var fine = new Fine
            {
                MemberID = 1,
                Amount = 30.00M,
                Status = "Pending",
                TransactionDate = DateTime.Now
            };

            // Act
            await _repository.AddFineAsync(fine);

            // Assert
            var addedFine = await _context.Fines.FindAsync(fine.FineID);
            addedFine.Should().NotBeNull();
            addedFine.MemberID.Should().Be(1);
            addedFine.Amount.Should().Be(30.00M);
            addedFine.Status.Should().Be("Pending");
        }

        [Fact]
        public async Task UpdateFineAsync_ShouldUpdateFine()
        {
            // Arrange
            var fine = await _context.Fines.FindAsync(1);
            fine.Amount = 75.00M;
            fine.Status = "Updated";

            // Act
            await _repository.UpdateFineAsync(fine);

            // Assert
            var updatedFine = await _context.Fines.FindAsync(1);
            updatedFine.Amount.Should().Be(75.00M);
            updatedFine.Status.Should().Be("Updated");
        }

        [Fact]
        public async Task GetFinesForMemberAsync_ShouldReturnMemberFines()
        {
            // Act
            var result = await _repository.GetFinesForMemberAsync(1);

            // Assert
            result.Should().HaveCount(1);
            result.First().MemberID.Should().Be(1);
        }

        [Fact]
        public async Task GetFinesForMemberAsync_ShouldReturnEmpty_WhenMemberHasNoFines()
        {
            // Act
            var result = await _repository.GetFinesForMemberAsync(999);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task DeleteFineAsync_ShouldDeleteFine()
        {
            // Act
            await _repository.DeleteFineAsync(1);

            // Assert
            var deletedFine = await _context.Fines.FindAsync(1);
            deletedFine.Should().BeNull();
        }

        [Fact]
        public async Task GetOutstandingFinesForMemberAsync_ShouldReturnPendingFines()
        {
            // Act
            var result = await _repository.GetOutstandingFinesForMemberAsync(1);

            // Assert
            result.Should().HaveCount(1);
            result.First().Status.Should().Be("Pending");
        }

        [Fact]
        public async Task GetOutstandingFinesForMemberAsync_ShouldNotReturnPaidFines()
        {
            // Act
            var result = await _repository.GetOutstandingFinesForMemberAsync(2);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetFinesByMemberNameAsync_ShouldReturnFinesForMemberWithName()
        {
            // Act
            var result = await _repository.GetFinesByMemberNameAsync("John");

            // Assert
            result.Should().HaveCount(1);
            result.First().MemberID.Should().Be(1);
        }

        [Fact]
        public async Task GetFinesByMemberNameAsync_ShouldBeEmptyForNonExistingName()
        {
            // Act
            var result = await _repository.GetFinesByMemberNameAsync("NonExistentMember");

            // Assert
            result.Should().BeEmpty();
        }
    }
}
