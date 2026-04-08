using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EcoCityWaste.Models.ViewModels
{
    // criar/editar rota
    public class RouteCreateViewModel
    {
        [Required(ErrorMessage = "O nome da rota é obrigatório.")]
        [StringLength(100)]
        [Display(Name = "Nome da Rota")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Descrição")]
        public string? Description { get; set; }

        /// <summary>lista ordenada dos IDs dos contentores (introduzidos pelo user no formulario)</summary>
        [Required(ErrorMessage = "Selecione pelo menos um contentor.")]
        public List<int> ContainerIds { get; set; } = new();
    }

    public class RouteEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome da rota é obrigatório.")]
        [StringLength(100)]
        [Display(Name = "Nome da Rota")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Descrição")]
        public string? Description { get; set; }

        /// <summary>lista ordenada dos IDs dos contentores (dps de arrastar)
        [Required(ErrorMessage = "Selecione pelo menos um contentor.")]
        public List<int> ContainerIds { get; set; } = new();
    }

    // atribuir rota
    public class RouteAssignViewModel
    {
        public int RouteId { get; set; }

        [Required(ErrorMessage = "Selecione um funcionário.")]
        [Display(Name = "Funcionário")]
        public int? EmployeeId { get; set; }
    }

    // otimizar rota
    public class OptimisedRouteDto
    {
        public List<OptimisedStopDto> Stops { get; set; } = new();
        public double EstimatedDistanceKm { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class OptimisedStopDto
    {
        public int ContainerId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int FillLevel { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int PickupOrder { get; set; }
    }
}
