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
        startDate = startDate.Date;

        ValidateRelationshipType(relationshipType);

        if (startDate > DateTime.Today)
        {
            throw new BusinessRuleException("Ngày bắt đầu cư trú không được ở tương lai.");
        }

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

        if (startDate < resident.DateOfBirth.Date)
        {
            throw new BusinessRuleException("Ngày bắt đầu cư trú không được nhỏ hơn ngày sinh của cư dân.");
        }

        var residenceHistory = await _unitOfWork.ResidenceHistories.GetHistoryByResidentIdAsync(residentId)
            ?? Enumerable.Empty<ResidenceHistory>();
        if (residenceHistory.Any(history =>
                history.StartDate.Date >= startDate ||
                !history.EndDate.HasValue ||
                history.EndDate.Value.Date > startDate))
        {
            throw new BusinessRuleException("Ngày bắt đầu cư trú bị chồng lấn với lịch sử cư trú đã có của cư dân.");
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
        endDate = endDate.Date;

        var residence = await _unitOfWork.ResidenceHistories.GetByIdAsync(residenceId);
        if (residence == null)
        {
            throw new BusinessRuleException("Không tìm thấy thông tin cư trú.");
        }

        if (!residence.IsActive)
        {
            throw new BusinessRuleException("Thông tin cư trú này đã được kết thúc trước đó.");
        }

        if (endDate > DateTime.Today)
        {
            throw new BusinessRuleException("Ngày kết thúc cư trú không được ở tương lai.");
        }

        if (endDate < residence.StartDate.Date)
        {
            throw new BusinessRuleException("Ngày kết thúc cư trú không được nhỏ hơn ngày bắt đầu.");
        }

        residence.IsActive = false;
        residence.EndDate = endDate;
        _unitOfWork.ResidenceHistories.Update(residence);

        // Đồng bộ trạng thái căn hộ từ dữ liệu cư trú thực tế.
        var activeResidences = await _unitOfWork.ResidenceHistories.GetActiveByApartmentIdAsync(residence.ApartmentId);
        // Truy vấn vẫn nhìn thấy bản ghi hiện tại trong database vì thay đổi chưa được lưu,
        // nên loại chính bản ghi đang kết thúc ra khỏi phép đếm.
        var countActive = activeResidences.Count(rh => rh.Id != residenceId);
        
        var apartment = await _unitOfWork.Apartments.GetByIdAsync(residence.ApartmentId);
        if (apartment != null)
        {
            apartment.Status = countActive == 0 ? ApartmentStatus.Empty : ApartmentStatus.Occupied;
            _unitOfWork.Apartments.Update(apartment);
        }

        await _unitOfWork.CompleteAsync();
    }

    // 3. CHUYỂN CĂN HỘ
    public async Task<ResidenceHistory> TransferApartmentAsync(int residentId, int newApartmentId, RelationshipType relationshipType, DateTime transferDate)
    {
        transferDate = transferDate.Date;

        ValidateRelationshipType(relationshipType);

        var activeResidence = await _unitOfWork.ResidenceHistories.GetActiveByResidentIdAsync(residentId);
        if (activeResidence == null)
        {
            throw new BusinessRuleException("Cư dân này hiện không có đăng ký cư trú hoạt động nào để thực hiện chuyển phòng.");
        }

        if (activeResidence.ApartmentId == newApartmentId)
        {
            throw new BusinessRuleException("Căn hộ mới trùng với căn hộ hiện tại của cư dân.");
        }

        if (transferDate > DateTime.Today)
        {
            throw new BusinessRuleException("Ngày chuyển căn hộ không được ở tương lai.");
        }

        if (transferDate < activeResidence.StartDate.Date)
        {
            throw new BusinessRuleException("Ngày chuyển căn hộ không được nhỏ hơn ngày bắt đầu cư trú hiện tại.");
        }

        // Kiểm tra toàn bộ điều kiện của căn hộ mới trước khi thay đổi cư trú hiện tại.
        // Điều này tránh để DbContext giữ entity ở trạng thái dở dang nếu validation thất bại.
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

        var activeOldResidences = await _unitOfWork.ResidenceHistories
            .GetActiveByApartmentIdAsync(activeResidence.ApartmentId);
        var oldApartment = await _unitOfWork.Apartments.GetByIdAsync(activeResidence.ApartmentId);
        var oldApartmentStillOccupied = activeOldResidences.Any(rh => rh.Id != activeResidence.Id);

        // Mọi validation đã thành công; cập nhật hai đầu của giao dịch trong một lần SaveChanges.
        activeResidence.IsActive = false;
        activeResidence.EndDate = transferDate;
        _unitOfWork.ResidenceHistories.Update(activeResidence);

        if (oldApartment != null)
        {
            oldApartment.Status = oldApartmentStillOccupied
                ? ApartmentStatus.Occupied
                : ApartmentStatus.Empty;
            _unitOfWork.Apartments.Update(oldApartment);
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

    private static void ValidateRelationshipType(RelationshipType relationshipType)
    {
        if (!Enum.IsDefined(relationshipType))
        {
            throw new BusinessRuleException("Quan hệ cư trú không hợp lệ.");
        }
    }
}
