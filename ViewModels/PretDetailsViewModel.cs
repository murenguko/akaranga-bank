using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.ViewModels;

public class PretDetailsViewModel
{
    public Pret Pret { get; set; } = null!;
    public IEnumerable<EcheancePret> Echeances { get; set; } = new List<EcheancePret>();
    public decimal TotalCapital => Echeances.Sum(e => e.Capital);
    public decimal TotalInteret => Echeances.Sum(e => e.Interet);
    public decimal TotalGeneral => Echeances.Sum(e => e.MontantTotal);
    public decimal TotalPaye => Echeances.Where(e => e.Statut == "Payee").Sum(e => e.MontantTotal);
}
