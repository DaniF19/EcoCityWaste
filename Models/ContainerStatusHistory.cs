namespace EcoCityWaste.Models
{
    public class ContainerStatusHistory
    {

        public int Id { get; set; }

        // relação com o contentor
        public int ContainerId { get; set; }
        public Container Container { get; set; }

        // estado do contentor no momento da alteração
        public Container.ContainerStatus Status { get; set; }

        public int FillLevel { get; set; }

        public bool IsActive { get; set; }

        // data da alteração
        public DateTime ChangedAt { get; set; }

        // utilizador que fez a alteração
        public string ChangedBy { get; set; }

    }
}
