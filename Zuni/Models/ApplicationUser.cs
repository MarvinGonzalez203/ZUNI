using Microsoft.AspNetCore.Identity;

namespace Zuni.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } =
            new List<PasswordResetToken>();

        public PerfilEstudiante? PerfilEstudiante { get; set; }
    }
}
