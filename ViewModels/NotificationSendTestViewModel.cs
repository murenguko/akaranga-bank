using System.ComponentModel.DataAnnotations;

namespace AkarangaBank.Web.ViewModels;

public class NotificationSendTestViewModel
{
    [Required]
    [Display(Name = "Canal de diffusion")]
    public string TypeNotification { get; set; } = "Email"; // Email, Sms, Push, Interne

    [Required]
    [Display(Name = "Destinataire (Email, N° téléphone, Identifiant utilisateur)")]
    public string Destinataire { get; set; } = "client@akaranga.bi";

    [Required]
    [Display(Name = "Sujet")]
    public string Sujet { get; set; } = "Test de notification Akaranga Bank";

    [Required]
    [Display(Name = "Message")]
    public string Message { get; set; } = "Ceci est une notification de test générée par le système.";

    public int? ClientId { get; set; }
}
