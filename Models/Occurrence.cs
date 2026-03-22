using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoCityWaste.Models
{
    public class Occurrence
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ContainerCode { get; set; }

        [Required]
        public string OccurrenceType { get; set; }

        [Required]
        public string Description { get; set; }

        // Data em que o cidadão fez o reporte
        public DateTime ReportDate { get; set; }

        // Estado do reporte
        public string Status { get; set; }
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }
        public int? AssignedEmployeeId { get; set; }
        [ForeignKey(nameof(AssignedEmployeeId))]
        public User? AssignedEmployee { get; set; }
        public DateTime? AssignedAt { get; set; }

        public string ImagePath { get; set; }
    }

    public enum OccurrenceStatus
    {
        Pendente, EmAnalise, EmResolucao, Resolvido, Rejeitado
    }
}   