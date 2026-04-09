using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using EcoCityWaste.Services;

/// <summary>
/// Serviço responsável pelo envio de comunicações eletrónicas (E-mail).
/// Utiliza o protocolo SMTP para entregar mensagens de recuperação de conta.
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    /// <summary>
    /// Injeta a configuração da aplicação para ler as credenciais do servidor de e-mail de forma segura.
    /// </summary>
    /// <param name="config">Interface de configuração do ASP.NET Core.</param>
    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Executa o envio de um e-mail de forma síncrona.
    /// Configura o cliente SMTP com suporte a SSL para garantir a segurança da transmissão.
    /// </summary>
    /// <param name="to">Endereço de e-mail do destinatário.</param>
    /// <param name="subject">Assunto ou título da mensagem.</param>
    /// <param name="body">Conteúdo da mensagem (suporta tags HTML).</param>
    public void SendEmail(string to, string subject, string body)
    {
        // Configuração do cliente SMTP baseada no ficheiro appsettings.json
        var smtp = new SmtpClient
        {
            Host = _config["EmailSettings:SmtpServer"],
            Port = int.Parse(_config["EmailSettings:Port"] ?? "587"),
            EnableSsl = true,
            Credentials = new NetworkCredential(
                _config["EmailSettings:Username"],
                _config["EmailSettings:Password"]
            )
        };

        // Construção da mensagem de correio
        var message = new MailMessage
        {
            From = new MailAddress(
                _config["EmailSettings:SenderEmail"] ?? "noreply@ecocitywaste.pt",
                _config["EmailSettings:SenderName"] ?? "EcoCity Waste"
            ),
            Subject = subject,
            Body = body,
            IsBodyHtml = true // Permite o uso de links e formatação visual no e-mail
        };

        message.To.Add(to);

        // Disparo do e-mail através do servidor configurado
        smtp.Send(message);
    }
}