using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using FinalProject.Infrastructure.DbContexts;
using FinalProject.Infrastructure.Repository;
using FinalProject.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace FinalProject.Tests.Repository
{
    public class BorrowingRepositoryTests
    {
        private readonly ApplicationDbContext _context;
        private readonly BorrowingTransactionRepository _repository;

        public BorrowingRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("TestDb")
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new BorrowingTransactionRepository(_context);

            // Seed data
            _context.BorrowingTransactions.AddRange(new List<BorrowingTransaction>
            {
                new BorrowingTransaction { TransactionID = 1, BookID = 1, MemberID = 1, Status = "Borrowed", ReturnDate = DateTime.Today.AddDays(-5) },
                new BorrowingTransaction { TransactionID = 2, BookID = 2, MemberID = 2, Status = "Returned", ReturnDate = DateTime.Today }
            });
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetAllTransactionsAsync_ShouldReturnAllTransactions()
        {
            var result = await _repository.GetAllTransactionsAsync();
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetTransactionByIdAsync_ShouldReturnTransaction()
        {
            var result = await _repository.GetTransactionByIdAsync(1);
            result.Should().NotBeNull();
            result.TransactionID.Should().Be(1);
        }

        [Fact]
        public async Task AddTransactionAsync_ShouldAddTransaction()
        {
            var transaction = new BorrowingTransaction { TransactionID = 3, BookID = 3, MemberID = 3, Status = "Borrowed" };
            await _repository.AddTransactionAsync(transaction);

            var result = await _repository.GetTransactionByIdAsync(3);
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetOverdueBooksAsync_ShouldReturnOverdueBooks()
        {
            var result = await _repository.GetOverdueBooksAsync();
            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task UpdateTransactionAsync_ShouldUpdateTransaction()
        {
            var transaction = await _repository.GetTransactionByIdAsync(1);
            transaction.Status = "Returned";

            await _repository.UpdateTransactionAsync(transaction);

            var updatedTransaction = await _repository.GetTransactionByIdAsync(1);
            updatedTransaction.Status.Should().Be("Returned");
        }

        [Fact]
        public async Task DeleteTransactionAsync_ShouldDeleteTransaction()
        {
            await _repository.DeleteTransactionAsync(1);

            var result = await _repository.GetTransactionByIdAsync(1);
            result.Should().BeNull();
        }
    }
}
