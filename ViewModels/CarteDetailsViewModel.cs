using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.ViewModels;

public class CarteDetailsViewModel
{
    public Carte Carte { get; set; } = null!;
    public IEnumerable<TransactionCarte> Transactions { get; set; } = new List<TransactionCarte>();
    public decimal TotalDepenseJour { get; set; }
}
