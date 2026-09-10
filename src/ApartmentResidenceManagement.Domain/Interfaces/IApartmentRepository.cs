using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;

namespace ApartmentResidenceManagement.Domain.Interfaces;

public interface IApartmentRepository : IRepository<Apartment>
{
    Task<Apartment?> GetByApartmentNumberAsync(string apartmentNumber);
}
