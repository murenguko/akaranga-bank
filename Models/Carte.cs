namespace AkarangaBank.Web.Models;

public class Carte
{
    public int Id { get; set; }
    public string NumeroCarte { get; set; } = string.Empty;
    public string NumeroMasque { get; set; } = string.Empty;
    public int CompteId { get; set; }
    public Compte Compte { get; set; } = null!;
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public string TypeCarte { get; set; } = string.Empty; // Visa, MasterCard, Locale
    public DateTime DateExpiration { get; set; }
    public decimal PlafondRetrait { get; set; } = 500_000m;
    public decimal PlafondPaiement { get; set; } = 1_000_000m;
    public decimal PlafondLigne { get; set; } = 500_000m;
    public string Statut { get; set; } = "Active"; // Active, Bloquee, Perdue, Volee, Opposition, Expiree
    public string? MotifOpposition { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<TransactionCarte> Transactions { get; set; } = new List<TransactionCarte>();
}
