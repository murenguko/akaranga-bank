using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.ViewModels;

public class PretListViewModel
{
    public IEnumerable<Pret> Prets { get; set; } = new List<Pret>();
    public string? Recherche { get; set; }
    public string? Statut { get; set; }
    public string? TypePret { get; set; }
    public int Page { get; set; } = 1;
    public int TaillePage { get; set; } = 10;
    public int Total { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)Total / TaillePage);
}
