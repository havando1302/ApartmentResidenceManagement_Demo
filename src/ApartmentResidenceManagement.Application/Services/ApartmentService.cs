using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Domain.Exceptions;
using ApartmentResidenceManagement.Domain.Interfaces;

namespace ApartmentResidenceManagement.Application.Services;

public class ApartmentService
{
    private readonly IUnitOfWork _unitOfWork;

    public ApartmentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<Apartment>> GetAllApartmentsAsync()
    {
        return await _unitOfWork.Apartments.GetAllAsync();
    }

    public async Task<Apartment?> GetApartmentByIdAsync(int id)
    {
        return await _unitOfWork.Apartments.GetByIdAsync(id);
    }

    public async Task<Apartment?> GetApartmentByNumberAsync(string apartmentNumber)
    {
        return await _unitOfWork.Apartments.GetByApartmentNumberAsync(apartmentNumber);
    }

    public async Task<Apartment> CreateApartmentAsync(Apartment apartment)
    {
        if (string.IsNullOrWhiteSpace(apartment.ApartmentNumber))
        {
            throw new BusinessRuleException("Số căn hộ không được để trống.");
        }

        var existing = await _unitOfWork.Apartments.GetByApartmentNumberAsync(apartment.ApartmentNumber);
        if (existing != null)
        {
            throw new BusinessRuleException($"Số căn hộ '{apartment.ApartmentNumber}' đã tồn tại trong hệ thống.");
        }

        if (apartment.Floor <= 0)
        {
            throw new BusinessRuleException("Số tầng phải lớn hơn 0.");
        }

        if (apartment.Area <= 0)
        {
            throw new BusinessRuleException("Diện tích căn hộ phải lớn hơn 0.");
        }

        apartment.Status = ApartmentStatus.Empty; // Mặc định khi tạo mới là Trống

        await _unitOfWork.Apartments.AddAsync(apartment);
        await _unitOfWork.CompleteAsync();

        return apartment;
    }

    public async Task UpdateApartmentAsync(Apartment apartment)
    {
        var existing = await _unitOfWork.Apartments.GetByIdAsync(apartment.Id);
        if (existing == null)
        {
            throw new BusinessRuleException("Không tìm thấy căn hộ cần cập nhật.");
        }

        // Nếu thay đổi số căn hộ, kiểm tra xem có trùng với căn hộ khác không
        if (existing.ApartmentNumber != apartment.ApartmentNumber)
        {
            var duplicate = await _unitOfWork.Apartments.GetByApartmentNumberAsync(apartment.ApartmentNumber);
            if (duplicate != null)
            {
                throw new BusinessRuleException($"Số căn hộ '{apartment.ApartmentNumber}' đã được sử dụng bởi căn hộ khác.");
            }
        }

        if (apartment.Floor <= 0)
        {
            throw new BusinessRuleException("Số tầng phải lớn hơn 0.");
        }

        if (apartment.Area <= 0)
        {
            throw new BusinessRuleException("Diện tích căn hộ phải lớn hơn 0.");
        }

        // Không được phép đổi trạng thái về Empty nếu có cư dân hoạt động
        if (apartment.Status == ApartmentStatus.Empty || apartment.Status == ApartmentStatus.UnderMaintenance)
        {
            var activeResidences = await _unitOfWork.ResidenceHistories.GetActiveByApartmentIdAsync(apartment.Id);
            if (activeResidences.Any())
            {
                throw new BusinessRuleException($"Không thể chuyển trạng thái căn hộ sang '{apartment.Status}' khi vẫn còn cư dân đang hoạt động.");
            }
        }

        existing.ApartmentNumber = apartment.ApartmentNumber;
        existing.Floor = apartment.Floor;
        existing.Area = apartment.Area;
        existing.Status = apartment.Status;

        _unitOfWork.Apartments.Update(existing);
        await _unitOfWork.CompleteAsync();
    }

    public async Task DeleteApartmentAsync(int id)
    {
        var apartment = await _unitOfWork.Apartments.GetByIdAsync(id);
        if (apartment == null)
        {
            throw new BusinessRuleException("Không tìm thấy căn hộ cần xóa.");
        }

        // Chặn xóa nếu căn hộ đang có cư dân hoạt động
        var activeResidences = await _unitOfWork.ResidenceHistories.GetActiveByApartmentIdAsync(id);
        if (activeResidences.Any())
        {
            throw new BusinessRuleException("Không thể xóa căn hộ đang có cư dân đang hoạt động cư trú.");
        }

        // Chặn xóa nếu có lịch sử cư trú cũ liên quan (để bảo toàn dữ liệu lịch sử)
        var allResidences = await _unitOfWork.ResidenceHistories.FindAsync(rh => rh.ApartmentId == id);
        if (allResidences.Any())
        {
            throw new BusinessRuleException("Không thể xóa căn hộ này vì đã từng phát sinh lịch sử cư trú trong quá khứ.");
        }

        _unitOfWork.Apartments.Delete(apartment);
        await _unitOfWork.CompleteAsync();
    }
}
