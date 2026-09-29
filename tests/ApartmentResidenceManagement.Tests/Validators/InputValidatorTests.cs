using ApartmentResidenceManagement.Application.Validators;

namespace ApartmentResidenceManagement.Tests.Validators;

public class InputValidatorTests
{
    [Theory]
    [InlineData("0321234567")]
    [InlineData("0551234567")]
    [InlineData("0987654321")]
    public void ValidatePhone_ValidVietnameseMobileNumber_ReturnsTrue(string phone)
    {
        Assert.True(InputValidator.ValidatePhone(phone));
    }

    [Theory]
    [InlineData("0|21234567")]
    [InlineData("0212345678")]
    [InlineData("098765432")]
    [InlineData("09876543210")]
    public void ValidatePhone_InvalidNumber_ReturnsFalse(string phone)
    {
        Assert.False(InputValidator.ValidatePhone(phone));
    }

    [Fact]
    public void ValidateDateOfBirth_Today_ReturnsTrue()
    {
        Assert.True(InputValidator.ValidateDateOfBirth(DateTime.Today));
    }
}
