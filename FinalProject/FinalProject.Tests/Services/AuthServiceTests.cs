using FinalProject.Application.Dto.Member;
using FinalProject.Application.Interfaces;
using FinalProject.Application.Services;
using FinalProject.Domain;
using FinalProject.Infrastructure.DbContexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace FinalProject.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<UserManager<IdentityUser>> _mockUserManager;
        private readonly Mock<ITokenRepository> _mockTokenRepository;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly DbContextOptions<ApplicationDbContext> _options;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            // Setup UserManager mock
            var userStore = new Mock<IUserStore<IdentityUser>>();
            _mockUserManager = new Mock<UserManager<IdentityUser>>(
                userStore.Object, null, null, null, null, null, null, null, null);

            _mockTokenRepository = new Mock<ITokenRepository>();
            _mockServiceProvider = new Mock<IServiceProvider>();

            // Set up in-memory database
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            // Setup ServiceProvider with DbContext
            var mockScope = new Mock<IServiceScope>();
            var mockServiceScopeFactory = new Mock<IServiceScopeFactory>();

            mockScope.Setup(s => s.ServiceProvider).Returns(_mockServiceProvider.Object);
            mockServiceScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
                .Returns(mockServiceScopeFactory.Object);

            // Create the AuthService with mocked dependencies
            _authService = new AuthService(
                _mockUserManager.Object,
                _mockTokenRepository.Object,
                _mockServiceProvider.Object
            );
        }

        private void SetupDbContext()
        {
            // Create a new instance of DbContext for each test
            var dbContext = new ApplicationDbContext(_options);
            dbContext.Database.EnsureCreated();

            _mockServiceProvider.Setup(x => x.GetRequiredService(typeof(ApplicationDbContext)))
                .Returns(dbContext);
        }

        [Fact]
        public async Task RegisterAsync_ValidInput_ReturnsTokenResponse()
        {
            // Arrange
            SetupDbContext();

            var registerDto = new RegisterMemberDto
            {
                Name = "Test User",
                Email = "test@example.com",
                Password = "Password123!",
                Phone = "1234567890",
                Address = "Test Address",
                Role = "User"
            };

            var testUser = new IdentityUser
            {
                Id = "testid",
                UserName = registerDto.Email,
                Email = registerDto.Email
            };

            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), registerDto.Password))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<IdentityUser>(), "User"))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(x => x.GetRolesAsync(It.IsAny<IdentityUser>()))
                .ReturnsAsync(new List<string> { "User" });

            _mockTokenRepository.Setup(x => x.CreateJwtToken(It.IsAny<IdentityUser>(), It.IsAny<List<string>>()))
                .Returns("test-jwt-token");

            // Act
            var result = await _authService.RegisterAsync(registerDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(registerDto.Email, result.Email);
            Assert.Contains("User", result.Roles);
            Assert.Equal("test-jwt-token", result.Token);

            _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<IdentityUser>(), registerDto.Password), Times.Once);
            _mockUserManager.Verify(x => x.AddToRoleAsync(It.IsAny<IdentityUser>(), "User"), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_InvalidRole_ThrowsException()
        {
            // Arrange
            SetupDbContext();

            var registerDto = new RegisterMemberDto
            {
                Name = "Test User",
                Email = "test@example.com",
                Password = "Password123!",
                Phone = "1234567890",
                Address = "Test Address",
                Role = "InvalidRole" // Role that doesn't exist
            };

            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), registerDto.Password))
                .ReturnsAsync(IdentityResult.Success);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _authService.RegisterAsync(registerDto));
        }

        [Fact]
        public async Task RegisterAsync_UserCreationFails_ThrowsException()
        {
            // Arrange
            SetupDbContext();

            var registerDto = new RegisterMemberDto
            {
                Name = "Test User",
                Email = "test@example.com",
                Password = "weak", // Too weak password
                Phone = "1234567890",
                Address = "Test Address"
            };

            var errors = new List<IdentityError>
            {
                new IdentityError { Description = "Password too weak" }
            };

            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), registerDto.Password))
                .ReturnsAsync(IdentityResult.Failed(errors.ToArray()));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _authService.RegisterAsync(registerDto));
            Assert.Contains("Password too weak", exception.Message);
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsTokenResponse()
        {
            // Arrange
            var loginDto = new LoginMemberDto
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            var testUser = new IdentityUser
            {
                Id = "testid",
                UserName = loginDto.Email,
                Email = loginDto.Email
            };

            _mockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Email))
                .ReturnsAsync(testUser);

            _mockUserManager.Setup(x => x.CheckPasswordAsync(testUser, loginDto.Password))
                .ReturnsAsync(true);

            _mockUserManager.Setup(x => x.GetRolesAsync(testUser))
                .ReturnsAsync(new List<string> { "User" });

            _mockTokenRepository.Setup(x => x.CreateJwtToken(testUser, It.IsAny<List<string>>()))
                .Returns("test-jwt-token");

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(loginDto.Email, result.Email);
            Assert.Contains("User", result.Roles);
            Assert.Equal("test-jwt-token", result.Token);
        }

        [Fact]
        public async Task LoginAsync_UserNotFound_ThrowsException()
        {
            // Arrange
            var loginDto = new LoginMemberDto
            {
                Email = "nonexistent@example.com",
                Password = "Password123!"
            };

            _mockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Email))
                .ReturnsAsync((IdentityUser)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _authService.LoginAsync(loginDto));
            Assert.Contains("Invalid credentials", exception.Message);
        }

        [Fact]
        public async Task LoginAsync_IncorrectPassword_ThrowsException()
        {
            // Arrange
            var loginDto = new LoginMemberDto
            {
                Email = "test@example.com",
                Password = "WrongPassword123!"
            };

            var testUser = new IdentityUser
            {
                Id = "testid",
                UserName = loginDto.Email,
                Email = loginDto.Email
            };

            _mockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Email))
                .ReturnsAsync(testUser);

            _mockUserManager.Setup(x => x.CheckPasswordAsync(testUser, loginDto.Password))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _authService.LoginAsync(loginDto));
            Assert.Contains("Invalid credentials", exception.Message);
        }

        [Fact]
        public async Task LogoutAsync_DeletesToken()
        {
            // Arrange
            var userId = "test-user-id";
            _mockTokenRepository.Setup(x => x.DeleteTokenAsync(userId))
                .Returns(Task.CompletedTask);

            // Act
            await _authService.LogoutAsync(userId);

            // Assert
            _mockTokenRepository.Verify(x => x.DeleteTokenAsync(userId), Times.Once);
        }
    }
}
