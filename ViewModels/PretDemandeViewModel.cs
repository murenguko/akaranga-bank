using System.ComponentModel.DataAnnotations;
using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.ViewModels;

public class PretDemandeViewModel
{
    [Required(ErrorMessage = "Le client est obligatoire.")]
    [Display(Name = "Client")]
    public int ClientId { get; set; }

    [Required(ErrorMessage = "Le compte de débit est obligatoire.")]
    [Display(Name = "Compte de débit")]
    public int CompteDebitId { get; set; }

    [Required(ErrorMessage = "Le type de prêt est obligatoire.")]
    [Display(Name = "Type de prêt")]
    public string TypePret { get; set; } = "Consommation"; // Consommation, Immobilier, Equipement, Commercial

    [Required(ErrorMessage = "Le montant est obligatoire.")]
    [Range(100_000, 100_000_000, ErrorMessage = "Le montant doit être compris entre 100 000 et 100 000 000 BIF.")]
    [Display(Name = "Montant demandé (BIF)")]
    public decimal Montant { get; set; } = 1_000_000m;

    [Required(ErrorMessage = "La durée est obligatoire.")]
    [Range(3, 120, ErrorMessage = "La durée doit être comprise entre 3 et 120 mois.")]
    [Display(Name = "Durée (en mois)")]
    public int DureeMois { get; set; } = 12;

    [StringLength(500)]
    [Display(Name = "Commentaire ou objet du prêt")]
    public string? Commentaire { get; set; }

    // Display helpers for simulation
    [Display(Name = "Taux annuel indicatif (%)")]
    public decimal TauxIndicatif { get; set; } = 12m;

    [Display(Name = "Mensualité estimée (BIF)")]
    public decimal MensualiteEstimee { get; set; }
}
