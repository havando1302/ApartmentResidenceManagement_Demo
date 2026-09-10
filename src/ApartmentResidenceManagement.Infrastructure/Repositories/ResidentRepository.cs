using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Interfaces;
using ApartmentResidenceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ApartmentResidenceManagement.Infrastructure.Repositories;

public class ResidentRepository : Repository<Resident>, IResidentRepository
{
    public ResidentRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Resident?> GetByIdentityCardAsync(string identityCard)
    {
        return await DbSet
            .FirstOrDefaultAsync(r => r.IdentityCard == identityCard);
    }

    public async Task<IEnumerable<Resident>> GetResidentsByApartmentIdAsync(int apartmentId)
    {
        return await Context.ResidenceHistories
            .Where(rh => rh.ApartmentId == apartmentId && rh.IsActive)
            .Select(rh => rh.Resident)
            .ToListAsync();
    }
}
