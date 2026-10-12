using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.ViewModels;

public class NotificationListViewModel
{
    public IEnumerable<Notification> Notifications { get; set; } = new List<Notification>();
    public string? Statut { get; set; }
    public string? TypeNotification { get; set; }
    public int Page { get; set; } = 1;
    public int TaillePage { get; set; } = 15;
    public int Total { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)Total / TaillePage);
}
