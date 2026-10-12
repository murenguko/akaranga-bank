using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AkarangaBank.Web.Models;

public class Compte
{
    public int Id { get; set; }
    public string NumeroCompte { get; set; } = string.Empty;
    public int? ClientId { get; set; }
    public Client? Client { get; set; }
    public int? TypeCompteId { get; set; }
    public TypeCompte? TypeCompte { get; set; }
    public int AgenceId { get; set; }
    [ValidateNever]
    public Agence Agence { get; set; } = null!;
    public decimal Solde { get; set; }
    public string Devise { get; set; } = "BIF";
    public string Statut { get; set; } = "Actif";
    public bool EstCompteInterne { get; set; }
    public DateTime DateOuverture { get; set; } = DateTime.UtcNow;
    public DateTime? DateFermeture { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime? DateModification { get; set; }
    // Navigation
    public ICollection<Ecriture> Ecritures { get; set; } = new List<Ecriture>();
    public ICollection<Carte> Cartes { get; set; } = new List<Carte>();
}
