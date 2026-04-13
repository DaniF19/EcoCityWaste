namespace EcoCityWaste.Models
{
    /// <summary>
    /// Tabela de histórico que guarda o registo de todas as alterações sofridas por um contentor.
    /// </summary>
    public class ContainerStatusHistory
    {
        /// <summary>
        /// Chave primária do registo de histórico.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Chave estrangeira que liga este registo ao contentor correspondente.
        /// </summary>
        public int ContainerId { get; set; }

        /// <summary>
        /// Propriedade de navegação para o contentor.
        /// </summary>
        public Container Container { get; set; }

        /// <summary>
        /// Estado físico do contentor.
        /// </summary>
        public Container.ContainerStatus Status { get; set; }

        /// <summary>
        /// Percentagem de enchimento registada.
        /// </summary>
        public int FillLevel { get; set; }

        /// <summary>
        /// Indica se o contentor está ativo na via pública.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Data e hora exatas em que a alteração aconteceu.
        /// </summary>
        public DateTime ChangedAt { get; set; }

        /// <summary>
        /// Identifica quem fez a alteração.
        /// </summary>
        public string ChangedBy { get; set; }
    }
}