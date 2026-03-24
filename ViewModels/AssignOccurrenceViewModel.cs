using EcoCityWaste.Models;

namespace EcoCityWaste.ViewModels
{
    public class AssignOccurrenceViewModel
    {
        public int SelectedOccurrenceId { get; set; }
        public int SelectedEmployeeId { get; set; }

        public List<Occurrence>? Occurrences { get; set; }
        public List<User>? Employees { get; set; }
        public Dictionary<int, int>? EmployeeOccurrenceCounts { get; set; }
    }
}

