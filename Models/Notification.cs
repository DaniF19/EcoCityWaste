using System;

namespace EcoCityWaste.Models
{
    public class Notification
    {
        public int Id { get; set; }

        public int? ContainerId { get; set; }
        
        //novo
        public int? RouteId { get; set; }

        public string Message { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        //
        public int UserId { get; set; }
        public User User { get; set; }
    }
}
