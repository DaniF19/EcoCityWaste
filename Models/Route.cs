using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoCityWaste.Models
{
    /// <summary>
    /// Entidade que representa o planeamento de uma rota de recolha de lixo.
    /// </summary>
    public class Route
    {
        /// <summary>
        /// Os possíveis estados do ciclo de vida da rota de recolha.
        /// </summary>
        public enum RouteStatus
        {
            [Display(Name = "Pendente")] Pending,
            [Display(Name = "Em Curso")] InProgress,
            [Display(Name = "Concluída")] Completed,
            [Display(Name = "Cancelada")] Cancelled
        }

        /// <summary>
        /// Chave primária da rota.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nome amigável dado à rota (ex: Rota do Centro Histórico).
        /// </summary>
        [Required(ErrorMessage = "O nome da rota é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Código único gerado pelo sistema (ex: RT-001).
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Observações adicionais para o motorista.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Estado em que a rota se encontra atualmente.
        /// </summary>
        public RouteStatus Status { get; set; } = RouteStatus.Pending;

        /// <summary>
        /// Data em que a rota foi planeada pelo administrador.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Data em que a rota foi atribuída a um funcionário.
        /// </summary>
        public DateTime? AssignedAt { get; set; }

        /// <summary>
        /// Data em que o funcionário marcou a rota como finalizada.
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Chave estrangeira do funcionário que vai conduzir o camião.
        /// </summary>
        public int? AssignedEmployeeId { get; set; }

        /// <summary>
        /// Propriedade de navegação para o funcionário encarregue da recolha.
        /// </summary>
        [ForeignKey(nameof(AssignedEmployeeId))]
        public virtual User? AssignedEmployee { get; set; }

        /// <summary>
        /// Nome do administrador que desenhou a rota.
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Distância total prevista da rota.
        /// </summary>
        public double? EstimatedDistanceKm { get; set; }

        /// <summary>
        /// Lista de contentores que pertencem a esta rota.
        /// </summary>
        public virtual ICollection<RouteContainer> RouteContainers { get; set; } = new List<RouteContainer>();
    }

    /// <summary>
    /// Tabela intermédia que faz a ligação entre a rota e os contentores que vão ser recolhidos.
    /// </summary>
    public class RouteContainer
    {
        /// <summary>
        /// Chave primária da tabela intermédia.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Chave estrangeira para a Rota.
        /// </summary>
        public int RouteId { get; set; }

        /// <summary>
        /// Propriedade de navegação para a Rota.
        /// </summary>
        [ForeignKey(nameof(RouteId))]
        public virtual Route Route { get; set; } = null!;

        /// <summary>
        /// Chave estrangeira para o Contentor.
        /// </summary>
        public int ContainerId { get; set; }

        /// <summary>
        /// Propriedade de navegação para o Contentor.
        /// </summary>
        [ForeignKey(nameof(ContainerId))]
        public virtual Container Container { get; set; } = null!;

        /// <summary>
        /// Define a ordem em que o camião deve parar neste contentor (1º, 2º, 3º, etc.).
        /// </summary>
        public int PickupOrder { get; set; }
    }
}