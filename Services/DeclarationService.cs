using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;
namespace AkarangaBank.Web.Services;
public class DeclarationService : IDeclarationService
{
    private readonly AkarangaBankContext _context;
    public DeclarationService(AkarangaBankContext context) => _context = context;
    public async Task<AlerteAML> CreerAlerteAsync(int clientId, int? operationId, string type, string niveau, string description)
    {
        var alerte = new AlerteAML { ClientId = clientId, OperationId = operationId, TypeAlerte = type, Niveau = niveau, Description = description, Statut = "Ouverte", DateAlerte = DateTime.UtcNow };
        _context.AlertesAML.Add(alerte); await _context.SaveChangesAsync(); return alerte;
    }
    public async Task<IEnumerable<AlerteAML>> GetAlertesAsync(string? statut, string? niveau)
    {
        var q = _context.AlertesAML.AsNoTracking().Include(a => a.Client).Include(a => a.Operation).AsQueryable();
        if (!string.IsNullOrWhiteSpace(statut)) q = q.Where(a => a.Statut == statut);
        if (!string.IsNullOrWhiteSpace(niveau)) q = q.Where(a => a.Niveau == niveau);
        return await q.OrderByDescending(a => a.DateAlerte).ToListAsync();
    }
    public Task<AlerteAML?> GetAlerteAsync(int id) => _context.AlertesAML.AsNoTracking().Include(a => a.Client).Include(a => a.Operation).ThenInclude(o => o!.Compte).FirstOrDefaultAsync(a => a.Id == id);
    public async Task<bool> TraiterAlerteAsync(int id, int utilisateurId, string statut, string? commentaire)
    {
        if (statut is not ("EnCours" or "Traitee" or "Classee")) throw new InvalidOperationException("Statut d'alerte invalide.");
        var a = await _context.AlertesAML.FirstOrDefaultAsync(x => x.Id == id); if (a is null) return false;
        a.Statut = statut; a.TraiteParId = utilisateurId; a.DateTraitement = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(commentaire)) a.Description = string.IsNullOrWhiteSpace(a.Description) ? commentaire.Trim() : a.Description + Environment.NewLine + commentaire.Trim();
        await _context.SaveChangesAsync(); return true;
    }
}
