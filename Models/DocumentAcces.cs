namespace AkarangaBank.Web.Models;

public class DocumentAcces
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public Document Document { get; set; } = null!;
    public int? UtilisateurId { get; set; }
    public ApplicationUser? Utilisateur { get; set; }
    public DateTime DateAcces { get; set; } = DateTime.UtcNow;
    public string TypeAction { get; set; } = "Telechargement"; // Telechargement, Consultation, Archivage, Suppression
    public string? AdresseIP { get; set; }
    public string? UserAgent { get; set; }
}
