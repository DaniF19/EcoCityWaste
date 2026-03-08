namespace EcoCityWaste.Models
{
    public class ConfirmEmailViewModel
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Email { get; set; }
    }
}
