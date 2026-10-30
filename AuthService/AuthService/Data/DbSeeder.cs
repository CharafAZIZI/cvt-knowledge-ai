using AuthService.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AuthDbContext context)
    {
        // Vérifier si un administrateur existe déjà
        var adminExists = await context.Users
            .AnyAsync(u => u.Role == "ADMIN");

        if (adminExists)
        {
            return;
        }

        // Création du premier administrateur
        var admin = new User
        {
            FirstName = "Super",
            LastName = "Admin",
            Email = "admin@cvt.com",

            PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                "Admin123!"
            ),

            Role = "ADMIN",
            IsActive = true,

            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(admin);

        await context.SaveChangesAsync();
    }
}