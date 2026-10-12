using System.ComponentModel.DataAnnotations;

namespace AkarangaBank.Web.ViewModels;

public class CarteEmissionViewModel
{
    [Required(ErrorMessage = "Le compte est obligatoire.")]
    [Display(Name = "Compte bancaire actif")]
    public int CompteId { get; set; }

    [Required(ErrorMessage = "Le type de carte est obligatoire.")]
    [Display(Name = "Type de carte")]
    public string TypeCarte { get; set; } = "Visa"; // Visa, MasterCard, Locale

    [Required]
    [Range(10_000, 10_000_000, ErrorMessage = "Le plafond de retrait doit être compris entre 10 000 et 10 000 000 BIF.")]
    [Display(Name = "Plafond journalier de retrait (BIF)")]
    public decimal PlafondRetrait { get; set; } = 500_000m;

    [Required]
    [Range(10_000, 20_000_000, ErrorMessage = "Le plafond de paiement doit être compris entre 10 000 et 20 000 000 BIF.")]
    [Display(Name = "Plafond journalier de paiement (BIF)")]
    public decimal PlafondPaiement { get; set; } = 1_000_000m;

    [Required]
    [Range(0, 10_000_000, ErrorMessage = "Le plafond en ligne doit être compris entre 0 et 10 000 000 BIF.")]
    [Display(Name = "Plafond journalier en ligne (BIF)")]
    public decimal PlafondLigne { get; set; } = 500_000m;
}
