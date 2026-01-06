using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Dat
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            optionsBuilder.UseSqlServer("Server=tcp:clinic-app-db-server.database.windows.net,1433;Initial Catalog=clinic-app-db;Persist Security Info=False;User ID=clinic-app-db-user;Password=3PvC2rkJx5Qj@6v;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
