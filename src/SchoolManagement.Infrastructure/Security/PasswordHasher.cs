using SchoolManagement.Application.Interfaces;

namespace SchoolManagement.Infrastructure.Security;

/// <summary>
/// BCrypt already generates and embeds its own salt inside the hash string, so the separate
/// "salt" column is kept only for compatibility with the User entity shape and stores the same
/// value as the hash for now — verification never depends on it. This keeps the door open to a
/// different algorithm later without a schema change.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    public (string hash, string salt) Hash(string plainTextPassword)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(plainTextPassword, workFactor: 12);
        return (hash, salt: string.Empty);
    }

    public bool Verify(string plainTextPassword, string hash, string salt)
    {
        return BCrypt.Net.BCrypt.Verify(plainTextPassword, hash);
    }
}
