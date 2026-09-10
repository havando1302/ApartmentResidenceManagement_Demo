using ApartmentResidenceManagement.Application.Security;
using Xunit;

namespace ApartmentResidenceManagement.Tests.Security;

public class BCryptPasswordHasherTests
{
    private readonly IPasswordHasher _hasher;

    public BCryptPasswordHasherTests()
    {
        _hasher = new BCryptPasswordHasher();
    }

    [Fact]
    public void HashPassword_ShouldReturnHashedString()
    {
        // Arrange
        string password = "mySecurePassword123";

        // Act
        string hash = _hasher.HashPassword(password);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.NotEqual(password, hash);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        string password = "mySecurePassword123";
        string hash = _hasher.HashPassword(password);

        // Act
        bool isValid = _hasher.VerifyPassword(password, hash);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        string password = "mySecurePassword123";
        string wrongPassword = "wrongPassword";
        string hash = _hasher.HashPassword(password);

        // Act
        bool isValid = _hasher.VerifyPassword(wrongPassword, hash);

        // Assert
        Assert.False(isValid);
    }
}
