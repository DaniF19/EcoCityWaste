using System.ComponentModel.DataAnnotations;

namespace EcoCityWaste.ViewModels
{
    public class UpdateStatusViewModel
    {
        public int OccurrenceId { get; set; }
        public string CurrentStatus { get; set; }
        [Required]
        public Models.OccurrenceStatus NewStatus { get; set; }
    }

}
