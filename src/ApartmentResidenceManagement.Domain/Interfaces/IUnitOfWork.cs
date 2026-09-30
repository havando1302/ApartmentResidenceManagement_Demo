using System;
using System.Threading.Tasks;

namespace ApartmentResidenceManagement.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IApartmentRepository Apartments { get; }
    IResidentRepository Residents { get; }
    IResidenceHistoryRepository ResidenceHistories { get; }
    IVehicleRepository Vehicles { get; }
    IUserAccountRepository UserAccounts { get; }
    
    Task<int> CompleteAsync();
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation);
}
