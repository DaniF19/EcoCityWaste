using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EcoCityWaste.ViewModels
{
    /// <summary>
    /// ViewModel utilizado para capturar os dados de um novo reporte de anomalia submetido por um cidadão.
    /// Inclui validações de formulário para garantir que a equipa técnica recebe informações precisas e detalhadas.
    /// </summary>
    public class ReportOccurrenceViewModel
    {
        /// <summary>
        /// Código identificador do contentor onde foi detetada a anomalia (ex: RT-001).
        /// Este campo é obrigatório para que o sistema possa localizar o equipamento no mapa.
        /// </summary>
        [Required(ErrorMessage = "Por favor, selecione o contentor.")]
        public string ContainerCode { get; set; } = string.Empty;

        /// <summary>
        /// Categoria da ocorrência.
        /// </summary>
        [Required(ErrorMessage = "Selecione o tipo de anomalia.")]
        public string OccurrenceType { get; set; } = string.Empty;

        /// <summary>
        /// Explicação detalhada do problema detetado pelo cidadão.
        /// Exige um mínimo de 10 caracteres para evitar reportes vagos ou sem contexto.
        /// </summary>
        [Required(ErrorMessage = "Por favor, descreva o problema.")]
        [MinLength(10, ErrorMessage = "A descrição deve ter pelo menos 10 caracteres.")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Ficheiro de imagem enviado pelo utilizador como prova visual da ocorrência.
        /// Este campo é opcional, permitindo submeter o reporte mesmo sem fotografia.
        /// </summary>
        public IFormFile? Photo { get; set; }
    }
}