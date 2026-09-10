using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Exceptions;
using ApartmentResidenceManagement.Domain.Interfaces;
using ApartmentResidenceManagement.Application.Validators;

namespace ApartmentResidenceManagement.Application.Services;

public class ResidentService
{
    private readonly IUnitOfWork _unitOfWork;

    public ResidentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<Resident>> GetAllResidentsAsync()
    {
        return await _unitOfWork.Residents.GetAllAsync();
    }

    public async Task<Resident?> GetResidentByIdAsync(int id)
    {
        return await _unitOfWork.Residents.GetByIdAsync(id);
    }

    public async Task<Resident?> GetResidentByIdentityCardAsync(string identityCard)
    {
        return await _unitOfWork.Residents.GetByIdentityCardAsync(identityCard);
    }

    public async Task<Resident> CreateResidentAsync(Resident resident)
    {
        if (string.IsNullOrWhiteSpace(resident.FullName))
        {
            throw new BusinessRuleException("Họ và tên cư dân không được để trống.");
        }

        if (!InputValidator.ValidateDateOfBirth(resident.DateOfBirth))
        {
            throw new BusinessRuleException("Ngày sinh không hợp lệ (không được ở tương lai hoặc quá 120 tuổi).");
        }

        // Validate số điện thoại nếu có
        if (!string.IsNullOrWhiteSpace(resident.PhoneNumber) && !InputValidator.ValidatePhone(resident.PhoneNumber))
        {
            throw new BusinessRuleException("Số điện thoại không hợp lệ.");
        }

        // Validate CCCD nếu có
        if (!string.IsNullOrWhiteSpace(resident.IdentityCard))
        {
            if (!InputValidator.ValidateIdentityCard(resident.IdentityCard))
            {
                throw new BusinessRuleException("Số CCCD/CMND không hợp lệ (bắt buộc phải là 9 hoặc 12 số).");
            }

            var existing = await _unitOfWork.Residents.GetByIdentityCardAsync(resident.IdentityCard);
            if (existing != null)
            {
                throw new BusinessRuleException($"Cư dân có số CCCD/CMND '{resident.IdentityCard}' đã tồn tại trong hệ thống.");
            }
        }

        await _unitOfWork.Residents.AddAsync(resident);
        await _unitOfWork.CompleteAsync();

        return resident;
    }

    public async Task UpdateResidentAsync(Resident resident)
    {
        var existing = await _unitOfWork.Residents.GetByIdAsync(resident.Id);
        if (existing == null)
        {
            throw new BusinessRuleException("Không tìm thấy cư dân cần cập nhật.");
        }

        if (string.IsNullOrWhiteSpace(resident.FullName))
        {
            throw new BusinessRuleException("Họ và tên cư dân không được để trống.");
        }

        if (!InputValidator.ValidateDateOfBirth(resident.DateOfBirth))
        {
            throw new BusinessRuleException("Ngày sinh không hợp lệ.");
        }

        if (!string.IsNullOrWhiteSpace(resident.PhoneNumber) && !InputValidator.ValidatePhone(resident.PhoneNumber))
        {
            throw new BusinessRuleException("Số điện thoại không hợp lệ.");
        }

        // Validate và kiểm tra trùng CCCD
        if (!string.IsNullOrWhiteSpace(resident.IdentityCard))
        {
            if (!InputValidator.ValidateIdentityCard(resident.IdentityCard))
            {
                throw new BusinessRuleException("Số CCCD/CMND không hợp lệ.");
            }

            if (existing.IdentityCard != resident.IdentityCard)
            {
                var duplicate = await _unitOfWork.Residents.GetByIdentityCardAsync(resident.IdentityCard);
                if (duplicate != null)
                {
                    throw new BusinessRuleException($"Số CCCD/CMND '{resident.IdentityCard}' đã được đăng ký bởi cư dân khác.");
                }
            }
        }

        existing.FullName = resident.FullName;
        existing.DateOfBirth = resident.DateOfBirth;
        existing.Gender = resident.Gender;
        existing.IdentityCard = resident.IdentityCard;
        existing.PhoneNumber = resident.PhoneNumber;
        existing.HomeTown = resident.HomeTown;

        _unitOfWork.Residents.Update(existing);
        await _unitOfWork.CompleteAsync();
    }

    public async Task DeleteResidentAsync(int id)
    {
        var resident = await _unitOfWork.Residents.GetByIdAsync(id);
        if (resident == null)
        {
            throw new BusinessRuleException("Không tìm thấy cư dân cần xóa.");
        }

        // Chặn xóa nếu cư dân đang có cư trú hoạt động
        var activeResidence = await _unitOfWork.ResidenceHistories.GetActiveByResidentIdAsync(id);
        if (activeResidence != null)
        {
            throw new BusinessRuleException($"Không thể xóa cư dân đang có đăng ký cư trú hoạt động tại phòng {activeResidence.Apartment.ApartmentNumber}.");
        }

        // Chặn xóa nếu có lịch sử cư trú cũ liên quan (đảm bảo bảo toàn dữ liệu lịch sử)
        var hasHistory = await _unitOfWork.ResidenceHistories.GetHistoryByResidentIdAsync(id);
        if (hasHistory.Any())
        {
            throw new BusinessRuleException("Không thể xóa cư dân này vì đã từng phát sinh lịch sử cư trú tại tòa nhà.");
        }

        _unitOfWork.Residents.Delete(resident);
        await _unitOfWork.CompleteAsync();
    }
}
