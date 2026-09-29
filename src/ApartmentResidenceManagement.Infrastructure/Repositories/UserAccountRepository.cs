using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Interfaces;
using ApartmentResidenceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ApartmentResidenceManagement.Infrastructure.Repositories;

public class UserAccountRepository : Repository<UserAccount>, IUserAccountRepository
{
    public UserAccountRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<UserAccount?> GetByUsernameAsync(string username)
    {
        return await DbSet
            .AsNoTracking()
            .Include(ua => ua.Resident)
            .FirstOrDefaultAsync(ua => ua.Username == username);
    }

    public async Task<UserAccount?> GetByResidentIdAsync(int residentId)
    {
        return await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(ua => ua.ResidentId == residentId);
    }
}
