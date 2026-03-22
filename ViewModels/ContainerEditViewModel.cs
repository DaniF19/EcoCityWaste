using EcoCityWaste.Models;

namespace EcoCityWaste.ViewModels
{
    public class ContainerEditViewModel
    {
        public int Id { get; set; }
        public string Location { get; set; }
        public string Type { get; set; }
        public Container.ContainerStatus Status { get; set; }
    }
}