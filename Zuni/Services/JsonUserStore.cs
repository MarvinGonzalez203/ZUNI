using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Zuni.Models;

namespace Zuni.Services;

public sealed class JsonUserStore : IUserStore
{
    private readonly string _filePath;
    private readonly PasswordHasher<AppUser> _hasher = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public JsonUserStore(IWebHostEnvironment environment)
    {
        var directory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "users.json");
    }

    public async Task<AppUser?> FindByEmailAsync(string email)
    {
        await _gate.WaitAsync();
        try { return (await ReadAsync()).FirstOrDefault(x => x.Email == Normalize(email)); }
        finally { _gate.Release(); }
    }

    public async Task<bool> CreateAsync(AppUser user, string password)
    {
        await _gate.WaitAsync();
        try
        {
            var users = await ReadAsync();
            user.Email = Normalize(user.Email);
            if (users.Any(x => x.Email == user.Email)) return false;
            user.PasswordHash = _hasher.HashPassword(user, password);
            users.Add(user);
            await WriteAsync(users);
            return true;
        }
        finally { _gate.Release(); }
    }

    public Task<bool> VerifyPasswordAsync(AppUser user, string password)
    {
        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return Task.FromResult(result != PasswordVerificationResult.Failed);
    }

    public async Task<string?> CreateResetTokenAsync(string email)
    {
        await _gate.WaitAsync();
        try
        {
            var users = await ReadAsync();
            var user = users.FirstOrDefault(x => x.Email == Normalize(email));
            if (user is null) return null;
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            user.ResetTokenHash = HashToken(token);
            user.ResetTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(30);
            await WriteAsync(users);
            return token;
        }
        finally { _gate.Release(); }
    }

    public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
    {
        await _gate.WaitAsync();
        try
        {
            var users = await ReadAsync();
            var user = users.FirstOrDefault(x => x.Email == Normalize(email));
            if (user?.ResetTokenHash is null || user.ResetTokenExpiresAtUtc <= DateTime.UtcNow ||
                !CryptographicOperations.FixedTimeEquals(Convert.FromHexString(user.ResetTokenHash), Convert.FromHexString(HashToken(token)))) return false;
            user.PasswordHash = _hasher.HashPassword(user, newPassword);
            user.ResetTokenHash = null;
            user.ResetTokenExpiresAtUtc = null;
            await WriteAsync(users);
            return true;
        }
        catch (FormatException) { return false; }
        finally { _gate.Release(); }
    }

    private async Task<List<AppUser>> ReadAsync()
    {
        if (!File.Exists(_filePath)) return [];
        await using var stream = File.OpenRead(_filePath);
        return await JsonSerializer.DeserializeAsync<List<AppUser>>(stream) ?? [];
    }

    private async Task WriteAsync(List<AppUser> users)
    {
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, users, JsonOptions);
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
