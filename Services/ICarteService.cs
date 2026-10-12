using AkarangaBank.Web.Models;
using AkarangaBank.Web.ViewModels;

namespace AkarangaBank.Web.Services;

public interface ICarteService
{
    Task<Carte> EmettreCarteAsync(CarteEmissionViewModel vm, int utilisateurId);
    Task<bool> BloquerCarteAsync(CarteActionViewModel vm, int utilisateurId);
    Task<Carte> RenouvelerCarteAsync(int carteId, int utilisateurId);
    Task<(bool Reussi, string Message, TransactionCarte Transaction)> AutoriserTransactionAsync(CarteTransactionViewModel vm, int utilisateurId);
    Task<CarteListViewModel> GetPagedAsync(string? recherche, string? statut, string? typeCarte, int page, int taille);
    Task<CarteDetailsViewModel?> GetDetailsAsync(int id);
    Task<Carte?> GetByIdAsync(int id);
    Task<IEnumerable<TransactionCarte>> GetHistoriqueTransactionsAsync(int carteId);
}
