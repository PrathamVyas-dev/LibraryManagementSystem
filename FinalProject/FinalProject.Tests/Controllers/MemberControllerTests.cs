using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FinalProject.Application.Dto.Member;
using FinalProject.API;
using FinalProject.Tests.Helpers;
using Xunit;
using FluentAssertions;

namespace FinalProject.Tests.Controllers
{
    public class MemberControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public MemberControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetAllMembers_ShouldReturnOk()
        {
            var response = await _client.GetAsync("/api/Member");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetMemberById_ShouldReturnOk_WhenMemberExists()
        {
            var response = await _client.GetAsync("/api/Member/1");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetMemberById_ShouldReturnNotFound_WhenMemberDoesNotExist()
        {
            var response = await _client.GetAsync("/api/Member/999");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task RegisterMember_ShouldReturnOk()
        {
            var registerDto = new RegisterMemberDto
            {
                Name = "John Doe",
                Email = "johndoe@example.com",
                Password = "Password123!",
                Phone = "1234567890",
                Address = "123 Main St"
            };

            var response = await _client.PostAsJsonAsync("/api/Member/register", registerDto);
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        }

        [Fact]
        public async Task UpdateMember_ShouldReturnNoContent()
        {
            var updateDto = new UpdateMemberDto
            {
                MemberID = 1,
                Name = "Updated Name",
                Phone = "9876543210",
                Address = "Updated Address"
            };

            var response = await _client.PutAsJsonAsync("/api/Member/1", updateDto);
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task DeleteMember_ShouldReturnNoContent()
        {
            var response = await _client.DeleteAsync("/api/Member/1");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task GetBorrowingsForMember_ShouldReturnOk()
        {
            var response = await _client.GetAsync("/api/Member/1/borrowings");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetOutstandingFinesForMember_ShouldReturnOk()
        {
            var response = await _client.GetAsync("/api/Member/1/fines");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        }

        [Fact]
        public async Task CheckAndUpdateMembershipStatus_ShouldReturnOk()
        {
            var response = await _client.PostAsync("/api/Member/check-membership-status", null);
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        }
    }
}
