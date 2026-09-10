using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Exceptions;
using ApartmentResidenceManagement.Domain.Interfaces;
using ApartmentResidenceManagement.Application.Security;
using ApartmentResidenceManagement.Application.Services;
using Moq;
using Xunit;

namespace ApartmentResidenceManagement.Tests.Services;

public class AccountServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IUserAccountRepository> _mockAccountRepo;
    private readonly Mock<IResidentRepository> _mockResidentRepo;
    private readonly Mock<IPasswordHasher> _mockHasher;
    private readonly AccountService _service;

    public AccountServiceTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockAccountRepo = new Mock<IUserAccountRepository>();
        _mockResidentRepo = new Mock<IResidentRepository>();
        _mockHasher = new Mock<IPasswordHasher>();

        _mockUow.Setup(u => u.UserAccounts).Returns(_mockAccountRepo.Object);
        _mockUow.Setup(u => u.Residents).Returns(_mockResidentRepo.Object);

        _service = new AccountService(_mockUow.Object, _mockHasher.Object);
    }

    [Fact]
    public async Task Login_WithIncorrectPassword_ShouldThrowBusinessRuleException()
    {
        // Arrange
        string username = "testuser";
        string password = "wrongPassword";
        var account = new UserAccount { Username = username, PasswordHash = "hashed_pass", IsActive = true };

        _mockAccountRepo.Setup(r => r.GetByUsernameAsync(username)).ReturnsAsync(account);
        _mockHasher.Setup(h => h.VerifyPassword(password, account.PasswordHash)).Returns(false); // verify thất bại

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.LoginAsync(username, password));
        Assert.Contains("không chính xác", ex.Message);
    }

    [Fact]
    public async Task Login_AccountLocked_ShouldThrowBusinessRuleException()
    {
        // Arrange
        string username = "testuser";
        var account = new UserAccount { Username = username, IsActive = false }; // tài khoản bị khóa

        _mockAccountRepo.Setup(r => r.GetByUsernameAsync(username)).ReturnsAsync(account);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.LoginAsync(username, "pass"));
        Assert.Contains("đã bị khóa", ex.Message);
    }

    [Fact]
    public async Task CreateResidentAccount_ResidentAlreadyHasAccount_ShouldThrowBusinessRuleException()
    {
        // Arrange
        int residentId = 10;
        var resident = new Resident { Id = residentId, FullName = "Nguyen Van A" };
        var existingAccount = new UserAccount { ResidentId = residentId };

        _mockResidentRepo.Setup(r => r.GetByIdAsync(residentId)).ReturnsAsync(resident);
        _mockAccountRepo.Setup(r => r.GetByResidentIdAsync(residentId)).ReturnsAsync(existingAccount); // Đã có account

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => 
            _service.CreateResidentAccountAsync(residentId, "newuser", "newpass123"));

        Assert.Contains("đã có tài khoản đăng nhập", ex.Message);
    }

    [Fact]
    public async Task ToggleAccountStatus_AdminAccount_ShouldThrowBusinessRuleException()
    {
        // Arrange
        int accountId = 1;
        var adminAccount = new UserAccount { Id = accountId, Username = "admin", IsActive = true };

        _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(adminAccount);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.ToggleAccountStatusAsync(accountId));
        Assert.Contains("Không được phép vô hiệu hóa tài khoản quản trị", ex.Message);
    }
}
