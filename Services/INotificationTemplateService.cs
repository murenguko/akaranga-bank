using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.Services;

public interface INotificationTemplateService
{
    Task<(string Sujet, string ContenuHtml, string ContenuTexte)> RendreAsync(string code, string langue, string type, IDictionary<string, string> variables);
    Task<NotificationTemplate?> GetTemplateAsync(string code, string type, string langue);
    Task<IEnumerable<NotificationTemplate>> GetAllAsync();
    Task InitialiserTemplatesParDefautAsync();
}
