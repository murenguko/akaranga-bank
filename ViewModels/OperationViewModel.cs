using System.ComponentModel.DataAnnotations;

namespace AkarangaBank.Web.ViewModels;

public class DepotViewModel
{
    [Required, Display(Name = "Compte")]
    public int CompteId { get; set; }
    [Range(0.01, double.MaxValue, ErrorMessage = "Le montant doit être positif.")]
    public decimal Montant { get; set; }
    [StringLength(255)] public string? Commentaire { get; set; }
}

public class RetraitViewModel
{
    [Required, Display(Name = "Compte")]
    public int CompteId { get; set; }
    [Range(0.01, double.MaxValue, ErrorMessage = "Le montant doit être positif.")]
    public decimal Montant { get; set; }
    [StringLength(255)] public string? Commentaire { get; set; }
}

public class VirementViewModel
{
    [Required, Display(Name = "Compte source")]
    public int CompteSourceId { get; set; }
    [Required, Display(Name = "Compte destination")]
    public int CompteDestinationId { get; set; }
    [Display(Name = "Numéro du compte destinataire")]
    public string? NumeroCompteDestination { get; set; }
    [Range(0.01, double.MaxValue, ErrorMessage = "Le montant doit être positif.")]
    public decimal Montant { get; set; }
    [StringLength(255)] public string? Motif { get; set; }
}
