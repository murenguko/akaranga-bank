namespace AkarangaBank.Web.Models;

public class Document
{
    public int Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string NomFichierOriginal { get; set; } = string.Empty;
    public string NomFichierStocke { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string Categorie { get; set; } = "KYC"; // KYC, Contrat, Comptable, AML, RH, Audit
    public long TailleOctets { get; set; }
    public string HashSha256 { get; set; } = string.Empty;
    public string CheminFichier { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public string Statut { get; set; } = "Actif"; // Actif, Archive, Supprime
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime? DateSuppression { get; set; }

    public int? ClientId { get; set; }
    public Client? Client { get; set; }

    public int? CompteId { get; set; }
    public Compte? Compte { get; set; }

    public int? PretId { get; set; }
    public Pret? Pret { get; set; }

    public int? UploadeParId { get; set; }
    public ApplicationUser? UploadePar { get; set; }

    public ICollection<DocumentAcces> Acces { get; set; } = new List<DocumentAcces>();
}
