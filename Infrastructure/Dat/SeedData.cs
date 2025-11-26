using Core.Entities;
using Core.Entities.Enums;

namespace Infrastructure.Dat
{
    public static class SeedData
    {
        public static void Seed(AppDbContext db)
        {
            // Create only master user if no users exist
            if (!db.Users.Any())
            {
                var master = new User
                {
                    UserName = "alphaadmin",
                    Email = "admin@alphadental.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Abacate@2025"),
                    Role = UserRole.Master
                };

                db.Users.Add(master);
                db.SaveChanges();
            }

            // Create the three clinics if no clinics exist
            if (!db.Clinics.Any())
            {
                var clinics = new[]
                {
                    new Clinic { Name = "Alpha Dental Somerville" },
                    new Clinic { Name = "Alpha Dental Chelsea" },
                    new Clinic { Name = "Alpha Dental Framingham" }
                };

                db.Clinics.AddRange(clinics);
                db.SaveChanges();
            }
        }
    }
}
