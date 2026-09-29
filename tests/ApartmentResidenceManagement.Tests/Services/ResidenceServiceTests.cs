using System;
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

public class ResidenceServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IApartmentRepository> _mockApartmentRepo;
    private readonly Mock<IResidentRepository> _mockResidentRepo;
    private readonly Mock<IResidenceHistoryRepository> _mockResidenceHistoryRepo;
    private readonly ResidenceService _service;

    public ResidenceServiceTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockApartmentRepo = new Mock<IApartmentRepository>();
        _mockResidentRepo = new Mock<IResidentRepository>();
        _mockResidenceHistoryRepo = new Mock<IResidenceHistoryRepository>();

        _mockUow.Setup(u => u.Apartments).Returns(_mockApartmentRepo.Object);
        _mockUow.Setup(u => u.Residents).Returns(_mockResidentRepo.Object);
        _mockUow.Setup(u => u.ResidenceHistories).Returns(_mockResidenceHistoryRepo.Object);

        _service = new ResidenceService(_mockUow.Object);
    }

    [Fact]
    public async Task RegisterResidence_ValidData_ShouldSucceed()
    {
        // Arrange
        int apartmentId = 1;
        int residentId = 10;
        var apartment = new Apartment { Id = apartmentId, ApartmentNumber = "101", Status = ApartmentStatus.Empty };
        var resident = new Resident { Id = residentId, FullName = "Nguyen Van A", DateOfBirth = new DateTime(1990, 1, 1) };

        _mockApartmentRepo.Setup(r => r.GetByIdAsync(apartmentId)).ReturnsAsync(apartment);
        _mockResidentRepo.Setup(r => r.GetByIdAsync(residentId)).ReturnsAsync(resident);
        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByResidentIdAsync(residentId)).ReturnsAsync((ResidenceHistory?)null);
        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByApartmentIdAsync(apartmentId)).ReturnsAsync(new List<ResidenceHistory>());

        // Act
        var result = await _service.RegisterResidenceAsync(apartmentId, residentId, RelationshipType.Owner, DateTime.Now);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(apartmentId, result.ApartmentId);
        Assert.Equal(residentId, result.ResidentId);
        Assert.Equal(RelationshipType.Owner, result.RelationshipType);
        Assert.True(result.IsActive);
        Assert.Equal(ApartmentStatus.Occupied, apartment.Status); // Căn hộ phải đổi sang Occupied
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task RegisterResidence_ResidentAlreadyHasActiveResidence_ShouldThrowBusinessRuleException()
    {
        // Arrange
        int apartmentId = 1;
        int residentId = 10;
        var apartment = new Apartment { Id = apartmentId, ApartmentNumber = "101" };
        var resident = new Resident { Id = residentId, FullName = "Nguyen Van A" };
        var existingActive = new ResidenceHistory 
        { 
            ApartmentId = 2, 
            ResidentId = residentId, 
            IsActive = true,
            Apartment = new Apartment { Id = 2, ApartmentNumber = "202" }
        };

        _mockApartmentRepo.Setup(r => r.GetByIdAsync(apartmentId)).ReturnsAsync(apartment);
        _mockResidentRepo.Setup(r => r.GetByIdAsync(residentId)).ReturnsAsync(resident);
        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByResidentIdAsync(residentId)).ReturnsAsync(existingActive);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => 
            _service.RegisterResidenceAsync(apartmentId, residentId, RelationshipType.Tenant, DateTime.Now));
        
        Assert.Contains("hiện đang cư trú hoạt động tại căn hộ", ex.Message);
    }

    [Fact]
    public async Task RegisterResidence_ApartmentUnderMaintenance_ShouldThrowBusinessRuleException()
    {
        // Arrange
        int apartmentId = 1;
        int residentId = 10;
        var apartment = new Apartment { Id = apartmentId, ApartmentNumber = "101", Status = ApartmentStatus.UnderMaintenance };

        _mockApartmentRepo.Setup(r => r.GetByIdAsync(apartmentId)).ReturnsAsync(apartment);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => 
            _service.RegisterResidenceAsync(apartmentId, residentId, RelationshipType.Owner, DateTime.Now));

        Assert.Contains("đang sửa chữa", ex.Message);
    }

    [Fact]
    public async Task RegisterResidence_AddSecondOwnerToSameApartment_ShouldThrowBusinessRuleException()
    {
        // Arrange
        int apartmentId = 1;
        int residentId = 10;
        var apartment = new Apartment { Id = apartmentId, ApartmentNumber = "101", Status = ApartmentStatus.Occupied };
        var resident = new Resident { Id = residentId, FullName = "Nguyen Van B", DateOfBirth = new DateTime(1990, 1, 1) };
        var existingOwner = new ResidenceHistory { ApartmentId = apartmentId, ResidentId = 9, RelationshipType = RelationshipType.Owner, IsActive = true };

        _mockApartmentRepo.Setup(r => r.GetByIdAsync(apartmentId)).ReturnsAsync(apartment);
        _mockResidentRepo.Setup(r => r.GetByIdAsync(residentId)).ReturnsAsync(resident);
        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByResidentIdAsync(residentId)).ReturnsAsync((ResidenceHistory?)null);
        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByApartmentIdAsync(apartmentId)).ReturnsAsync(new List<ResidenceHistory> { existingOwner });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => 
            _service.RegisterResidenceAsync(apartmentId, residentId, RelationshipType.Owner, DateTime.Now));

        Assert.Contains("đã có Chủ hộ đang hoạt động", ex.Message);
    }

    [Fact]
    public async Task TransferApartment_ValidData_ShouldCloseOldAndOpenNew()
    {
        // Arrange
        int residentId = 10;
        int oldApartmentId = 1;
        int newApartmentId = 2;
        var oldApartment = new Apartment { Id = oldApartmentId, ApartmentNumber = "101", Status = ApartmentStatus.Occupied };
        var newApartment = new Apartment { Id = newApartmentId, ApartmentNumber = "202", Status = ApartmentStatus.Empty };
        
        var oldActiveResidence = new ResidenceHistory 
        { 
            Id = 5,
            ApartmentId = oldApartmentId, 
            ResidentId = residentId, 
            IsActive = true, 
            StartDate = DateTime.Now.AddDays(-10),
            Apartment = oldApartment
        };

        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByResidentIdAsync(residentId)).ReturnsAsync(oldActiveResidence);
        _mockApartmentRepo.Setup(r => r.GetByIdAsync(newApartmentId)).ReturnsAsync(newApartment);
        _mockApartmentRepo.Setup(r => r.GetByIdAsync(oldApartmentId)).ReturnsAsync(oldApartment);
        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByApartmentIdAsync(oldApartmentId)).ReturnsAsync(new List<ResidenceHistory> { oldActiveResidence });
        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByApartmentIdAsync(newApartmentId)).ReturnsAsync(new List<ResidenceHistory>());

        // Act
        var result = await _service.TransferApartmentAsync(residentId, newApartmentId, RelationshipType.Owner, DateTime.Now);

        // Assert
        Assert.False(oldActiveResidence.IsActive);
        Assert.NotNull(oldActiveResidence.EndDate);
        Assert.Equal(ApartmentStatus.Empty, oldApartment.Status); // Căn hộ cũ không còn ai ở
        Assert.Equal(ApartmentStatus.Occupied, newApartment.Status); // Căn hộ mới có người ở
        Assert.NotNull(result);
        Assert.True(result.IsActive);
        Assert.Equal(newApartmentId, result.ApartmentId);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task TransferApartment_TargetDoesNotExist_ShouldNotMutateCurrentResidence()
    {
        var currentResidence = new ResidenceHistory
        {
            Id = 5,
            ApartmentId = 1,
            ResidentId = 10,
            IsActive = true,
            StartDate = DateTime.Today.AddMonths(-1)
        };

        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByResidentIdAsync(10))
            .ReturnsAsync(currentResidence);
        _mockApartmentRepo.Setup(r => r.GetByIdAsync(2))
            .ReturnsAsync((Apartment?)null);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.TransferApartmentAsync(10, 2, RelationshipType.Tenant, DateTime.Today));

        Assert.True(currentResidence.IsActive);
        Assert.Null(currentResidence.EndDate);
        _mockResidenceHistoryRepo.Verify(r => r.Update(It.IsAny<ResidenceHistory>()), Times.Never);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task TransferApartment_DateBeforeCurrentResidence_ShouldNotMutateCurrentResidence()
    {
        var currentResidence = new ResidenceHistory
        {
            Id = 5,
            ApartmentId = 1,
            ResidentId = 10,
            IsActive = true,
            StartDate = DateTime.Today.AddDays(-5)
        };

        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByResidentIdAsync(10))
            .ReturnsAsync(currentResidence);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.TransferApartmentAsync(10, 2, RelationshipType.Tenant, DateTime.Today.AddDays(-6)));

        Assert.True(currentResidence.IsActive);
        Assert.Null(currentResidence.EndDate);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task RegisterResidence_OverlappingHistory_ShouldRejectOperation()
    {
        var apartment = new Apartment { Id = 1, ApartmentNumber = "101", Status = ApartmentStatus.Empty };
        var resident = new Resident { Id = 10, DateOfBirth = new DateTime(1990, 1, 1) };
        _mockApartmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(apartment);
        _mockResidentRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(resident);
        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByResidentIdAsync(10)).ReturnsAsync((ResidenceHistory?)null);
        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByApartmentIdAsync(1))
            .ReturnsAsync(new List<ResidenceHistory>());
        _mockResidenceHistoryRepo.Setup(r => r.GetHistoryByResidentIdAsync(10))
            .ReturnsAsync(new List<ResidenceHistory>
            {
                new() { ResidentId = 10, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 6, 30), IsActive = false }
            });

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.RegisterResidenceAsync(1, 10, RelationshipType.Tenant, new DateTime(2026, 3, 1)));

        Assert.Contains("chồng lấn", ex.Message);
        Assert.Equal(ApartmentStatus.Empty, apartment.Status);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task TerminateResidence_WithRemainingResident_ShouldKeepApartmentOccupied()
    {
        var residence = new ResidenceHistory
        {
            Id = 5,
            ApartmentId = 1,
            ResidentId = 10,
            StartDate = DateTime.Today.AddMonths(-1),
            IsActive = true
        };
        var remainingResidence = new ResidenceHistory { Id = 6, ApartmentId = 1, ResidentId = 11, IsActive = true };
        var apartment = new Apartment { Id = 1, Status = ApartmentStatus.Empty };
        _mockResidenceHistoryRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(residence);
        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByApartmentIdAsync(1))
            .ReturnsAsync(new List<ResidenceHistory> { residence, remainingResidence });
        _mockApartmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(apartment);

        await _service.TerminateResidenceAsync(5, DateTime.Today);

        Assert.False(residence.IsActive);
        Assert.Equal(DateTime.Today, residence.EndDate);
        Assert.Equal(ApartmentStatus.Occupied, apartment.Status);
    }

    [Fact]
    public async Task TransferApartment_WithRemainingResident_ShouldKeepOldApartmentOccupied()
    {
        var oldResidence = new ResidenceHistory
        {
            Id = 5,
            ApartmentId = 1,
            ResidentId = 10,
            StartDate = DateTime.Today.AddMonths(-1),
            IsActive = true
        };
        var oldApartment = new Apartment { Id = 1, ApartmentNumber = "101", Status = ApartmentStatus.Empty };
        var newApartment = new Apartment { Id = 2, ApartmentNumber = "202", Status = ApartmentStatus.Empty };
        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByResidentIdAsync(10)).ReturnsAsync(oldResidence);
        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByApartmentIdAsync(1))
            .ReturnsAsync(new List<ResidenceHistory>
            {
                oldResidence,
                new() { Id = 6, ApartmentId = 1, ResidentId = 11, IsActive = true }
            });
        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByApartmentIdAsync(2))
            .ReturnsAsync(new List<ResidenceHistory>());
        _mockApartmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(oldApartment);
        _mockApartmentRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(newApartment);

        await _service.TransferApartmentAsync(10, 2, RelationshipType.Tenant, DateTime.Today);

        Assert.Equal(ApartmentStatus.Occupied, oldApartment.Status);
        Assert.Equal(ApartmentStatus.Occupied, newApartment.Status);
    }

    [Fact]
    public async Task TransferApartment_TargetUnderMaintenance_ShouldNotMutateCurrentResidence()
    {
        var currentResidence = new ResidenceHistory
        {
            Id = 5,
            ApartmentId = 1,
            ResidentId = 10,
            StartDate = DateTime.Today.AddMonths(-1),
            IsActive = true
        };
        _mockResidenceHistoryRepo.Setup(r => r.GetActiveByResidentIdAsync(10)).ReturnsAsync(currentResidence);
        _mockApartmentRepo.Setup(r => r.GetByIdAsync(2))
            .ReturnsAsync(new Apartment { Id = 2, Status = ApartmentStatus.UnderMaintenance });

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.TransferApartmentAsync(10, 2, RelationshipType.Tenant, DateTime.Today));

        Assert.True(currentResidence.IsActive);
        Assert.Null(currentResidence.EndDate);
        _mockUow.Verify(u => u.CompleteAsync(), Times.Never);
    }
}
