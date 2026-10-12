using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace AkarangaBank.Web.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> EnvoyerEmailAsync(string destinataire, string sujet, string contenuHtml, string? contenuTexte = null)
    {
        if (string.IsNullOrWhiteSpace(destinataire))
            return false;

        string? smtpHost = _configuration["Email:SmtpHost"];
        int smtpPort = int.TryParse(_configuration["Email:SmtpPort"], out int p) ? p : 587;
        string? username = _configuration["Email:Username"];
        string? password = _configuration["Email:Password"];
        string fromEmail = _configuration["Email:FromEmail"] ?? "noreply@akaranga.bi";
        string fromName = _configuration["Email:FromName"] ?? "Akaranga Bank";

        // Si aucun serveur SMTP n'est configuré en dev, on simule l'envoi avec log
        if (string.IsNullOrWhiteSpace(smtpHost))
        {
            _logger.LogInformation("[SIMULATION EMAIL] Vers: {Destinataire} | Sujet: {Sujet} | Contenu: {Contenu}",
                destinataire, sujet, contenuTexte ?? contenuHtml);
            return true;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress(destinataire, destinataire));
            message.Subject = sujet;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = contenuHtml,
                TextBody = contenuTexte ?? contenuHtml
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.Auto);

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                await client.AuthenticateAsync(username, password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email envoyé avec succès à {Destinataire} (Sujet: {Sujet})", destinataire, sujet);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'envoi de l'email via MailKit à {Destinataire}", destinataire);
            return false;
        }
    }
}
