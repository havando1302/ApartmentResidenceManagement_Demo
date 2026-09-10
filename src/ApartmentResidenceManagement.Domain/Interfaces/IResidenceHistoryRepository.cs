using System.Collections.Generic;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;

namespace ApartmentResidenceManagement.Domain.Interfaces;

public interface IResidenceHistoryRepository : IRepository<ResidenceHistory>
{
    Task<IEnumerable<ResidenceHistory>> GetActiveByApartmentIdAsync(int apartmentId);
    Task<ResidenceHistory?> GetActiveByResidentIdAsync(int residentId);
    Task<IEnumerable<ResidenceHistory>> GetHistoryByResidentIdAsync(int residentId);
    Task<IEnumerable<ResidenceHistory>> GetAllWithDetailsAsync();
}
