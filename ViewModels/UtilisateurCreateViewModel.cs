using System.ComponentModel.DataAnnotations; 
 
namespace AkarangaBank.Web.ViewModels; 
 
public class UtilisateurCreateViewModel 
{ 
    [Required] 
    [StringLength(100)] 
    [Display(Name = "Nom complet")] 
    public string NomComplet { get; set; } = string.Empty; 
 
    [Required] 
    [EmailAddress] 
    public string Email { get; set; } = string.Empty; 
 
    [Required] 
    [StringLength(100, MinimumLength = 8)] 
    [DataType(DataType.Password)] 
    public string Password { get; set; } = string.Empty; 
 
    [Required] 
    public string Role { get; set; } = "guichetier"; 
 
    [Range(1, int.MaxValue, ErrorMessage = "Veuillez sélectionner une agence valide.")] 
    [Display(Name = "Agence")] 
    public int AgenceId { get; set; } 

    [Display(Name = "Client associé (facultatif)")]
    public int? ClientId { get; set; }

 
} 
