using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoCityWaste.Models
{
    public class Route
    {
        public enum RouteStatus
        {
            [Display(Name = "Pendente")] Pending,
            [Display(Name = "Em Curso")] InProgress,
            [Display(Name = "Concluída")] Completed,
            [Display(Name = "Cancelada")] Cancelled
        }

        public int Id { get; set; }

        [Required(ErrorMessage = "O nome da rota é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>codigo da rota</summary>
        public string Code { get; set; } = string.Empty;

        public string? Description { get; set; }

        public RouteStatus Status { get; set; } = RouteStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? AssignedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        /// <summary>FK do funcionario que foi atribuida a rota</summary>
        public int? AssignedEmployeeId { get; set; }

        [ForeignKey(nameof(AssignedEmployeeId))]
        public virtual User? AssignedEmployee { get; set; }

        /// <summary>nome do admin que criou a rota</summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>distancia total estimada em km</summary>
        public double? EstimatedDistanceKm { get; set; }

        public virtual ICollection<RouteContainer> RouteContainers { get; set; } = new List<RouteContainer>();
    }

    /// <summary>join - que container esta em cada rota e em que posicao</summary>
    public class RouteContainer
    {
        public int Id { get; set; }
        public int RouteId { get; set; }

        [ForeignKey(nameof(RouteId))]
        public virtual Route Route { get; set; } = null!;

        public int ContainerId { get; set; }
        
        [ForeignKey(nameof(ContainerId))]
        public virtual Container Container { get; set; } = null!;

        public int PickupOrder { get; set; }
    }
}
