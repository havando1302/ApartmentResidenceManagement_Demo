using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Infrastructure.Data;
using ApartmentResidenceManagement.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ApartmentResidenceManagement.Tests.Repositories;

public class RepositoryTests
{
    [Fact]
    public void Update_WhenNavigationHasDuplicateKey_DoesNotAttachNavigationGraph()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(
                "Server=localhost;Database=repository_tests;User Id=test;Password=test",
                new MySqlServerVersion(new Version(8, 0, 0)))
            .Options;
        using var context = new AppDbContext(options);
        var trackedApartment = new Apartment { Id = 1, ApartmentNumber = "101" };
        context.Attach(trackedApartment);

        var residence = new ResidenceHistory
        {
            Id = 10,
            ApartmentId = trackedApartment.Id,
            ResidentId = 20,
            Apartment = new Apartment { Id = 1, ApartmentNumber = "101" }
        };
        var repository = new Repository<ResidenceHistory>(context);

        var exception = Record.Exception(() => repository.Update(residence));

        Assert.Null(exception);
        Assert.Equal(EntityState.Modified, context.Entry(residence).State);
        Assert.Single(context.ChangeTracker.Entries<Apartment>());
        Assert.Same(trackedApartment, context.ChangeTracker.Entries<Apartment>().Single().Entity);
    }
}
