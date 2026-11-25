using ClaimFlow.Api.Modules.Auth.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClaimFlow.Api.Data;

public record DemoUser(string Email, string DisplayName, string Password, UserRole Role);

// Called only in Development. Safe to run repeatedly: existing emails are left unchanged.
public static class DemoUserSeeder
{
    public static async Task SeedAsync(
        ClaimFlowDbContext db, IPasswordHasher<User> hasher, IEnumerable<DemoUser> demoUsers)
    {
        foreach (var demo in demoUsers)
        {
            var email = demo.Email.ToLowerInvariant();
            if (await db.Users.AnyAsync(u => u.Email == email))
            {
                continue;
            }

            // The hasher does not use its user argument.
            db.Users.Add(new User(email, demo.DisplayName, hasher.HashPassword(null!, demo.Password), demo.Role));
        }

        await db.SaveChangesAsync();
    }
}
