using System;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Exceptions;
using ApartmentResidenceManagement.Domain.Interfaces;
using ApartmentResidenceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ApartmentResidenceManagement.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IApartmentRepository? _apartments;
    private IResidentRepository? _residents;
    private IResidenceHistoryRepository? _residenceHistories;
    private IVehicleRepository? _vehicles;
    private IUserAccountRepository? _userAccounts;

    public UnitOfWork(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IApartmentRepository Apartments =>
        _apartments ??= new ApartmentRepository(_context);

    public IResidentRepository Residents =>
        _residents ??= new ResidentRepository(_context);

    public IResidenceHistoryRepository ResidenceHistories =>
        _residenceHistories ??= new ResidenceHistoryRepository(_context);

    public IVehicleRepository Vehicles =>
        _vehicles ??= new VehicleRepository(_context);

    public IUserAccountRepository UserAccounts =>
        _userAccounts ??= new UserAccountRepository(_context);

    public async Task<int> CompleteAsync()
    {
        try
        {
            var affectedRows = await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
            return affectedRows;
        }
        catch (DbUpdateException dbEx)
        {
            // Dọn sạch trạng thái entity bị lỗi để EF Core không bị kẹt ở lần lưu tiếp theo
            _context.ChangeTracker.Clear();

            var inner = dbEx.InnerException?.Message ?? string.Empty;

            // ── Lỗi vi phạm UNIQUE INDEX (Duplicate entry) ──────────────────────────
            if (inner.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase)
                || inner.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
                || inner.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
            {
                if (inner.Contains("ActiveResidentId", StringComparison.OrdinalIgnoreCase))
                    throw new BusinessRuleException(
                        "Cư dân này đang có một bản ghi cư trú hoạt động khác trong hệ thống. " +
                        "Vui lòng kết thúc bản ghi cũ trước khi thực hiện thao tác này.", dbEx);

                if (inner.Contains("ActiveOwnerApartmentId", StringComparison.OrdinalIgnoreCase))
                    throw new BusinessRuleException(
                        "Căn hộ này đã có Chủ hộ đang hoạt động. Không thể thêm Chủ hộ thứ hai.", dbEx);

                if (inner.Contains("LicensePlate", StringComparison.OrdinalIgnoreCase)
                    || inner.Contains("IX_Vehicles", StringComparison.OrdinalIgnoreCase))
                    throw new BusinessRuleException(
                        "Biển số xe này đã tồn tại trong hệ thống.", dbEx);

                if (inner.Contains("IdentityCard", StringComparison.OrdinalIgnoreCase)
                    || inner.Contains("IX_Residents", StringComparison.OrdinalIgnoreCase))
                    throw new BusinessRuleException(
                        "Số CCCD/CMND này đã được đăng ký bởi cư dân khác.", dbEx);

                if (inner.Contains("ApartmentNumber", StringComparison.OrdinalIgnoreCase)
                    || inner.Contains("IX_Apartments", StringComparison.OrdinalIgnoreCase))
                    throw new BusinessRuleException(
                        "Số căn hộ này đã tồn tại trong hệ thống.", dbEx);

                throw new BusinessRuleException(
                    "Dữ liệu bị trùng lặp. Vui lòng kiểm tra lại thông tin đã nhập.", dbEx);
            }

            // ── Lỗi vi phạm CHECK CONSTRAINT ────────────────────────────────────────
            if (inner.Contains("CHECK constraint", StringComparison.OrdinalIgnoreCase)
                || inner.Contains("check constraint", StringComparison.OrdinalIgnoreCase)
                || inner.Contains("CK_", StringComparison.OrdinalIgnoreCase))
            {
                if (inner.Contains("ActiveEndDate", StringComparison.OrdinalIgnoreCase))
                    throw new BusinessRuleException(
                        "Dữ liệu cư trú không hợp lệ: bản ghi đang hoạt động không được có ngày kết thúc.", dbEx);

                if (inner.Contains("DateRange", StringComparison.OrdinalIgnoreCase))
                    throw new BusinessRuleException(
                        "Ngày kết thúc cư trú không được nhỏ hơn ngày bắt đầu.", dbEx);

                throw new BusinessRuleException(
                    "Dữ liệu vi phạm ràng buộc nghiệp vụ. Vui lòng kiểm tra lại thông tin.", dbEx);
            }

            // ── Lỗi FK (Foreign Key constraint) ─────────────────────────────────────
            if (inner.Contains("foreign key", StringComparison.OrdinalIgnoreCase)
                || inner.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase))
                throw new BusinessRuleException(
                    "Không thể thực hiện vì dữ liệu đang được tham chiếu bởi bảng khác.", dbEx);

            // ── Lỗi DB không xác định ────────────────────────────────────────────────
            throw new BusinessRuleException(
                "Đã xảy ra lỗi khi lưu dữ liệu vào cơ sở dữ liệu. Vui lòng thử lại.", dbEx);
        }
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var executionStrategy = _context.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var result = await operation();
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                throw;
            }
        });
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
