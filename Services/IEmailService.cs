namespace EcoCityWaste.Services
{
    /// <summary>
    /// Interface que define a assinatura do método para os serviços de envio de mensagens eletrónicas.
    /// Permite a separação entre a lógica de negócio e o fornecedor específico de e-mail.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Define a assinatura do método responsável por enviar e-mails de forma síncrona.
        /// </summary>
        /// <param name="to">Endereço de e-mail do destinatário final.</param>
        /// <param name="subject">O assunto que aparecerá no cabeçalho da mensagem.</param>
        /// <param name="body">O conteúdo principal da mensagem, podendo incluir formatação HTML.</param>
        void SendEmail(string to, string subject, string body);
    }
}