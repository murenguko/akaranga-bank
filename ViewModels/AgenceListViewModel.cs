using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.ViewModels;

public class AgenceListViewModel
{
    public IEnumerable<Agence> Agences {get; set;} = new List<Agence>();
    public string? Recherche {get; set;}
    public int Page{get; set;} = 1;
    public int TaillePage {get; set;} = 10;
    public int Total {get; set;}

    public int TotalPages => (int)Math.Ceiling((double)Total/TaillePage);
}