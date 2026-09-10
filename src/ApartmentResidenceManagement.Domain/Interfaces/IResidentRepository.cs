using System.Collections.Generic;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;

namespace ApartmentResidenceManagement.Domain.Interfaces;

public interface IResidentRepository : IRepository<Resident>
{
    Task<Resident?> GetByIdentityCardAsync(string identityCard);
    Task<IEnumerable<Resident>> GetResidentsByApartmentIdAsync(int apartmentId);
}
