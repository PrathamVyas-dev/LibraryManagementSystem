using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FinalProject.API;
using FinalProject.Application.Dto.Fine;
using FluentAssertions;
using Xunit;

namespace FinalProject.Tests.Controllers
{
    public class FinesControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public FinesControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetAllFines_ShouldReturnOk()
        {
            // Act
            var response = await _client.GetAsync("/api/fines");

            // Assert
            response.EnsureSuccessStatusCode();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var fines = await response.Content.ReadFromJsonAsync<IEnumerable<FineDetailsDto>>();
            fines.Should().NotBeNull();
        }

        [Fact]
        public async Task GetFineById_ShouldReturnOk_WhenFineExists()
        {
            // Act
            var response = await _client.GetAsync("/api/fines/1");

            // Assert
            response.EnsureSuccessStatusCode();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var fine = await response.Content.ReadFromJsonAsync<FineDetailsDto>();
            fine.Should().NotBeNull();
            fine.FineID.Should().Be(1);
        }

        [Fact]
        public async Task GetFineById_ShouldReturnNotFound_WhenFineDoesNotExist()
        {
            // Act
            var response = await _client.GetAsync("/api/fines/999");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetFinesForMember_ShouldReturnOk()
        {
            // Act
            var response = await _client.GetAsync("/api/fines/member/1");

            // Assert
            response.EnsureSuccessStatusCode();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var fines = await response.Content.ReadFromJsonAsync<IEnumerable<FineDetailsDto>>();
            fines.Should().NotBeNull();
        }

        [Fact]
        public async Task GetFineByMemberName_ShouldReturnOk_WhenFinesExist()
        {
            // Act
            var response = await _client.GetAsync("/api/fines/member/name/John");

            // Assert
            response.EnsureSuccessStatusCode();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var fines = await response.Content.ReadFromJsonAsync<IEnumerable<FineDetailsDto>>();
            fines.Should().NotBeNull();
        }

        [Fact]
        public async Task GetFineByMemberName_ShouldReturnNotFound_WhenNoFinesExist()
        {
            // Act
            var response = await _client.GetAsync("/api/fines/member/name/NonExistentMember");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ApplyOverdueFines_ShouldReturnOk()
        {
            // Act
            var response = await _client.PostAsync("/api/fines/apply-overdue-fines", null);

            // Assert
            response.EnsureSuccessStatusCode();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<dynamic>();
            ((string)result.Message).Should().Be("Overdue fines applied successfully.");
        }

        [Fact]
        public async Task AddFine_ShouldReturnOk_WhenModelStateIsValid()
        {
            // Arrange
            var createFineDto = new CreateFineDto
            {
                MemberID = 1,
                Amount = 100.00M,
                Status = "Pending",
                TransactionDate = DateTime.Now
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/fines", createFineDto);

            // Assert
            response.EnsureSuccessStatusCode();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<dynamic>();
            ((string)result.Message).Should().Be("Fine added successfully.");
        }

        [Fact]
        public async Task AddFine_ShouldReturnBadRequest_WhenModelStateIsInvalid()
        {
            // Arrange
            var createFineDto = new CreateFineDto
            {
                // Missing required fields
                MemberID = 1,
                // Amount is missing
                Status = "Pending"
                // TransactionDate is missing
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/fines", createFineDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateFine_ShouldReturnOk_WhenModelStateIsValid()
        {
            // Arrange
            var updateFineDto = new UpdateFineDto
            {
                FineID = 1,
                MemberID = 1,
                Amount = 75.00M,
                Status = "Pending",
                TransactionDate = DateTime.Now
            };

            // Act
            var response = await _client.PutAsJsonAsync("/api/fines", updateFineDto);

            // Assert
            response.EnsureSuccessStatusCode();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<dynamic>();
            ((string)result.Message).Should().Be("Fine updated successfully.");
        }

        [Fact]
        public async Task UpdateFine_ShouldReturnBadRequest_WhenModelStateIsInvalid()
        {
            // Arrange
            var updateFineDto = new UpdateFineDto
            {
                // Missing required fields
                FineID = 1
                // Other properties are missing
            };

            // Act
            var response = await _client.PutAsJsonAsync("/api/fines", updateFineDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task DeleteFine_ShouldReturnOk()
        {
            // Act
            var response = await _client.DeleteAsync("/api/fines/2"); // Deleting a paid fine

            // Assert
            response.EnsureSuccessStatusCode();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<dynamic>();
            ((string)result.Message).Should().Be("Fine deleted successfully.");
        }

        [Fact]
        public async Task PayFine_ShouldReturnOk_WhenModelStateIsValid()
        {
            // Arrange
            var payFineDto = new PayFineDto
            {
                FineID = 1,
                Amount = 50.00M
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/fines/pay", payFineDto);

            // Assert
            response.EnsureSuccessStatusCode();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<dynamic>();
            ((string)result.Message).Should().Be("Fine payment successful. Notification sent to the member.");
        }

        [Fact]
        public async Task PayFine_ShouldReturnBadRequest_WhenModelStateIsInvalid()
        {
            // Arrange
            var payFineDto = new PayFineDto
            {
                // Missing required fields
                FineID = 1
                // Amount is missing
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/fines/pay", payFineDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
