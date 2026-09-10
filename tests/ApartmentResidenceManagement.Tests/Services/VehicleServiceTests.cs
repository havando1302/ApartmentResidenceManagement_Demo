using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Exceptions;
using ApartmentResidenceManagement.Domain.Interfaces;
using ApartmentResidenceManagement.Application.Services;
using Moq;
using Xunit;

namespace ApartmentResidenceManagement.Tests.Services;

public class VehicleServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IVehicleRepository> _mockVehicleRepo;
    private readonly Mock<IResidenceHistoryRepository> _mockResidenceHistoryRepo;
    private readonly VehicleService _service;

    public VehicleServiceTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockVehicleRepo = new Mock<IVehicleRepository>();
        _mockResidenceHistoryRepo = new Mock<IResidenceHistoryRepository>();

        _mockUow.Setup(u => u.Vehicles).Returns(_mockVehicleRepo.Object);
        _mockUow.Setup(u => u.ResidenceHistories).Returns(_mockResidenceHistoryRepo.Object);

        _service = new VehicleService(_mockUow.Object);
    }

    [Fact]
    public async Task CreateVehicle_OwnerNotActive_ShouldThrowBusinessRuleException()
    {
        // Arrange
        int ownerId = 10;
        var vehicle = new Vehicle { LicensePlate = "29A1-12345", OwnerId = ownerId };
        
        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByResidentIdAsync(ownerId)).ReturnsAsync((ResidenceHistory?)null); // Chủ xe không active

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateVehicleAsync(vehicle));
        Assert.Contains("Chỉ cư dân đang cư trú hoạt động", ex.Message);
    }

    [Fact]
    public async Task CreateVehicle_DuplicateLicensePlate_ShouldThrowBusinessRuleException()
    {
        // Arrange
        int ownerId = 10;
        var vehicle = new Vehicle { LicensePlate = "29A1-12345", OwnerId = ownerId };
        var activeResidence = new ResidenceHistory { IsActive = true };

        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByResidentIdAsync(ownerId)).ReturnsAsync(activeResidence);
        _mockVehicleRepo.Setup(r => r.GetByLicensePlateAsync("29A1-12345")).ReturnsAsync(vehicle);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateVehicleAsync(vehicle));
        Assert.Contains("đã tồn tại trong hệ thống", ex.Message);
    }
}
