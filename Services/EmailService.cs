using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using EcoCityWaste.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public void SendEmail(string to, string subject, string body)
    {
        var smtpServer = _config["EmailSettings:SmtpServer"];

        // If SMTP not configured, simulate send by writing to a log file for local/dev testing
        if (string.IsNullOrWhiteSpace(smtpServer))
        {
            try
            {
                var logDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                Directory.CreateDirectory(logDir);
                var path = Path.Combine(logDir, "emails.log");
                var entry = $"[{DateTime.UtcNow:O}] To: {to}\nSubject: {subject}\nBody:\n{body}\n---\n";
                File.AppendAllText(path, entry);
            }
            catch
            {
                // Swallow errors in simulation mode
            }
            return;
        }

        var smtp = new SmtpClient
        {
            Host = smtpServer,
            Port = int.Parse(_config["EmailSettings:Port"] ?? "25"),
            EnableSsl = bool.TryParse(_config["EmailSettings:EnableSsl"], out var ssl) ? ssl : true,
            Credentials = new NetworkCredential(
                _config["EmailSettings:Username"],
                _config["EmailSettings:Password"]
            )
        };

        var message = new MailMessage
        {
            From = new MailAddress(
                _config["EmailSettings:SenderEmail"],
                _config["EmailSettings:SenderName"]
            ),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        message.To.Add(to);

        smtp.Send(message);
    }
}
