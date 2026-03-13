using System;
using System.ComponentModel.DataAnnotations;

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

        // Data exata em que o cidadão fez o reporte
        public DateTime ReportDate { get; set; }

        // Estado do reporte (ex: Pendente, Em Resolução, Concluído)
        public string Status { get; set; }
    }
}