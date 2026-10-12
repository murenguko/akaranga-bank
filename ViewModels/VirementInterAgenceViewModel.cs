using System.ComponentModel.DataAnnotations;
namespace AkarangaBank.Web.ViewModels;
public class VirementInterAgenceViewModel
{
    [Required, Display(Name = "Compte source")] public int CompteSourceId { get; set; }
    [Required, Display(Name = "Compte destination")] public int CompteDestinationId { get; set; }
    [Range(0.01, double.MaxValue)] public decimal Montant { get; set; }
    [StringLength(500)] public string? Motif { get; set; }
}
