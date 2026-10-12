using AkarangaBank.Web.Models;
using AkarangaBank.Web.ViewModels;

namespace AkarangaBank.Web.Services;

public interface IPretService
{
    decimal CalculerMensualite(decimal capital, decimal tauxAnnuel, int dureeMois);
    List<EcheancePret> GenererEcheancier(decimal capital, decimal tauxAnnuel, int dureeMois, DateTime dateDebut);
    Task<Pret> CreerDemandeAsync(PretDemandeViewModel vm, int utilisateurId);
    Task<bool> ValiderPretAsync(int pretId, decimal? tauxPersonnalise, int utilisateurId);
    Task<bool> RejeterPretAsync(int pretId, string motif, int utilisateurId);
    Task<Operation> DebloquerAsync(int pretId, int utilisateurId);
    Task<Operation> RembourserAsync(int echeanceId, int utilisateurId);
    Task<int> MettreAJourRetardsAsync();
    Task<PretListViewModel> GetPagedAsync(string? recherche, string? statut, string? typePret, int page, int taille, int? agenceId = null);
    Task<PretDetailsViewModel?> GetDetailsAsync(int id);
    Task<Pret?> GetByIdAsync(int id);
}
