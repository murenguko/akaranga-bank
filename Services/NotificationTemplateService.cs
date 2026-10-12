using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.Services;

public class NotificationTemplateService : INotificationTemplateService
{
    private readonly AkarangaBankContext _context;

    public NotificationTemplateService(AkarangaBankContext context)
    {
        _context = context;
    }

    public async Task<(string Sujet, string ContenuHtml, string ContenuTexte)> RendreAsync(string code, string langue, string type, IDictionary<string, string> variables)
    {
        var template = await GetTemplateAsync(code, type, langue)
            ?? await GetTemplateAsync(code, type, "fr-BI"); // Fallback to fr-BI

        if (template is null)
        {
            // Default generic fallback
            string defaultSubject = $"Notification Akaranga Bank ({code})";
            string defaultContent = string.Join("\n", variables.Select(v => $"{v.Key}: {v.Value}"));
            return (defaultSubject, $"<p>{defaultContent.Replace("\n", "<br/>")}</p>", defaultContent);
        }

        string renderedSujet = RemplacerVariables(template.Sujet, variables);
        string renderedHtml = RemplacerVariables(template.ContenuHtml, variables);
        string renderedText = RemplacerVariables(template.ContenuTexte, variables);

        return (renderedSujet, renderedHtml, renderedText);
    }

    private static string RemplacerVariables(string input, IDictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        string result = input;
        foreach (var (key, value) in variables)
        {
            result = result.Replace("{{" + key + "}}", value ?? string.Empty);
            result = result.Replace("{" + key + "}", value ?? string.Empty);
        }
        return result;
    }

    public async Task<NotificationTemplate?> GetTemplateAsync(string code, string type, string langue)
    {
        return await _context.NotificationTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Code == code && t.TypeNotification == type && t.Langue == langue);
    }

    public async Task<IEnumerable<NotificationTemplate>> GetAllAsync()
    {
        return await _context.NotificationTemplates
            .AsNoTracking()
            .OrderBy(t => t.Code)
            .ThenBy(t => t.Langue)
            .ToListAsync();
    }

    public async Task InitialiserTemplatesParDefautAsync()
    {
        if (await _context.NotificationTemplates.AnyAsync()) return;

        var templates = new List<NotificationTemplate>
        {
            // 1. VIREMENT_RECU (Email - fr-BI)
            new()
            {
                Code = "VIREMENT_RECU",
                TypeNotification = "Email",
                Langue = "fr-BI",
                Sujet = "Akaranga Bank : Virement de {{Montant}} BIF reçu",
                ContenuHtml = "<h3>Bonjour {{NomClient}},</h3><p>Un virement d'un montant de <strong>{{Montant}} BIF</strong> a été crédité sur votre compte <strong>{{NumeroCompte}}</strong>.</p><p>Motif : {{Motif}}<br/>Référence : {{Reference}}</p><p>Merci pour votre confiance,<br/><strong>Akaranga Bank</strong></p>",
                ContenuTexte = "Bonjour {{NomClient}}, un virement de {{Montant}} BIF a été crédité sur votre compte {{NumeroCompte}}. Réf: {{Reference}}. Akaranga Bank.",
                Version = 1,
                DateCreation = DateTime.UtcNow
            },
            // 2. VIREMENT_RECU (Sms - fr-BI)
            new()
            {
                Code = "VIREMENT_RECU",
                TypeNotification = "Sms",
                Langue = "fr-BI",
                Sujet = "Virement reçu",
                ContenuHtml = "",
                ContenuTexte = "Akaranga Bank: Compte {{NumeroCompte}} credite de {{Montant}} BIF. Réf {{Reference}}.",
                Version = 1,
                DateCreation = DateTime.UtcNow
            },
            // 3. VIREMENT_RECU (Email - rn-BI Kirundi)
            new()
            {
                Code = "VIREMENT_RECU",
                TypeNotification = "Email",
                Langue = "rn-BI",
                Sujet = "Akaranga Bank : Amahera {{Montant}} BIF yinjiye kuri konti yawe",
                ContenuHtml = "<h3>Mwaramutse {{NomClient}},</h3><p>Konti yawe <strong>{{NumeroCompte}}</strong> yaronse amahera angana na <strong>{{Montant}} BIF</strong>.</p><p>Urutonde: {{Reference}}</p><p>Akaranga Bank irabashimiye.</p>",
                ContenuTexte = "Mwaramutse {{NomClient}}, amahera {{Montant}} BIF yinjiye kuri konti {{NumeroCompte}}. Ref {{Reference}}. Akaranga Bank.",
                Version = 1,
                DateCreation = DateTime.UtcNow
            },
            // 4. PRET_VALIDE (Email - fr-BI)
            new()
            {
                Code = "PRET_VALIDE",
                TypeNotification = "Email",
                Langue = "fr-BI",
                Sujet = "Akaranga Bank : Votre demande de prêt a été approuvée !",
                ContenuHtml = "<h3>Félicitations {{NomClient}},</h3><p>Votre demande de prêt (Réf: <strong>{{Reference}}</strong>) d'un montant de <strong>{{Montant}} BIF</strong> a été validée avec succès.</p><p>Taux : {{Taux}}% | Mensualité : {{Mensualite}} BIF | Durée : {{Duree}} mois.</p><p>Les fonds seront mis à disposition sur votre compte.</p>",
                ContenuTexte = "Félicitations {{NomClient}}, votre prêt {{Reference}} de {{Montant}} BIF a été validé. Mensualité: {{Mensualite}} BIF. Akaranga Bank.",
                Version = 1,
                DateCreation = DateTime.UtcNow
            },
            // 5. ALERTE_AML_CRITIQUE (Email - fr-BI)
            new()
            {
                Code = "ALERTE_AML_CRITIQUE",
                TypeNotification = "Email",
                Langue = "fr-BI",
                Sujet = "[ALERTE CONFORMITÉ] Alerte AML Critique détectée",
                ContenuHtml = "<h3 style='color:red;'>ALERTE DE SÉCURITÉ AML / LBC-FT</h3><p>Une alerte critique a été déclenchée :</p><ul><li>Client : {{NomClient}} ({{NumeroClient}})</li><li>Type : {{TypeAlerte}}</li><li>Description : {{Description}}</li><li>Montant : {{Montant}} BIF</li></ul><p>Veuillez traiter cette alerte sans délai sur le portail de conformité.</p>",
                ContenuTexte = "URGENT AML: Alerte critique pour le client {{NomClient}} ({{NumeroClient}}). Type: {{TypeAlerte}}. Montant: {{Montant}} BIF.",
                Version = 1,
                DateCreation = DateTime.UtcNow
            },
            // 6. RAPPEL_ECHEANCE (Sms - fr-BI)
            new()
            {
                Code = "RAPPEL_ECHEANCE",
                TypeNotification = "Sms",
                Langue = "fr-BI",
                Sujet = "Rappel échéance",
                ContenuHtml = "",
                ContenuTexte = "Akaranga Bank: Rappel echeance pret {{Reference}} de {{Montant}} BIF a regler le {{DateEcheance}} sur le compte {{NumeroCompte}}.",
                Version = 1,
                DateCreation = DateTime.UtcNow
            }
        };

        _context.NotificationTemplates.AddRange(templates);
        await _context.SaveChangesAsync();
    }
}
