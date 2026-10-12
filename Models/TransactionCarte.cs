namespace AkarangaBank.Web.Models;

public class TransactionCarte
{
    public int Id { get; set; }
    public int CarteId { get; set; }
    public Carte Carte { get; set; } = null!;
    public decimal Montant { get; set; }
    public string TypeTransaction { get; set; } = "Paiement"; // Retrait, Paiement, PaiementEnLigne
    public string Statut { get; set; } = "Validee"; // Validee, Refusee
    public string? MotifRefus { get; set; }
    public string? Commercant { get; set; }
    public int? OperationId { get; set; }
    public Operation? Operation { get; set; }
    public DateTime DateTransaction { get; set; } = DateTime.UtcNow;
}
