using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
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
        return await CreateVehicleCoreAsync(vehicle, VehicleRegistrationStatus.Approved);
    }

    public async Task<Vehicle> SubmitVehicleRegistrationAsync(Vehicle vehicle)
    {
        return await CreateVehicleCoreAsync(vehicle, VehicleRegistrationStatus.Pending);
    }

    public async Task UpdateVehicleAsync(Vehicle vehicle)
    {
        var existing = await _unitOfWork.Vehicles.GetByIdAsync(vehicle.Id);
        if (existing == null)
        {
            throw new BusinessRuleException("Không tìm thấy phương tiện cần cập nhật.");
        }

        await ValidateVehicleDataAsync(vehicle, existing.Id);
        existing.LicensePlate = vehicle.LicensePlate;
        existing.VehicleType = vehicle.VehicleType;
        existing.Brand = vehicle.Brand;
        existing.OwnerId = vehicle.OwnerId;

        _unitOfWork.Vehicles.Update(existing);
        await _unitOfWork.CompleteAsync();
    }

    public async Task UpdateVehicleRegistrationAsync(int ownerId, Vehicle vehicle)
    {
        var existing = await _unitOfWork.Vehicles.GetByIdAsync(vehicle.Id);
        if (existing == null)
        {
            throw new BusinessRuleException("Không tìm thấy phương tiện cần cập nhật.");
        }

        if (existing.OwnerId != ownerId || vehicle.OwnerId != ownerId)
        {
            throw new BusinessRuleException("Bạn chỉ được cập nhật phương tiện thuộc sở hữu của mình.");
        }

        await ValidateVehicleDataAsync(vehicle, existing.Id);
        existing.LicensePlate = vehicle.LicensePlate;
        existing.VehicleType = vehicle.VehicleType;
        existing.Brand = vehicle.Brand;
        existing.RegistrationStatus = VehicleRegistrationStatus.Pending;

        _unitOfWork.Vehicles.Update(existing);
        await _unitOfWork.CompleteAsync();
    }

    public async Task ReviewVehicleAsync(int vehicleId, VehicleRegistrationStatus status)
    {
        if (status is not VehicleRegistrationStatus.Approved and not VehicleRegistrationStatus.Rejected)
        {
            throw new BusinessRuleException("Trạng thái duyệt phương tiện không hợp lệ.");
        }

        var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(vehicleId);
        if (vehicle == null)
        {
            throw new BusinessRuleException("Không tìm thấy phương tiện cần duyệt.");
        }

        if (status == VehicleRegistrationStatus.Approved)
        {
            var activeResidence = await _unitOfWork.ResidenceHistories.GetActiveByResidentIdAsync(vehicle.OwnerId);
            if (activeResidence == null)
            {
                throw new BusinessRuleException("Không thể duyệt xe vì chủ sở hữu không còn cư trú tại tòa nhà.");
            }
        }

        vehicle.RegistrationStatus = status;
        _unitOfWork.Vehicles.Update(vehicle);
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

    public async Task DeleteVehicleByOwnerAsync(int ownerId, int vehicleId)
    {
        var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(vehicleId);
        if (vehicle == null)
        {
            throw new BusinessRuleException("Không tìm thấy phương tiện cần xóa.");
        }

        if (vehicle.OwnerId != ownerId)
        {
            throw new BusinessRuleException("Bạn chỉ được xóa phương tiện thuộc sở hữu của mình.");
        }

        _unitOfWork.Vehicles.Delete(vehicle);
        await _unitOfWork.CompleteAsync();
    }

    private async Task<Vehicle> CreateVehicleCoreAsync(Vehicle vehicle, VehicleRegistrationStatus status)
    {
        await ValidateVehicleDataAsync(vehicle);
        vehicle.RegistrationStatus = status;

        await _unitOfWork.Vehicles.AddAsync(vehicle);
        await _unitOfWork.CompleteAsync();

        return vehicle;
    }

    private async Task ValidateVehicleDataAsync(Vehicle vehicle, int? currentVehicleId = null)
    {
        if (string.IsNullOrWhiteSpace(vehicle.LicensePlate))
        {
            throw new BusinessRuleException("Biển số xe không được để trống.");
        }

        vehicle.LicensePlate = vehicle.LicensePlate.Replace(" ", "").ToUpperInvariant();
        vehicle.Brand = string.IsNullOrWhiteSpace(vehicle.Brand) ? null : vehicle.Brand.Trim();

        if (!Enum.IsDefined(vehicle.VehicleType))
        {
            throw new BusinessRuleException("Loại phương tiện không hợp lệ.");
        }

        if (!InputValidator.ValidateLicensePlate(vehicle.LicensePlate))
        {
            throw new BusinessRuleException($"Biển số xe '{vehicle.LicensePlate}' không đúng định dạng (ví dụ: 29A1-12345 hoặc 29A-1234).");
        }

        if (vehicle.Brand?.Length > 50)
        {
            throw new BusinessRuleException("Nhãn hiệu phương tiện không được vượt quá 50 ký tự.");
        }

        var duplicate = await _unitOfWork.Vehicles.GetByLicensePlateAsync(vehicle.LicensePlate);
        if (duplicate != null && duplicate.Id != currentVehicleId)
        {
            throw new BusinessRuleException($"Biển số xe '{vehicle.LicensePlate}' đã tồn tại trong hệ thống.");
        }

        var owner = await _unitOfWork.Residents.GetByIdAsync(vehicle.OwnerId);
        if (owner == null)
        {
            throw new BusinessRuleException("Không tìm thấy cư dân sở hữu phương tiện.");
        }

        var activeResidence = await _unitOfWork.ResidenceHistories.GetActiveByResidentIdAsync(vehicle.OwnerId);
        if (activeResidence == null)
        {
            throw new BusinessRuleException("Chỉ cư dân đang cư trú hoạt động tại tòa nhà mới được phép đăng ký phương tiện.");
        }
    }
}
