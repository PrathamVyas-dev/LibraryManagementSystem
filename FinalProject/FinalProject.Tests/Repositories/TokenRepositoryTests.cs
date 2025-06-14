// FinalProject.Tests/Repositories/TokenRepositoryTests.cs
using FinalProject.Application.Interfaces;
using FinalProject.Infrastructure.Repository;
using FinalProject.Infrastructure.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace FinalProject.Tests.Repositories
{
    public class TokenRepositoryTests
    {
        private readonly IJwtSettings _jwtSettings;
        private readonly TokenRepository _tokenRepository;

        public TokenRepositoryTests()
        {
            // Create real JwtSettings for testing
            _jwtSettings = new JwtSettings
            {
                Issuer = "test-issuer",
                Audience = "test-audience",
                Key = "this-is-a-very-long-secret-key-for-testing-purposes-only-do-not-use-in-production"
            };

            _tokenRepository = new TokenRepository(_jwtSettings);
        }

        [Fact]
        public async Task StoreTokenAsync_SavesToken()
        {
            // Arrange
            var userId = "test-user";
            var token = "test-token";

            // Act
            await _tokenRepository.StoreTokenAsync(userId, token);
            var retrieved = await _tokenRepository.GetTokenAsync(userId);

            // Assert
            Assert.Equal(token, retrieved);
        }

        [Fact]
        public async Task GetTokenAsync_NonExistentUser_ReturnsNull()
        {
            // Arrange & Act
            var result = await _tokenRepository.GetTokenAsync("non-existent-user");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteTokenAsync_RemovesToken()
        {
            // Arrange
            var userId = "user-to-delete";
            var token = "token-to-delete";

            // Act
            await _tokenRepository.StoreTokenAsync(userId, token);
            await _tokenRepository.DeleteTokenAsync(userId);
            var result = await _tokenRepository.GetTokenAsync(userId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void CreateJwtToken_GeneratesValidToken()
        {
            // Arrange
            var user = new IdentityUser
            {
                Id = "user-id",
                Email = "test@example.com",
                UserName = "test@example.com"
            };

            var roles = new List<string> { "User" };

            // Act
            var token = _tokenRepository.CreateJwtToken(user, roles);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);

            var tokenHandler = new JwtSecurityTokenHandler();
            var canReadToken = tokenHandler.CanReadToken(token);
            Assert.True(canReadToken);

            // Decode and verify the token
            var jwtToken = tokenHandler.ReadJwtToken(token);

            Assert.Equal(_jwtSettings.Issuer, jwtToken.Issuer);
            Assert.Equal(_jwtSettings.Audience, jwtToken.Audience);

            var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            Assert.NotNull(emailClaim);
            Assert.Equal(user.Email, emailClaim.Value);

            var idClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            Assert.NotNull(idClaim);
            Assert.Equal(user.Id, idClaim.Value);

            var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            Assert.NotNull(roleClaim);
            Assert.Equal("User", roleClaim.Value);
        }

        [Fact]
        public void CreateJwtToken_MultipleRoles_AllIncludedInToken()
        {
            // Arrange
            var user = new IdentityUser
            {
                Id = "admin-id",
                Email = "admin@example.com",
                UserName = "admin@example.com"
            };

            var roles = new List<string> { "Admin", "User" };

            // Act
            var token = _tokenRepository.CreateJwtToken(user, roles);
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);

            // Assert
            var roleClaims = jwtToken.Claims.Where(c => c.Type == ClaimTypes.Role).ToList();
            Assert.Equal(2, roleClaims.Count);
            Assert.Contains(roleClaims, c => c.Value == "Admin");
            Assert.Contains(roleClaims, c => c.Value == "User");
        }

        [Fact]
        public void CreateJwtToken_TokenCanBeValidatedWithCorrectKey()
        {
            // Arrange
            var user = new IdentityUser
            {
                Id = "user-id",
                Email = "test@example.com",
                UserName = "test@example.com"
            };

            var roles = new List<string> { "User" };

            // Act
            var token = _tokenRepository.CreateJwtToken(user, roles);

            // Assert - Validate the token
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key)),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            // This will throw an exception if validation fails
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            Assert.NotNull(principal);
            Assert.IsType<JwtSecurityToken>(validatedToken);
        }
    }
}
