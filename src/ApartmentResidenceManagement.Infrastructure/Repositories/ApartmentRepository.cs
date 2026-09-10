using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Interfaces;
using ApartmentResidenceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ApartmentResidenceManagement.Infrastructure.Repositories;

public class ApartmentRepository : Repository<Apartment>, IApartmentRepository
{
    public ApartmentRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Apartment?> GetByApartmentNumberAsync(string apartmentNumber)
    {
        return await DbSet
            .FirstOrDefaultAsync(a => a.ApartmentNumber == apartmentNumber);
    }
}
