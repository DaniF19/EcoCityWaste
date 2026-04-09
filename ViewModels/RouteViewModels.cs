using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EcoCityWaste.ViewModels
{
    /// <summary>
    /// ViewModel utilizado para o formulário de criação de novas rotas.
    /// Captura a intenção inicial do administrador e a seleção manual de contentores.
    /// </summary>
    public class RouteCreateViewModel
    {
        [Required(ErrorMessage = "O nome da rota é obrigatório.")]
        [StringLength(100)]
        [Display(Name = "Nome da Rota")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Descrição")]
        public string? Description { get; set; }

        /// <summary>
        /// Lista ordenada dos identificadores dos contentores selecionados para a rota.
        /// A ordem nesta lista define a sequência inicial de recolha.
        /// </summary>
        [Required(ErrorMessage = "Selecione pelo menos um contentor.")]
        public List<int> ContainerIds { get; set; } = new();
    }

    /// <summary>
    /// ViewModel para edição de rotas existentes.
    /// Permite a reordenação de paragens e a atualização de metadados da rota.
    /// </summary>
    public class RouteEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome da rota é obrigatório.")]
        [StringLength(100)]
        [Display(Name = "Nome da Rota")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Descrição")]
        public string? Description { get; set; }

        /// <summary>
        /// Lista de IDs de contentores refletindo a nova ordem.
        /// </summary>
        [Required(ErrorMessage = "Selecione pelo menos um contentor.")]
        public List<int> ContainerIds { get; set; } = new();
    }

    /// <summary>
    /// ViewModel simplificado para o processo de atribuição de uma rota a um colaborador operacional.
    /// </summary>
    public class RouteAssignViewModel
    {
        public int RouteId { get; set; }

        [Required(ErrorMessage = "Selecione um funcionário.")]
        [Display(Name = "Funcionário")]
        public int? EmployeeId { get; set; }
    }

    /// <summary>
    /// DTO que transporta o resultado do algoritmo de otimização geoespacial.
    /// Contém a sequência ideal de paragens e as métricas de eficiência calculadas.
    /// </summary>
    public class OptimisedRouteDto
    {
        /// <summary> Lista de paragens ordenadas pelo critério de proximidade e nível de enchimento. </summary>
        public List<OptimisedStopDto> Stops { get; set; } = new();

        /// <summary> Distância total do trajeto calculada. </summary>
        public double EstimatedDistanceKm { get; set; }

        /// <summary> Mensagem de feedback sobre o sucesso ou avisos do processo de otimização. </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Representa uma paragem individual dentro de uma rota otimizada.
    /// Inclui dados geográficos necessários para a renderização em mapas.
    /// </summary>
    public class OptimisedStopDto
    {
        public int ContainerId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int FillLevel { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        /// <summary> Posição sequencial na fila de recolha (1ª, 2ª, etc.). </summary>
        public int PickupOrder { get; set; }
    }
}