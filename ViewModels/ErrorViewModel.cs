namespace EcoCityWaste.ViewModels
{
    /// <summary>
    /// ViewModel utilizado para a apresentação de detalhes técnicos em caso de erro na aplicação.
    /// Ajuda no debug e no rastreio de problemas reportados pelos utilizadores.
    /// </summary>
    public class ErrorViewModel
    {
        /// <summary>
        /// Identificador único do pedido que gerou a exceção.
        /// Este ID é fundamental para consultar os logs do servidor e identificar a causa raiz do erro.
        /// </summary>
        public string? RequestId { get; set; }

        /// <summary>
        /// Propriedade calculada que determina se o Identificador do Pedido deve ser exibido na interface.
        /// Retorna verdadeiro apenas se o RequestId possuir um valor válido.
        /// </summary>
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}