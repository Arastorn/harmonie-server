using Harmonie.Application.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Harmonie.Infrastructure.Authentication;
public sealed class PasswordHasher : IPasswordHasher
{
    private readonly IPasswordHasher<object> _hasher = new PasswordHasher<object>();
    public string HashPassword(string password) => _hasher.HashPassword(new object(), password);
    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        var result = _hasher.VerifyHashedPassword(new object(), hashedPassword, providedPassword);
        return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}
