using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Domain.Exceptions;
using ApartmentResidenceManagement.Domain.Interfaces;

namespace ApartmentResidenceManagement.Application.Services;

public class ResidenceService
{
    private readonly IUnitOfWork _unitOfWork;

    public ResidenceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ResidenceHistory>> GetActiveResidencesByApartmentIdAsync(int apartmentId)
    {
        return await _unitOfWork.ResidenceHistories.GetActiveByApartmentIdAsync(apartmentId);
    }

    public async Task<ResidenceHistory?> GetActiveResidenceByResidentIdAsync(int residentId)
    {
        return await _unitOfWork.ResidenceHistories.GetActiveByResidentIdAsync(residentId);
    }

    public async Task<IEnumerable<ResidenceHistory>> GetHistoryByResidentIdAsync(int residentId)
    {
        return await _unitOfWork.ResidenceHistories.GetHistoryByResidentIdAsync(residentId);
    }

    // 1. ĐĂNG KÝ CƯ TRÚ MỚI
    public async Task<ResidenceHistory> RegisterResidenceAsync(int apartmentId, int residentId, RelationshipType relationshipType, DateTime startDate)
    {
        var apartment = await _unitOfWork.Apartments.GetByIdAsync(apartmentId);
        if (apartment == null)
        {
            throw new BusinessRuleException("Không tìm thấy căn hộ.");
        }

        // Chặn đăng ký vào căn hộ đang sửa chữa
        if (apartment.Status == ApartmentStatus.UnderMaintenance)
        {
            throw new BusinessRuleException("Không thể đăng ký cư trú vào căn hộ đang sửa chữa (Under Maintenance).");
        }

        var resident = await _unitOfWork.Residents.GetByIdAsync(residentId);
        if (resident == null)
        {
            throw new BusinessRuleException("Không tìm thấy cư dân.");
        }

        // Chặn nếu cư dân đang có cư trú active khác
        var activeResidence = await _unitOfWork.ResidenceHistories.GetActiveByResidentIdAsync(residentId);
        if (activeResidence != null)
        {
            throw new BusinessRuleException($"Cư dân này hiện đang cư trú hoạt động tại căn hộ {activeResidence.Apartment?.ApartmentNumber ?? activeResidence.ApartmentId.ToString()}. Vui lòng kết thúc cư trú cũ trước.");
        }

        // Chặn nếu đăng ký Chủ hộ (Owner) mà căn hộ đã có Chủ hộ đang active
        if (relationshipType == RelationshipType.Owner)
        {
            var activeResidences = await _unitOfWork.ResidenceHistories.GetActiveByApartmentIdAsync(apartmentId);
            if (activeResidences.Any(rh => rh.RelationshipType == RelationshipType.Owner))
            {
                throw new BusinessRuleException($"Căn hộ {apartment.ApartmentNumber} đã có Chủ hộ đang hoạt động. Không thể thêm Chủ hộ mới.");
            }
        }

        if (startDate < resident.DateOfBirth)
        {
            throw new BusinessRuleException("Ngày bắt đầu cư trú không được nhỏ hơn ngày sinh của cư dân.");
        }

        var residence = new ResidenceHistory
        {
            ApartmentId = apartmentId,
            ResidentId = residentId,
            RelationshipType = relationshipType,
            StartDate = startDate,
            IsActive = true
        };

        // Cập nhật trạng thái căn hộ sang Occupied
        apartment.Status = ApartmentStatus.Occupied;
        _unitOfWork.Apartments.Update(apartment);

        await _unitOfWork.ResidenceHistories.AddAsync(residence);
        await _unitOfWork.CompleteAsync();

        return residence;
    }

