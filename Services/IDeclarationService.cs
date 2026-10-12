using AkarangaBank.Web.Models;
namespace AkarangaBank.Web.Services;
public interface IDeclarationService
{
    Task<AlerteAML> CreerAlerteAsync(int clientId, int? operationId, string type, string niveau, string description);
    Task<IEnumerable<AlerteAML>> GetAlertesAsync(string? statut, string? niveau);
    Task<AlerteAML?> GetAlerteAsync(int id);
    Task<bool> TraiterAlerteAsync(int id, int utilisateurId, string statut, string? commentaire);
}
