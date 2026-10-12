using AkarangaBank.Web.Models;
using AkarangaBank.Web.ViewModels;

namespace AkarangaBank.Web.Services;

public interface INotificationService
{
    Task<Notification> EnvoyerNotificationAsync(
        string typeNotification,
        string destinataire,
        string sujet,
        string message,
        string? templateCode = null,
        int? clientId = null,
        int? utilisateurId = null);

    Task<Notification> EnvoyerDepuisTemplateAsync(
        string templateCode,
        string typeNotification,
        string destinataire,
        string langue,
        IDictionary<string, string> variables,
        int? clientId = null,
        int? utilisateurId = null);

    Task<int> ReessayerNotificationsEchoueesAsync();
    Task<int> EnvoyerRappelsEcheanceAsync();
    Task<NotificationListViewModel> GetPagedAsync(string? statut, string? typeNotification, int page, int taille);
    Task<bool> MarquerCommeLueAsync(int id);
}