    // 2. KẾT THÚC CƯ TRÚ
    public async Task TerminateResidenceAsync(int residenceId, DateTime endDate)
    {
        var residence = await _unitOfWork.ResidenceHistories.GetByIdAsync(residenceId);
        if (residence == null)
        {
            throw new BusinessRuleException("Không tìm thấy thông tin cư trú.");
        }

        if (!residence.IsActive)
        {
            throw new BusinessRuleException("Thông tin cư trú này đã được kết thúc trước đó.");
        }

        if (endDate < residence.StartDate)
        {
            throw new BusinessRuleException("Ngày kết thúc cư trú không được nhỏ hơn ngày bắt đầu.");
        }

        residence.IsActive = false;
        residence.EndDate = endDate;
        _unitOfWork.ResidenceHistories.Update(residence);

        // Kiểm tra xem căn hộ đó còn ai đang ở active không
        var activeResidences = await _unitOfWork.ResidenceHistories.GetActiveByApartmentIdAsync(residence.ApartmentId);
        // Lưu ý: Lệnh GetActiveByApartmentIdAsync lấy từ database. 
        // Vì EF Core theo dõi các thực thể đang thay đổi ở DbContext, ta lọc bỏ thực thể hiện tại vừa bị đặt IsActive = false.
        var countActive = activeResidences.Count(rh => rh.Id != residenceId);
        
        if (countActive == 0)
        {
            var apartment = await _unitOfWork.Apartments.GetByIdAsync(residence.ApartmentId);
            if (apartment != null)
            {
                apartment.Status = ApartmentStatus.Empty;
                _unitOfWork.Apartments.Update(apartment);
            }
        }

        await _unitOfWork.CompleteAsync();
    }

    // 3. CHUYỂN CĂN HỘ
    public async Task<ResidenceHistory> TransferApartmentAsync(int residentId, int newApartmentId, RelationshipType relationshipType, DateTime transferDate)
    {
        var activeResidence = await _unitOfWork.ResidenceHistories.GetActiveByResidentIdAsync(residentId);
        if (activeResidence == null)
        {
            throw new BusinessRuleException("Cư dân này hiện không có đăng ký cư trú hoạt động nào để thực hiện chuyển phòng.");
        }

        if (activeResidence.ApartmentId == newApartmentId)
        {
            throw new BusinessRuleException("Căn hộ mới trùng với căn hộ hiện tại của cư dân.");
        }

        // Bước 1: Kết thúc cư trú tại căn hộ cũ (Ngày kết thúc là ngày trước ngày chuyển 1 ngày hoặc chính là ngày chuyển)
        // Ta chọn ngày kết thúc là ngày chuyển để liền mạch
        activeResidence.IsActive = false;
        activeResidence.EndDate = transferDate;
        _unitOfWork.ResidenceHistories.Update(activeResidence);

        // Kiểm tra căn hộ cũ xem còn cư dân khác active không
        var activeOldResidences = await _unitOfWork.ResidenceHistories.GetActiveByApartmentIdAsync(activeResidence.ApartmentId);
        if (activeOldResidences.Count(rh => rh.Id != activeResidence.Id) == 0)
        {
            var oldApartment = await _unitOfWork.Apartments.GetByIdAsync(activeResidence.ApartmentId);
            if (oldApartment != null)
            {
                oldApartment.Status = ApartmentStatus.Empty;
                _unitOfWork.Apartments.Update(oldApartment);
            }
        }

        // Bước 2: Đăng ký tại căn hộ mới
        var newApartment = await _unitOfWork.Apartments.GetByIdAsync(newApartmentId);
        if (newApartment == null)
        {
            throw new BusinessRuleException("Không tìm thấy căn hộ mới.");
        }

        if (newApartment.Status == ApartmentStatus.UnderMaintenance)
        {
            throw new BusinessRuleException("Không thể chuyển cư dân vào căn hộ đang sửa chữa.");
        }

        if (relationshipType == RelationshipType.Owner)
        {
            var activeNewResidences = await _unitOfWork.ResidenceHistories.GetActiveByApartmentIdAsync(newApartmentId);
            if (activeNewResidences.Any(rh => rh.RelationshipType == RelationshipType.Owner))
            {
                throw new BusinessRuleException($"Căn hộ mới ({newApartment.ApartmentNumber}) đã có sẵn Chủ hộ. Không thể đặt làm Chủ hộ.");
            }
        }

        var newResidence = new ResidenceHistory
        {
            ApartmentId = newApartmentId,
            ResidentId = residentId,
            RelationshipType = relationshipType,
            StartDate = transferDate,
            IsActive = true
        };

        newApartment.Status = ApartmentStatus.Occupied;
        _unitOfWork.Apartments.Update(newApartment);

        await _unitOfWork.ResidenceHistories.AddAsync(newResidence);
        await _unitOfWork.CompleteAsync();

        return newResidence;
    }

    public async Task<IEnumerable<ResidenceHistory>> GetAllResidencesAsync()
    {
        return await _unitOfWork.ResidenceHistories.GetAllWithDetailsAsync();
    }
}
