using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Interfaces;
using ApartmentResidenceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ApartmentResidenceManagement.Infrastructure.Repositories;

public class ResidenceHistoryRepository : Repository<ResidenceHistory>, IResidenceHistoryRepository
{
    public ResidenceHistoryRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ResidenceHistory>> GetActiveByApartmentIdAsync(int apartmentId)
    {
        return await DbSet
            .Include(rh => rh.Resident)
            .Where(rh => rh.ApartmentId == apartmentId && rh.IsActive)
            .ToListAsync();
    }

    public async Task<ResidenceHistory?> GetActiveByResidentIdAsync(int residentId)
    {
        return await DbSet
            .Include(rh => rh.Apartment)
            .FirstOrDefaultAsync(rh => rh.ResidentId == residentId && rh.IsActive);
    }

    public async Task<IEnumerable<ResidenceHistory>> GetHistoryByResidentIdAsync(int residentId)
    {
        return await DbSet
            .Include(rh => rh.Apartment)
            .Where(rh => rh.ResidentId == residentId)
            .OrderByDescending(rh => rh.StartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<ResidenceHistory>> GetAllWithDetailsAsync()
    {
        return await DbSet
            .Include(rh => rh.Apartment)
            .Include(rh => rh.Resident)
            .OrderByDescending(rh => rh.StartDate)
            .ToListAsync();
    }
}
