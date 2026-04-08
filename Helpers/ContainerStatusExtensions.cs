using EcoCityWaste.Models;

namespace EcoCityWaste.Helpers
{
    public static class ContainerStatusExtensions
    {
        public static string ToDisplayName(this Container.ContainerStatus status)
        {
            return status switch
            {
                Container.ContainerStatus.Good => "Bom",
                Container.ContainerStatus.Full => "Cheio",
                Container.ContainerStatus.Empty => "Vazio",
                Container.ContainerStatus.Broken => "Avariado",
                Container.ContainerStatus.Maintenance => "Manutenção",
                _ => "Desconhecido"
            };
        }
    }
}