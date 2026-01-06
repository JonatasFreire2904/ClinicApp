using Core.Entities;
using Core.Entities.Enums;

namespace Infrastructure.Dat
{
    public static class SeedData
    {
        public static void Seed(AppDbContext db)
        {
            // 🔹 Usuário master
            if (!db.Users.Any())
            {
                var master = new User
                {
                    Id = Guid.NewGuid(), // ✅ explícito
                    UserName = "alphaadmin",
                    Email = "admin@alphadental.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Abacate@2025"),
                    Role = UserRole.Master,
                    CreatedAt = DateTime.UtcNow
                };

                db.Users.Add(master);
                db.SaveChanges();
            }

            // 🔹 Clínicas
            if (!db.Clinics.Any())
            {
                var clinics = new[]
                {
                    new Clinic
                    {
                        Id = Guid.NewGuid(), // ✅ explícito
                        Name = "Alpha Dental Somerville"
                    },
                    new Clinic
                    {
                        Id = Guid.NewGuid(),
                        Name = "Alpha Dental Chelsea"
                    },
                    new Clinic
                    {
                        Id = Guid.NewGuid(),
                        Name = "Alpha Dental Framingham"
                    }
                };

                db.Clinics.AddRange(clinics);
                db.SaveChanges();
            }
        }
    }
}
