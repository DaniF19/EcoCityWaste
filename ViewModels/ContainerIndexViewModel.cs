using EcoCityWaste.Models;

namespace EcoCityWaste.ViewModels
{
    public class ContainerIndexViewModel
    {
        public IEnumerable<Container> Containers { get; set; }

        public string? Search { get; set; }
        public string? StatusFilter { get; set; }
        public string? SortOrder { get; set; }

        public ContainerDashboardStats Stats { get; set; }
    }

    public class ContainerDashboardStats
    {
        public int Total { get; set; }
        public int TotalCheios { get; set; }
        public int TotalAvariados { get; set; }
        public int TotalAtivos { get; set; }
    }
}
