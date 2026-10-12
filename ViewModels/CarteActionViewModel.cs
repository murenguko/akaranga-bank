using System.ComponentModel.DataAnnotations;

namespace AkarangaBank.Web.ViewModels;

public class CarteActionViewModel
{
    [Required]
    public int CarteId { get; set; }

    [Required]
    [Display(Name = "Motif")]
    public string Motif { get; set; } = "Perte"; // Perte, Vol, Fraude, Autre

    [Required]
    [Display(Name = "Nouveau statut")]
    public string NouveauStatut { get; set; } = "Bloquee"; // Perdue, Volee, Opposition, Bloquee
}
