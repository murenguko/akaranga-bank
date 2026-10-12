namespace AkarangaBank.Web.Models;

public class CompensationAgence
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public int AgenceId { get; set; }
    public Agence Agence { get; set; } = null!;
    public int CompteCompensationId { get; set; }
    public Compte CompteCompensation { get; set; } = null!;
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal Solde { get; set; }
    public DateTime DateCompensation { get; set; } = DateTime.UtcNow;
    public string Statut { get; set; } = "EnCours";
}
