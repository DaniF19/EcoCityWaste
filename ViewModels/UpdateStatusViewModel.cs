using System.ComponentModel.DataAnnotations;

namespace EcoCityWaste.ViewModels
{
    /// <summary>
    /// ViewModel utilizado para a atualização do estado de progresso de uma ocorrência.
    /// Facilita a transição de estados por parte de administradores ou funcionários.
    /// </summary>
    public class UpdateStatusViewModel
    {
        /// <summary>
        /// Identificador único da ocorrência que será atualizada.
        /// </summary>
        public int OccurrenceId { get; set; }

        /// <summary>
        /// Estado atual da ocorrência no momento em que o formulário é carregado.
        /// Serve como referência visual para o utilizador antes da alteração.
        /// </summary>
        public Models.OccurrenceStatus CurrentStatus { get; set; }

        /// <summary>
        /// Registo da última data/hora em que a ocorrência sofreu uma modificação.
        /// Importante para auditoria e histórico de intervenção.
        /// </summary>
        public DateTime? LastUpdatedAt { get; set; }

        /// <summary>
        /// O novo estado selecionado para a ocorrência.
        /// Validado de acordo com os estados permitidos no domínio do sistema (Models.OccurrenceStatus).
        /// </summary>
        [Required(ErrorMessage = "É necessário selecionar um novo estado.")]
        public Models.OccurrenceStatus NewStatus { get; set; }
    }
}