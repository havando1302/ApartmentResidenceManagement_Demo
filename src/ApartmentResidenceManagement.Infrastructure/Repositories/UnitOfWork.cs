using System;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Interfaces;
using ApartmentResidenceManagement.Infrastructure.Data;

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
        var affectedRows = await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return affectedRows;
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
