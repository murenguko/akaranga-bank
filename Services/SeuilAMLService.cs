using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;
namespace AkarangaBank.Web.Services;
public class SeuilAMLService : ISeuilAMLService
{
    private readonly AkarangaBankContext _context;
    public SeuilAMLService(AkarangaBankContext context) => _context = context;
    private async Task EnsureDefaultsAsync()
    {
        if (await _context.SeuilsAML.AnyAsync()) return;
        _context.SeuilsAML.AddRange(
            new SeuilAML { Code = "VIGILANCE", Libelle = "Vigilance renforcée", Montant = 5_000_000m, TypeOperation = "Toutes" },
            new SeuilAML { Code = "DECLARATION", Libelle = "Déclaration espèces CNRF", Montant = 10_000_000m, TypeOperation = "Depot" },
            new SeuilAML { Code = "DECLARATION", Libelle = "Déclaration espèces CNRF", Montant = 10_000_000m, TypeOperation = "Retrait" },
            new SeuilAML { Code = "BLOCAGE", Libelle = "Blocage automatique", Montant = 50_000_000m, TypeOperation = "Toutes" });
        await _context.SaveChangesAsync();
    }
    public async Task<SeuilAML?> GetSeuilAsync(string code, string typeOperation)
    {
        await EnsureDefaultsAsync();
        return await _context.SeuilsAML.AsNoTracking().Where(s => s.Code == code && s.Actif && (s.TypeOperation == typeOperation || s.TypeOperation == "Toutes")).OrderBy(s => s.TypeOperation == typeOperation ? 0 : 1).FirstOrDefaultAsync();
    }
    public async Task<IEnumerable<SeuilAML>> GetAllAsync() { await EnsureDefaultsAsync(); return await _context.SeuilsAML.AsNoTracking().OrderBy(s => s.Code).ThenBy(s => s.TypeOperation).ToListAsync(); }
    public async Task<bool> MettreAJourAsync(int seuilId, decimal nouveauMontant, int utilisateurId)
    {
        if (nouveauMontant <= 0) throw new InvalidOperationException("Le seuil doit être positif.");
        var seuil = await _context.SeuilsAML.FirstOrDefaultAsync(s => s.Id == seuilId); if (seuil is null) return false;
        var avant = seuil.Montant; seuil.Montant = nouveauMontant; seuil.DateModification = DateTime.UtcNow;
        _context.AuditLogs.Add(new AuditLog { DateEvenement = DateTime.UtcNow, UtilisateurId = utilisateurId, Action = "ModificationSeuilAML", TableConcernee = "SeuilsAML", EnregistrementId = seuil.Id, ValeurAvant = avant.ToString("0.00"), ValeurApres = nouveauMontant.ToString("0.00"), Details = $"Seuil {seuil.Code} ({seuil.TypeOperation}) modifié." });
        await _context.SaveChangesAsync(); return true;
    }
    public async Task<decimal> GetSeuilVigilanceAsync() => (await GetSeuilAsync("VIGILANCE", "Toutes"))?.Montant ?? 5_000_000m;
    public async Task<decimal> GetSeuilDeclarationAsync() => (await GetSeuilAsync("DECLARATION", "Depot"))?.Montant ?? 10_000_000m;
    public async Task<decimal> GetSeuilBlocageAsync() => (await GetSeuilAsync("BLOCAGE", "Toutes"))?.Montant ?? 50_000_000m;
}
