using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Dat
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            optionsBuilder.UseSqlServer(
                "Server=tcp:myclinicsql.database.windows.net,1433;" +
                "Initial Catalog=myclinicapp;" +
                "User ID=psqladminun;" +
                "Password=Jojo*2020;" +
                "Encrypt=True;"
            );

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
