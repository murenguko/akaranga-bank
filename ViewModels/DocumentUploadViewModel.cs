using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace AkarangaBank.Web.ViewModels;

public class DocumentUploadViewModel
{
    [Required(ErrorMessage = "Le fichier est obligatoire.")]
    [Display(Name = "Fichier à téléverser (PDF, JPG, PNG, DOCX, XLSX — max 10 Mo)")]
    public IFormFile Fichier { get; set; } = null!;

    [Required(ErrorMessage = "Le titre est obligatoire.")]
    [StringLength(200)]
    [Display(Name = "Titre du document")]
    public string Titre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La catégorie est obligatoire.")]
    [Display(Name = "Catégorie")]
    public string Categorie { get; set; } = "KYC"; // KYC, Contrat, Comptable, AML, RH, Audit

    [Display(Name = "Client rattaché")]
    public int? ClientId { get; set; }

    [Display(Name = "Compte rattaché")]
    public int? CompteId { get; set; }

    [Display(Name = "Dossier de prêt rattaché")]
    public int? PretId { get; set; }
}
