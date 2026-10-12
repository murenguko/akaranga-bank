using Microsoft.AspNetCore.SignalR;

namespace AkarangaBank.Web.Hubs;

public class NotificationHub : Hub
{
    public async Task EnvoyerNotification(string utilisateur, string titre, string message, string type)
    {
        await Clients.All.SendAsync("RecevoirNotification", new
        {
            Utilisateur = utilisateur,
            Titre = titre,
            Message = message,
            Type = type,
            Date = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss")
        });
    }
}
