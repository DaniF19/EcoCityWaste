using System.ComponentModel.DataAnnotations;

namespace EcoCityWaste.ViewModels
{
    public class ContainerRegisterViewModel
    {
        [Required(ErrorMessage = "A localização é obrigatória.")]
        public string Location { get; set; }

        [Required(ErrorMessage = "O tipo é obrigatório.")]
        public string Type { get; set; }

        [Required(ErrorMessage = "O estado inicial é obrigatório.")]
        public string Status { get; set; }
    }
    
}
