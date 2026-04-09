using System.ComponentModel.DataAnnotations;

namespace EcoCityWaste.ViewModels
{
    /// <summary>
    /// ViewModel responsável pelo formulário de registo de novos contentores no sistema.
    /// Define as regras de validação necessárias para garantir que nenhum contentor é criado sem os dados essenciais.
    /// </summary>
    public class ContainerRegisterViewModel
    {
        /// <summary>
        /// Localização geográfica ou morada onde o contentor será instalado.
        /// Campo obrigatório para permitir a geocodificação e visualização no mapa.
        /// </summary>
        [Required(ErrorMessage = "A localização é obrigatória.")]
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// Categoria do contentor (ex: Plástico, Papel, Vidro).
        /// Essencial para a gestão seletiva de resíduos.
        /// </summary>
        [Required(ErrorMessage = "O tipo é obrigatório.")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Estado inicial de conservação do contentor no momento do registo.
        /// </summary>
        [Required(ErrorMessage = "O estado inicial é obrigatório.")]
        public string Status { get; set; } = string.Empty;
    }
}