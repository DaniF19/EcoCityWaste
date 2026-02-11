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

        // Login externo
        public string? AuthProvider { get; set; }
        public string? ProviderUserId { get; set; }
    }

}
