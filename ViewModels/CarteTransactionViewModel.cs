using System.ComponentModel.DataAnnotations;

namespace AkarangaBank.Web.ViewModels;

public class CarteTransactionViewModel
{
    [Required]
    [Display(Name = "Carte")]
    public int CarteId { get; set; }

    [Required]
    [Display(Name = "Type de transaction")]
    public string TypeTransaction { get; set; } = "Paiement"; // Retrait, Paiement, PaiementEnLigne

    [Required]
    [Range(100, 50_000_000, ErrorMessage = "Le montant doit être supérieur à 100 BIF.")]
    [Display(Name = "Montant (BIF)")]
    public decimal Montant { get; set; }

    [StringLength(100)]
    [Display(Name = "Commerçant / DAB")]
    public string? Commercant { get; set; }
}
