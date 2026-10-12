using System.ComponentModel.DataAnnotations;

namespace AkarangaBank.Web.ViewModels;

public class PretValidationViewModel
{
    [Required]
    public int PretId { get; set; }

    [Display(Name = "Taux personnalisé (%)")]
    [Range(0, 50, ErrorMessage = "Le taux d'intérêt doit être compris entre 0 et 50 %.")]
    public decimal? TauxPersonnalise { get; set; }

    [Display(Name = "Action")]
    public bool Approuve { get; set; } = true;

    [Display(Name = "Motif de rejet (obligatoire en cas de refus)")]
    [StringLength(500)]
    public string? MotifRejet { get; set; }
}
