using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Hubs;
using AkarangaBank.Web.Models;
using AkarangaBank.Web.ViewModels;

namespace AkarangaBank.Web.Services;

public class NotificationService : INotificationService
{
    private readonly AkarangaBankContext _context;
    private readonly INotificationTemplateService _templateService;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly IPushNotificationService _pushService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        AkarangaBankContext context,
        INotificationTemplateService templateService,
        IEmailService emailService,
        ISmsService smsService,
        IPushNotificationService pushService,
        IHubContext<NotificationHub> hubContext,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _templateService = templateService;
        _emailService = emailService;
        _smsService = smsService;
        _pushService = pushService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<Notification> EnvoyerNotificationAsync(
        string typeNotification,
        string destinataire,
        string sujet,
        string message,
        string? templateCode = null,
        int? clientId = null,
        int? utilisateurId = null)
    {
        var notification = new Notification
        {
            TypeNotification = typeNotification,
            Destinataire = destinataire,
            Sujet = sujet,
            Message = message,
            TemplateCode = templateCode,
            ClientId = clientId,
            UtilisateurId = utilisateurId,
            Statut = "EnAttente",
            NombreTentatives = 1,
            DateCreation = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        bool success = false;
        string? erreur = null;

        try
        {
            switch (typeNotification.ToLowerInvariant())
            {
                case "email":
                    success = await _emailService.EnvoyerEmailAsync(destinataire, sujet, $"<p>{message}</p>", message);
                    break;
                case "sms":
                    success = await _smsService.EnvoyerSmsAsync(destinataire, message);
                    break;
                case "push":
                    success = await _pushService.EnvoyerPushAsync(destinataire, sujet, message);
                    break;
                case "interne":
                default:
                    await _hubContext.Clients.All.SendAsync("RecevoirNotification", new
                    {
                        Titre = sujet,
                        Message = message,
                        Type = "info",
                        Date = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss")
                    });
                    success = true;
                    break;
            }
        }
        catch (Exception ex)
        {
            success = false;
            erreur = ex.Message;
            _logger.LogError(ex, "Échec de l'envoi de la notification #{Id}", notification.Id);
        }

        notification.Statut = success ? "Envoye" : "Echec";
        notification.DateEnvoi = success ? DateTime.UtcNow : null;
        notification.DerniereErreur = erreur;

        await _context.SaveChangesAsync();

        // Diffuser également en temps réel via SignalR pour notification utilisateur active
        try
        {
            await _hubContext.Clients.All.SendAsync("RecevoirNotification", new
            {
                Titre = sujet,
                Message = message,
                Type = typeNotification,
                Date = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss")
            });
        }
        catch
        {
            // Ignore SignalR transport issue
        }

        return notification;
    }

    public async Task<Notification> EnvoyerDepuisTemplateAsync(
        string templateCode,
        string typeNotification,
        string destinataire,
        string langue,
        IDictionary<string, string> variables,
        int? clientId = null,
        int? utilisateurId = null)
    {
        var (sujet, contenuHtml, contenuTexte) = await _templateService.RendreAsync(templateCode, langue, typeNotification, variables);
        string message = typeNotification.Equals("Email", StringComparison.OrdinalIgnoreCase) ? contenuHtml : contenuTexte;

        return await EnvoyerNotificationAsync(
            typeNotification,
            destinataire,
            sujet,
            message,
            templateCode,
            clientId,
            utilisateurId);
    }

    public async Task<int> ReessayerNotificationsEchoueesAsync()
    {
        var enEchec = await _context.Notifications
            .Where(n => n.Statut == "Echec" && n.NombreTentatives < 3)
            .ToListAsync();

        int successCount = 0;
        foreach (var notif in enEchec)
        {
            notif.NombreTentatives++;
            bool success = false;
            try
            {
                if (notif.TypeNotification.Equals("Email", StringComparison.OrdinalIgnoreCase))
                {
                    success = await _emailService.EnvoyerEmailAsync(notif.Destinataire, notif.Sujet, $"<p>{notif.Message}</p>", notif.Message);
                }
                else if (notif.TypeNotification.Equals("Sms", StringComparison.OrdinalIgnoreCase))
                {
                    success = await _smsService.EnvoyerSmsAsync(notif.Destinataire, notif.Message);
                }
                else
                {
                    success = true;
                }
            }
            catch (Exception ex)
            {
                notif.DerniereErreur = ex.Message;
            }

            if (success)
            {
                notif.Statut = "Envoye";
                notif.DateEnvoi = DateTime.UtcNow;
                notif.DerniereErreur = null;
                successCount++;
            }
        }

        await _context.SaveChangesAsync();
        return successCount;
    }

    public async Task<int> EnvoyerRappelsEcheanceAsync()
    {
        // Échéances dans 3 jours (EF-14.16)
        DateTime dans3Jours = DateTime.UtcNow.Date.AddDays(3);
        DateTime finDans3Jours = dans3Jours.AddDays(1);

        var echeances = await _context.EcheancesPret
            .Include(e => e.Pret)
            .ThenInclude(p => p.Client)
            .Include(e => e.Pret)
            .ThenInclude(p => p.CompteDebit)
            .Where(e => e.Statut == "EnAttente" && e.DateEcheance >= dans3Jours && e.DateEcheance < finDans3Jours)
            .ToListAsync();

        int rappelsEnvoyes = 0;
        foreach (var ech in echeances)
        {
            var client = ech.Pret.Client;
            if (!string.IsNullOrEmpty(client.Telephone))
            {
                var vars = new Dictionary<string, string>
                {
                    { "NomClient", $"{client.Nom} {client.Prenom}" },
                    { "Reference", ech.Pret.Reference },
                    { "Montant", ech.MontantTotal.ToString("N0") },
                    { "DateEcheance", ech.DateEcheance.ToString("dd/MM/yyyy") },
                    { "NumeroCompte", ech.Pret.CompteDebit.NumeroCompte }
                };

                await EnvoyerDepuisTemplateAsync("RAPPEL_ECHEANCE", "Sms", client.Telephone, "fr-BI", vars, client.Id);
                rappelsEnvoyes++;
            }
        }

        return rappelsEnvoyes;
    }

    public async Task<NotificationListViewModel> GetPagedAsync(string? statut, string? typeNotification, int page, int taille)
    {
        var query = _context.Notifications
            .Include(n => n.Client)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(statut))
            query = query.Where(n => n.Statut == statut);

        if (!string.IsNullOrWhiteSpace(typeNotification))
            query = query.Where(n => n.TypeNotification == typeNotification);

        var total = await query.CountAsync();
        var notifications = await query
            .OrderByDescending(n => n.DateCreation)
            .Skip((page - 1) * taille)
            .Take(taille)
            .ToListAsync();

        return new NotificationListViewModel
        {
            Notifications = notifications,
            Statut = statut,
            TypeNotification = typeNotification,
            Page = page,
            TaillePage = taille,
            Total = total
        };
    }

    public async Task<bool> MarquerCommeLueAsync(int id)
    {
        var notif = await _context.Notifications.FindAsync(id);
        if (notif is null) return false;

        notif.Statut = "Lu";
        notif.DateLecture = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
}
