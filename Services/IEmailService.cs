namespace AkarangaBank.Web.Services;

public interface IEmailService
{
    Task<bool> EnvoyerEmailAsync(string destinataire, string sujet, string contenuHtml, string? contenuTexte = null);
}
