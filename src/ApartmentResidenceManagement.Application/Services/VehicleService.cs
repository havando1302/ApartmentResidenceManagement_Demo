using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Exceptions;
using ApartmentResidenceManagement.Domain.Interfaces;
using ApartmentResidenceManagement.Application.Validators;

namespace ApartmentResidenceManagement.Application.Services;

public class VehicleService
{
    private readonly IUnitOfWork _unitOfWork;

    public VehicleService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<Vehicle>> GetAllVehiclesAsync()
    {
        return await _unitOfWork.Vehicles.GetAllWithDetailsAsync();
    }

    public async Task<Vehicle?> GetVehicleByIdAsync(int id)
    {
        return await _unitOfWork.Vehicles.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Vehicle>> GetVehiclesByOwnerIdAsync(int ownerId)
    {
        return await _unitOfWork.Vehicles.GetVehiclesByOwnerIdAsync(ownerId);
    }

    public async Task<Vehicle> CreateVehicleAsync(Vehicle vehicle)
    {
        if (string.IsNullOrWhiteSpace(vehicle.LicensePlate))
        {
            throw new BusinessRuleException("Biển số xe không được để trống.");
        }

        // Chuẩn hóa biển số trước khi validate và lưu
        vehicle.LicensePlate = vehicle.LicensePlate.Replace(" ", "").ToUpper();

        if (!InputValidator.ValidateLicensePlate(vehicle.LicensePlate))
        {
            throw new BusinessRuleException($"Biển số xe '{vehicle.LicensePlate}' không đúng định dạng (Ví dụ đúng: 29A1-12345 hoặc 29A-1234).");
        }

        // Kiểm tra trùng biển số xe
        var existing = await _unitOfWork.Vehicles.GetByLicensePlateAsync(vehicle.LicensePlate);
        if (existing != null)
        {
            throw new BusinessRuleException($"Biển số xe '{vehicle.LicensePlate}' đã tồn tại trong hệ thống.");
        }

        // Business Rule: Chủ xe (Resident) phải đang cư trú hoạt động tại tòa nhà
        var activeResidence = await _unitOfWork.ResidenceHistories.GetActiveByResidentIdAsync(vehicle.OwnerId);
        if (activeResidence == null)
        {
            throw new BusinessRuleException("Chỉ cư dân đang cư trú hoạt động tại tòa nhà mới được phép đăng ký phương tiện.");
        }

        await _unitOfWork.Vehicles.AddAsync(vehicle);
        await _unitOfWork.CompleteAsync();

        return vehicle;
    }

    public async Task UpdateVehicleAsync(Vehicle vehicle)
    {
        var existing = await _unitOfWork.Vehicles.GetByIdAsync(vehicle.Id);
        if (existing == null)
        {
            throw new BusinessRuleException("Không tìm thấy phương tiện cần cập nhật.");
        }

        if (string.IsNullOrWhiteSpace(vehicle.LicensePlate))
        {
            throw new BusinessRuleException("Biển số xe không được để trống.");
        }

        vehicle.LicensePlate = vehicle.LicensePlate.Replace(" ", "").ToUpper();

        if (!InputValidator.ValidateLicensePlate(vehicle.LicensePlate))
        {
            throw new BusinessRuleException("Biển số xe không đúng định dạng.");
        }

        // Kiểm tra trùng biển số khi đổi biển số mới
        if (existing.LicensePlate != vehicle.LicensePlate)
        {
            var duplicate = await _unitOfWork.Vehicles.GetByLicensePlateAsync(vehicle.LicensePlate);
            if (duplicate != null)
            {
                throw new BusinessRuleException($"Biển số xe '{vehicle.LicensePlate}' đã được đăng ký bởi phương tiện khác.");
            }
        }

        // Kiểm tra xem chủ sở hữu xe có đang cư trú active không
        var activeResidence = await _unitOfWork.ResidenceHistories.GetActiveByResidentIdAsync(vehicle.OwnerId);
        if (activeResidence == null)
        {
            throw new BusinessRuleException("Cư dân sở hữu xe phải đang cư trú hoạt động tại tòa nhà.");
        }

        existing.LicensePlate = vehicle.LicensePlate;
        existing.VehicleType = vehicle.VehicleType;
        existing.Brand = vehicle.Brand;
        existing.OwnerId = vehicle.OwnerId;

        _unitOfWork.Vehicles.Update(existing);
        await _unitOfWork.CompleteAsync();
    }

    public async Task DeleteVehicleAsync(int id)
    {
        var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(id);
        if (vehicle == null)
        {
            throw new BusinessRuleException("Không tìm thấy phương tiện cần xóa.");
        }

        _unitOfWork.Vehicles.Delete(vehicle);
        await _unitOfWork.CompleteAsync();
    }
}
