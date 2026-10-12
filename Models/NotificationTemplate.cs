namespace AkarangaBank.Web.Models;

public class NotificationTemplate
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty; // VIREMENT_RECU, PRET_VALIDE, ALERTE_AML_CRITIQUE, RAPPEL_ECHEANCE, etc.
    public string TypeNotification { get; set; } = "Email"; // Email, Sms, Push, Interne
    public string Langue { get; set; } = "fr-BI"; // fr-BI, rn-BI, en-US, sw-KE
    public string Sujet { get; set; } = string.Empty;
    public string ContenuHtml { get; set; } = string.Empty;
    public string ContenuTexte { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime? DateModification { get; set; }
}
