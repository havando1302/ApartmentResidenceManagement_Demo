using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Interfaces;
using ApartmentResidenceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ApartmentResidenceManagement.Infrastructure.Repositories;

public class VehicleRepository : Repository<Vehicle>, IVehicleRepository
{
    public VehicleRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Vehicle?> GetByLicensePlateAsync(string licensePlate)
    {
        return await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.LicensePlate == licensePlate);
    }

    public async Task<IEnumerable<Vehicle>> GetVehiclesByOwnerIdAsync(int ownerId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(v => v.OwnerId == ownerId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Vehicle>> GetAllWithDetailsAsync()
    {
        return await DbSet
            .AsNoTracking()
            .Include(v => v.Owner)
            .ToListAsync();
    }
}
