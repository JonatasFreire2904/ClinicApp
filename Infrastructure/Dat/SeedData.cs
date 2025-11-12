using Core.Entities;
using Core.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Dat
{
    public static class SeedData
    {
        public static void Seed(AppDbContext db)
        {
            if (!db.Users.Any())
            {
                var master = new User
                {
                    UserName = "admin",
                    Email = "admin@local",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    Role = UserRole.Master
                };

                db.Users.Add(master);
                db.SaveChanges();
            }

            if (!db.Clinics.Any())
            {
                var clinics = new[]
                {
                    new Clinic { Name = "Clínica Centro" },
                    new Clinic { Name = "Clínica Norte" },
                    new Clinic { Name = "Clínica Sul" }
                };

                db.Clinics.AddRange(clinics);
                db.SaveChanges();
            }
        }
    }



}
