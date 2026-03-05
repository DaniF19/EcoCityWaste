namespace EcoCityWaste.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string? Token { get; set; }
        public DateTime? TokenExpiry { get; set; }

        // Email verification
        public bool EmailVerified { get; set; } = false;
        // Hashed verification code (do not store plain codes)
        public string? EmailVerificationCodeHash { get; set; }
        public DateTime? EmailVerificationExpiry { get; set; }
        public DateTime? EmailVerificationSentAt { get; set; }
        public DateTime? EmailVerificationBlockedUntil { get; set; }
        public int EmailVerificationAttempts { get; set; } = 0;

        // Login externo
        public string? AuthProvider { get; set; }
        public string? ProviderUserId { get; set; }

        // roles 
        public string Role { get; set; } = "Cidadao";
    }

}
