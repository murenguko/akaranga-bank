using System.Text;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;
namespace AkarangaBank.Web.Services;
public class CNRFService : ICNRFService
{
    private readonly AkarangaBankContext _context;
    public CNRFService(AkarangaBankContext context) => _context = context;
    public async Task<DeclarationSoupcon> GenererDOSAsync(int alerteId, int redigeParId)
    {
        var existing = await _context.DeclarationsSoupcon.Include(d => d.Client).FirstOrDefaultAsync(d => d.AlerteId == alerteId);
        if (existing is not null) return existing;
        var alerte = await _context.AlertesAML.Include(a => a.Client).Include(a => a.Operation).ThenInclude(o => o!.Compte).FirstOrDefaultAsync(a => a.Id == alerteId)
            ?? throw new InvalidOperationException("Alerte AML introuvable.");
        var type = alerte.Operation?.TypeOperation is "Depot" or "Retrait" && alerte.TypeAlerte == "DepassementSeuil" ? "DTSE" : "DOS";
        var d = new DeclarationSoupcon { Reference = $"CNRF-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..20].ToUpperInvariant(), AlerteId = alerte.Id, ClientId = alerte.ClientId, TypeDeclaration = type, Contenu = await GenererContenuDOSAsync(alerte), MotifSuspicion = alerte.Description, Statut = "Brouillon", DateCreation = DateTime.UtcNow, RedigeParId = redigeParId };
        _context.DeclarationsSoupcon.Add(d); await _context.SaveChangesAsync(); return d;
    }
    public async Task<bool> TransmettreAsync(int declarationId)
    {
        var d = await _context.DeclarationsSoupcon.FirstOrDefaultAsync(x => x.Id == declarationId); if (d is null) return false;
        if (d.Statut is not ("Brouillon" or "Validee")) return false;
        d.Statut = "Transmise"; d.DateTransmission = DateTime.UtcNow; await _context.SaveChangesAsync(); return true;
    }
    public async Task<bool> EnregistrerAccuseAsync(int declarationId, string reponse)
    {
        var d = await _context.DeclarationsSoupcon.FirstOrDefaultAsync(x => x.Id == declarationId); if (d is null || d.Statut != "Transmise") return false;
        d.Statut = "Accusee"; d.DateAccuseReception = DateTime.UtcNow; d.ReponseCNRF = string.IsNullOrWhiteSpace(reponse) ? "Accusé de réception enregistré." : reponse.Trim()[..Math.Min(reponse.Trim().Length, 1000)];
        await _context.SaveChangesAsync(); return true;
    }
    public async Task<IEnumerable<DeclarationSoupcon>> GetDeclarationsAsync(string? statut)
    {
        var q = _context.DeclarationsSoupcon.AsNoTracking().Include(d => d.Client).Include(d => d.Alerte).AsQueryable();
        if (!string.IsNullOrWhiteSpace(statut)) q = q.Where(d => d.Statut == statut);
        return await q.OrderByDescending(d => d.DateCreation).ToListAsync();
    }
    public Task<string> GenererContenuDOSAsync(AlerteAML alerte)
    {
        var sb = new StringBuilder();
        sb.AppendLine("DÉCLARATION À LA CELLULE NATIONALE DE RENSEIGNEMENT FINANCIER");
        sb.AppendLine($"Référence alerte : {alerte.Id}");
        sb.AppendLine($"Date de génération : {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC");
        sb.AppendLine($"Type et niveau : {alerte.TypeAlerte} / {alerte.Niveau}");
        sb.AppendLine($"Client : {alerte.Client?.NumeroClient} — {alerte.Client?.Nom} {alerte.Client?.Prenom}");
        sb.AppendLine($"Identifiant client : {alerte.Client?.NumeroCNI}");
        sb.AppendLine($"Motif : {alerte.Description}");
        if (alerte.Operation is { } op)
        {
            sb.AppendLine($"Référence opération : {op.Reference}");
            sb.AppendLine($"Type opération : {op.TypeOperation}");
            sb.AppendLine($"Montant : {op.Montant:N2} {op.Devise}");
            sb.AppendLine($"Date opération : {op.DateOperation:dd/MM/yyyy HH:mm} UTC");
            sb.AppendLine($"Compte : {op.Compte?.NumeroCompte}");
        }
        sb.AppendLine("Cette déclaration est confidentielle et doit être traitée selon les procédures de conformité.");
        return Task.FromResult(sb.ToString());
    }
}
