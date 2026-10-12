using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.ViewModels;

public class CarteListViewModel
{
    public IEnumerable<Carte> Cartes { get; set; } = new List<Carte>();
    public string? Recherche { get; set; }
    public string? Statut { get; set; }
    public string? TypeCarte { get; set; }
    public int Page { get; set; } = 1;
    public int TaillePage { get; set; } = 10;
    public int Total { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)Total / TaillePage);
}
