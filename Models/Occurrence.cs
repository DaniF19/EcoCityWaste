using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoCityWaste.Models
{
    /// <summary>
    /// Entidade que representa um problema reportado por um cidadão 
    /// (ex: contentor a transbordar, vandalismo, lixo na via pública).
    /// </summary>
    public class Occurrence
    {
        /// <summary>
        /// Chave primária da ocorrência.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Código de identificação do contentor que apresenta o problema.
        /// </summary>
        [Required]
        public string ContainerCode { get; set; }

        /// <summary>
        /// Categoria da anomalia selecionada pelo cidadão no momento do reporte.
        /// </summary>
        [Required]
        public string OccurrenceType { get; set; }

        /// <summary>
        /// Descrição textual detalhada fornecida pelo utilizador sobre o problema.
        /// </summary>
        [Required]
        public string Description { get; set; }

        /// <summary>
        /// Data e hora exatas em que o cidadão submeteu o alerta no portal.
        /// </summary>
        public DateTime ReportDate { get; set; }

        /// <summary>
        /// Estado atual da ocorrência (Pendente, Resolvido, etc.). 
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Chave estrangeira que identifica qual o cidadão que submeteu a ocorrência.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Propriedade de navegação para os dados do cidadão.
        /// </summary>
        [ForeignKey("UserId")]
        public User User { get; set; }

        /// <summary>
        /// Chave estrangeira do funcionário da autarquia que foi destacado para resolver o problema.
        /// Pode ser nulo enquanto estiver pendente na fila do Administrador.
        /// </summary>
        public int? AssignedEmployeeId { get; set; }

        /// <summary>
        /// Propriedade de navegação para o perfil do funcionário encarregue do trabalho.
        /// </summary>
        [ForeignKey(nameof(AssignedEmployeeId))]
        public User? AssignedEmployee { get; set; }

        /// <summary>
        /// Data e hora em que o Administrador atribuiu esta ocorrência a um funcionário.
        /// </summary>
        public DateTime? AssignedAt { get; set; }

        /// <summary>
        /// Caminho para o ficheiro da fotografia guardada no servidor (ex: /uploads/img.png).
        /// </summary>
        public string? ImagePath { get; set; }

        /// <summary>
        /// Data e hora da última vez que o estado da ocorrência foi atualizado.
        /// </summary>
        public DateTime? LastUpdatedAt { get; set; }
    }

    /// <summary>
    /// Enumeração que mapeia o fluxo e ciclo de vida de uma ocorrência.
    /// </summary>
    public enum OccurrenceStatus
    {
        [Display(Name = "Pendente")]
        Pendente,

        [Display(Name = "Em Análise")]
        EmAnalise,

        [Display(Name = "Em Resolução")]
        EmResolucao,

        [Display(Name = "Resolvido")]
        Resolvido,

        [Display(Name = "Rejeitado")]
        Rejeitado
    }
}