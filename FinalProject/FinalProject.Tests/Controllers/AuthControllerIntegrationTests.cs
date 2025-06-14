using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FinalProject.API;
using FinalProject.Application.Dto.Member;
using FinalProject.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Xunit;
using Newtonsoft.Json;
using System.Linq;

namespace FinalProject.Tests.Controllers
{
    public class AuthControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory<Program> _factory;
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public AuthControllerIntegrationTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Register_ValidUser_ReturnsSuccess()
        {
            // Arrange
            var registerDto = new RegisterMemberDto
            {
                Name = "Test User",
                Email = "newuser@test.com",
                Password = "TestPassword123!",
                Phone = "1234567890",
                Address = "123 Test St",
                Role = "User"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<dynamic>(content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Registration successful", (string)result.Message);
            Assert.NotNull((string)result.Token);
            Assert.Contains("User", (string[])result.Roles.ToObject<string[]>());
        }

        [Fact]
        public async Task Register_DuplicateEmail_ReturnsBadRequest()
        {
            // Arrange - First register a user
            var registerDto = new RegisterMemberDto
            {
                Name = "Duplicate User",
                Email = "duplicate@test.com",
                Password = "Password123!",
                Phone = "1234567890",
                Address = "123 Test St"
            };

            await _client.PostAsJsonAsync("/api/auth/register", registerDto);

            // Act - Try to register with the same email
            var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("is already taken", content);
        }

        [Fact]
        public async Task Register_InvalidRole_ReturnsBadRequest()
        {
            // Arrange
            var registerDto = new RegisterMemberDto
            {
                Name = "Invalid Role User",
                Email = "invalidrole@test.com",
                Password = "Password123!",
                Phone = "1234567890",
                Address = "123 Test St",
                Role = "InvalidRole" // Role that doesn't exist
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Invalid role specified", content);
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsToken()
        {
            // Arrange - Register a user first
            var email = "logintest@test.com";
            var password = "Password123!";

            var registerDto = new RegisterMemberDto
            {
                Name = "Login Test User",
                Email = email,
                Password = password,
                Phone = "1234567890",
                Address = "123 Test St"
            };

            await _client.PostAsJsonAsync("/api/auth/register", registerDto);

            var loginDto = new LoginMemberDto
            {
                Email = email,
                Password = password
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<TokenResponseDto>(content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(email, result.Email);
            Assert.NotNull(result.Token);
            Assert.Contains("User", result.Roles);
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var loginDto = new LoginMemberDto
            {
                Email = "nonexistent@test.com",
                Password = "WrongPassword123!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Invalid credentials", content);
        }

        [Fact]
        public async Task Logout_AuthenticatedUser_ReturnsSuccess()
        {
            // Arrange - Register and login a user to get token
            var email = "logouttest@test.com";
            var password = "Password123!";

            var registerDto = new RegisterMemberDto
            {
                Name = "Logout Test User",
                Email = email,
                Password = password,
                Phone = "1234567890",
                Address = "123 Test St"
            };

            await _client.PostAsJsonAsync("/api/auth/register", registerDto);

            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginMemberDto
            {
                Email = email,
                Password = password
            });

            var loginContent = await loginResponse.Content.ReadAsStringAsync();
            var tokenResult = JsonConvert.DeserializeObject<TokenResponseDto>(loginContent);

            // Set the authentication header with the token
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", tokenResult.Token);

            // Act
            var logoutResponse = await _client.PostAsync("/api/auth/logout", null);
            var logoutContent = await logoutResponse.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<dynamic>(logoutContent);

            // Assert
            Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);
            Assert.Equal("Logout successful", (string)result.Message);

            // Clear the authentication header for other tests
            _client.DefaultRequestHeaders.Authorization = null;
        }

        [Fact]
        public async Task Logout_UnauthenticatedUser_ReturnsUnauthorized()
        {
            // Arrange - No authentication token

            // Act
            var response = await _client.PostAsync("/api/auth/logout", null);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
