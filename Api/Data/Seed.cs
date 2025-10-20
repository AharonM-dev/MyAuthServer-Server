using Api.Models;

namespace Api.Data;

public static class Seed
{
    public static async Task RunAsync(AppDbContext db, Func<string,(byte[],byte[])> hashFactory)
    {
        if (db.Users.Any()) return;

        (byte[] h, byte[] s) = hashFactory("Admin123!");
        db.Users.Add(new User {
            UserName = "admin",
            Email = "admin@example.com",
            PasswordHash = h, PasswordSalt = s
        });

        await db.SaveChangesAsync();
    }
}
