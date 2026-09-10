using System;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Exceptions;
using ApartmentResidenceManagement.Domain.Interfaces;
using ApartmentResidenceManagement.Application.Services;
using Moq;
using Xunit;

namespace ApartmentResidenceManagement.Tests.Services;

public class ResidentServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IResidentRepository> _mockResidentRepo;
    private readonly ResidentService _service;

    public ResidentServiceTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockResidentRepo = new Mock<IResidentRepository>();

        _mockUow.Setup(u => u.Residents).Returns(_mockResidentRepo.Object);

        _service = new ResidentService(_mockUow.Object);
    }

    [Fact]
    public async Task CreateResident_InvalidDateOfBirth_ShouldThrowBusinessRuleException()
    {
        // Arrange
        var resident = new Resident 
        { 
            FullName = "Nguyen Van A", 
            DateOfBirth = DateTime.Now.AddDays(1) // Trong tương lai
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateResidentAsync(resident));
        Assert.Contains("Ngày sinh không hợp lệ", ex.Message);
    }

    [Fact]
    public async Task CreateResident_InvalidPhoneNumber_ShouldThrowBusinessRuleException()
    {
        // Arrange
        var resident = new Resident 
        { 
            FullName = "Nguyen Van A", 
            DateOfBirth = new DateTime(1990, 1, 1),
            PhoneNumber = "1234" // Lỗi định dạng
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateResidentAsync(resident));
        Assert.Contains("Số điện thoại không hợp lệ", ex.Message);
    }

    [Fact]
    public async Task CreateResident_DuplicateIdentityCard_ShouldThrowBusinessRuleException()
    {
        // Arrange
        var resident = new Resident 
        { 
            FullName = "Nguyen Van A", 
            DateOfBirth = new DateTime(1990, 1, 1),
            IdentityCard = "123456789012"
        };
        _mockResidentRepo.Setup(r => r.GetByIdentityCardAsync("123456789012")).ReturnsAsync(resident);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateResidentAsync(resident));
        Assert.Contains("đã tồn tại trong hệ thống", ex.Message);
    }
}
