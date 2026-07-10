namespace Tekram.Api.src.auth.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Tekram.Api.src.auth.Application.Interfaces;
using Tekram.Api.src.auth.Domain;
using Tekram.Api.src.shared;

public class UserRepository(TekramDbContext db) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return await db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);
    }

    public async Task<User?> GetByPhoneAsync(string phone, CancellationToken ct = default)
    {
        return await db.Users.FirstOrDefaultAsync(u => u.Phone == phone, ct);
    }

    public async Task<User?> GetByIdentifierAsync(string identifier, CancellationToken ct = default)
    {
        var normalized = identifier.Trim().ToLowerInvariant();
        return await db.Users.FirstOrDefaultAsync(
            u => u.Email == normalized || u.Phone == identifier.Trim(), ct);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
    {
        return await db.Users.AnyAsync(u => u.Email == email.ToLowerInvariant(), ct);
    }

    public async Task<bool> PhoneExistsAsync(string phone, CancellationToken ct = default)
    {
        return await db.Users.AnyAsync(u => u.Phone == phone, ct);
    }

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await db.Users.AddAsync(user, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        db.Users.Update(user);
        await db.SaveChangesAsync(ct);
    }
}
