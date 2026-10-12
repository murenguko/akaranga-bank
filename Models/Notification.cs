namespace AkarangaBank.Web.Models;

public class Notification
{
    public int Id { get; set; }
    public string? TemplateCode { get; set; }
    public string Destinataire { get; set; } = string.Empty;
    public int? ClientId { get; set; }
    public Client? Client { get; set; }
    public int? UtilisateurId { get; set; }
    public ApplicationUser? Utilisateur { get; set; }
    public string TypeNotification { get; set; } = "Email"; // Email, Sms, Push, Interne
    public string Sujet { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Statut { get; set; } = "EnAttente"; // EnAttente, Envoye, Echec, Lu
    public int NombreTentatives { get; set; } = 0;
    public string? DerniereErreur { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime? DateEnvoi { get; set; }
    public DateTime? DateLecture { get; set; }
}
