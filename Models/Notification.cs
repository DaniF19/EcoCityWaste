using System;

namespace EcoCityWaste.Models
{
    public class Notification
    {
        public int Id { get; set; }

        public int? ContainerId { get; set; }

        public string Message { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public int? UserId { get; set; }
        public User? User { get; set; }

        public string? LinkUrl { get; set; }  // e.g. "/Containers/Details/5"
        public string? NotificationType { get; set; }  // "container" | "occurrence"
    }
}
