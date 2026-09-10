using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ApartmentResidenceManagement.Infrastructure.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        
        // Sử dụng Connection String của MySQL Aiven cho chế độ Design-time
        string connectionString = "Server=mysql-3eddc8d3-apartment-residence-management.d.aivencloud.com;Port=11321;Database=defaultdb;User Id=avnadmin;Password=AVNS_9mvDwCNQ5vak2zzNeRB;SSL Mode=Required";
        var serverVersion = new MySqlServerVersion(new Version(8, 4, 8)); // Khai báo phiên bản MySQL trên Aiven là 8.4.8

        optionsBuilder.UseMySql(connectionString, serverVersion);

        return new AppDbContext(optionsBuilder.Options);
    }
}
