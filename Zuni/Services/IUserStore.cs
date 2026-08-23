using Zuni.Models;

namespace Zuni.Services;

public interface IUserStore
{
    Task<AppUser?> FindByEmailAsync(string email);
    Task<bool> CreateAsync(AppUser user, string password);
    Task<bool> VerifyPasswordAsync(AppUser user, string password);
    Task<string?> CreateResetTokenAsync(string email);
    Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
}
