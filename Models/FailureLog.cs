namespace EcoCityWaste.Models
{
    /// <summary>
    /// Represents a single error/failure event persisted to the database.
    /// </summary>
    public class FailureLog
    {
        public int Id { get; set; }

        public DateTime OccurredAt { get; set; } = DateTime.Now;

        public string Message { get; set; } = string.Empty;

        public string? StackTrace { get; set; }

        public string? Controller { get; set; }

        public string? Action { get; set; }

        public string? UserName { get; set; }
    }
}