using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ApartmentResidenceManagement.Infrastructure.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string LocalDevelopmentConnection =
        "Server=mysql-24ea5697-hado5201314-2be8.b.aivencloud.com;Port=28269;Database=defaultdb;User Id=avnadmin;Password=AVNS_4I9qJCIaO5NGQCyOsMz;SslMode=Required";

    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        // Không lưu thông tin truy cập thật trong mã nguồn. Khi chạy dotnet ef, có thể
        // ghi đè bằng biến môi trường ConnectionStrings__DefaultConnection.
        string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? LocalDevelopmentConnection;
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 0));

        optionsBuilder.UseMySql(connectionString, serverVersion);

        return new AppDbContext(optionsBuilder.Options);
    }
}
