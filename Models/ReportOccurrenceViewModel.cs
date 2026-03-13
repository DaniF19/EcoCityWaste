using System.ComponentModel.DataAnnotations;

namespace EcoCityWaste.Models
{
    public class ReportOccurrenceViewModel
    {
        [Required(ErrorMessage = "Por favor, selecione o contentor.")]
        public string ContainerCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecione o tipo de anomalia.")]
        public string OccurrenceType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Por favor, descreva o problema.")]
        [MinLength(10, ErrorMessage = "A descrição deve ter pelo menos 10 caracteres.")]
        public string Description { get; set; } = string.Empty;
    }
}