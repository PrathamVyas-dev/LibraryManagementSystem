using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FinalProject.Application.Dto.BorrowingTransaction;
using FinalProject.API;
using FinalProject.Tests.Helpers;
using Xunit;
using FluentAssertions;

namespace FinalProject.Tests.Controllers
{
    public class BorrowingControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public BorrowingControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetAllTransactions_ShouldReturnOk()
        {
            var response = await _client.GetAsync("/api/borrowing");
            response.EnsureSuccessStatusCode();

            var transactions = await response.Content.ReadFromJsonAsync<IEnumerable<BorrowingTransactionDto>>();
            transactions.Should().NotBeNull();
        }

        [Fact]
        public async Task GetTransactionById_ShouldReturnTransaction()
        {
            var response = await _client.GetAsync("/api/borrowing/1");
            response.EnsureSuccessStatusCode();

            var transaction = await response.Content.ReadFromJsonAsync<BorrowingTransactionDto>();
            transaction.Should().NotBeNull();
            transaction.TransactionID.Should().Be(1);
        }

        [Fact]
        public async Task BorrowBook_ShouldReturnSuccess()
        {
            var borrowDto = new BorrowBookDto
            {
                BookID = 1,
                MemberID = 1,
                BorrowDate = DateTime.Today
            };

            var response = await _client.PostAsJsonAsync("/api/borrowing/borrow", borrowDto);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<dynamic>();
            result.Message.Should().Be("Book borrowed successfully.");
        }

        [Fact]
        public async Task ReturnBook_ShouldReturnSuccess()
        {
            var returnDto = new ReturnBookDto
            {
                TransactionID = 1,
                ReturnDate = DateTime.Today
            };

            var response = await _client.PostAsJsonAsync("/api/borrowing/return", returnDto);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<dynamic>();
            result.Message.Should().Be("Book returned successfully.");
        }

        [Fact]
        public async Task GetOverdueBooks_ShouldReturnOverdueBooks()
        {
            var response = await _client.GetAsync("/api/borrowing/overdue");
            response.EnsureSuccessStatusCode();

            var overdueBooks = await response.Content.ReadFromJsonAsync<IEnumerable<BorrowingTransactionDto>>();
            overdueBooks.Should().NotBeNull();
        }

        [Fact]
        public async Task GetMemberBorrowHistory_ShouldReturnHistory()
        {
            var response = await _client.GetAsync("/api/borrowing/member/1");
            response.EnsureSuccessStatusCode();

            var history = await response.Content.ReadFromJsonAsync<IEnumerable<BorrowingTransactionDto>>();
            history.Should().NotBeNull();
        }
    }
}
