using AkarangaBank.Web.ViewModels;

namespace AkarangaBank.Web.Services;

public interface IReportService
{
    Task<DashboardViewModel> GetTableauDeBordAsync();
    Task<BalanceGeneraleViewModel> GetBalanceGeneraleAsync();
    Task<CompteResultatViewModel> GetCompteDeResultatAsync(DateTime? du, DateTime? au);
    Task<BilanViewModel> GetBilanAsync(DateTime? date);
    Task<RapportAmlMensuelViewModel> GetRapportAmlMensuelAsync(int mois, int annee);
}
