// FinalProject.Tests/Repositories/JwtSettingsTests.cs
using FinalProject.Application.Interfaces;
using FinalProject.Infrastructure.Settings;
using Xunit;

namespace FinalProject.Tests.Repositories
{
    public class JwtSettingsTests
    {
        [Fact]
        public void JwtSettings_Properties_SetAndGet()
        {
            // Arrange
            var jwtSettings = new JwtSettings();

            // Act
            jwtSettings.Issuer = "test-issuer";
            jwtSettings.Audience = "test-audience";
            jwtSettings.Key = "test-key";

            // Assert
            Assert.Equal("test-issuer", jwtSettings.Issuer);
            Assert.Equal("test-audience", jwtSettings.Audience);
            Assert.Equal("test-key", jwtSettings.Key);
        }
    }
}
