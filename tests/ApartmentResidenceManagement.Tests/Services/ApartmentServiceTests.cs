using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Domain.Exceptions;
using ApartmentResidenceManagement.Domain.Interfaces;
using ApartmentResidenceManagement.Application.Services;
using Moq;
using Xunit;

namespace ApartmentResidenceManagement.Tests.Services;

public class ApartmentServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IApartmentRepository> _mockApartmentRepo;
    private readonly Mock<IResidenceHistoryRepository> _mockResidenceHistoryRepo;
    private readonly ApartmentService _service;

    public ApartmentServiceTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockApartmentRepo = new Mock<IApartmentRepository>();
        _mockResidenceHistoryRepo = new Mock<IResidenceHistoryRepository>();

        _mockUow.Setup(u => u.Apartments).Returns(_mockApartmentRepo.Object);
        _mockUow.Setup(u => u.ResidenceHistories).Returns(_mockResidenceHistoryRepo.Object);

        _service = new ApartmentService(_mockUow.Object);
    }

    [Fact]
    public async Task CreateApartment_DuplicateNumber_ShouldThrowBusinessRuleException()
    {
        // Arrange
        var apartment = new Apartment { ApartmentNumber = "101", Floor = 1, Area = 50 };
        _mockApartmentRepo.Setup(r => r.GetByApartmentNumberAsync("101")).ReturnsAsync(apartment);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateApartmentAsync(apartment));
        Assert.Contains("đã tồn tại trong hệ thống", ex.Message);
    }

    [Fact]
    public async Task DeleteApartment_WithActiveResidents_ShouldThrowBusinessRuleException()
    {
        // Arrange
        int apartmentId = 1;
        var apartment = new Apartment { Id = apartmentId, ApartmentNumber = "101" };
        var activeResidences = new List<ResidenceHistory> { new ResidenceHistory { ApartmentId = apartmentId } };

        _mockApartmentRepo.Setup(r => r.GetByIdAsync(apartmentId)).ReturnsAsync(apartment);
        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByApartmentIdAsync(apartmentId)).ReturnsAsync(activeResidences);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.DeleteApartmentAsync(apartmentId));
        Assert.Contains("đang có cư dân đang hoạt động cư trú", ex.Message);
    }

    [Fact]
    public async Task UpdateApartment_OccupiedWithoutActiveResidents_ShouldThrowBusinessRuleException()
    {
        var existing = new Apartment { Id = 1, ApartmentNumber = "101", Floor = 1, Area = 50, Status = ApartmentStatus.Empty };
        var update = new Apartment { Id = 1, ApartmentNumber = "101", Floor = 1, Area = 50, Status = ApartmentStatus.Occupied };
        _mockApartmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByApartmentIdAsync(1))
            .ReturnsAsync(new List<ResidenceHistory>());

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.UpdateApartmentAsync(update));

        Assert.Contains("chưa có cư trú hoạt động", ex.Message);
        Assert.Equal(ApartmentStatus.Empty, existing.Status);
    }

    [Fact]
    public async Task UpdateApartment_MaintenanceWithActiveResidents_ShouldThrowBusinessRuleException()
    {
        var existing = new Apartment { Id = 1, ApartmentNumber = "101", Floor = 1, Area = 50, Status = ApartmentStatus.Occupied };
        var update = new Apartment { Id = 1, ApartmentNumber = "101", Floor = 1, Area = 50, Status = ApartmentStatus.UnderMaintenance };
        _mockApartmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByApartmentIdAsync(1))
            .ReturnsAsync(new List<ResidenceHistory> { new() { ApartmentId = 1, IsActive = true } });

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.UpdateApartmentAsync(update));

        Assert.Contains("còn cư dân đang ở", ex.Message);
        Assert.Equal(ApartmentStatus.Occupied, existing.Status);
    }

    [Fact]
    public async Task CreateApartment_NonFiniteArea_ShouldThrowBusinessRuleException()
    {
        var apartment = new Apartment { ApartmentNumber = "101", Floor = 1, Area = double.NaN };

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateApartmentAsync(apartment));

        Assert.Contains("Diện tích", ex.Message);
    }
}
