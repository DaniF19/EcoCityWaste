using EcoCityWaste.Models;

namespace EcoCityWaste.Helpers
{
    /// <summary>
    /// Classe auxiliar que contém métodos de extensão para a enumeração ContainerStatus.
    /// Serve para centralizar a lógica de tradução ao utilizador.
    /// </summary>
    public static class ContainerStatusExtensions
    {
        /// <summary>
        /// Converte o valor do Enum para uma string formatada em Português de Portugal.
        /// Este método permite chamar 'status.ToDisplayName()' diretamente em qualquer variável do tipo ContainerStatus.
        /// </summary>
        /// <param name="status">O estado técnico do contentor.</param>
        /// <returns>O nome legível para ser apresentado na interface (ex: "Avariado").</returns>
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