using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.ViewModels;

public class DocumentListViewModel
{
    public IEnumerable<Document> Documents { get; set; } = new List<Document>();
    public string? Recherche { get; set; }
    public string? Categorie { get; set; }
    public string? Statut { get; set; }
    public int? ClientId { get; set; }
    public int Page { get; set; } = 1;
    public int TaillePage { get; set; } = 10;
    public int Total { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)Total / TaillePage);
}
