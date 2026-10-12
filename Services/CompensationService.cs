using System.Data;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;
namespace AkarangaBank.Web.Services;
public class CompensationService : ICompensationService
{
    private readonly AkarangaBankContext _context;
    public CompensationService(AkarangaBankContext context) => _context = context;
    public async Task<CompensationAgence> CalculerCompensationAsync(int agenceId, DateTime date)
    {
        var jour = date.Date; var fin = jour.AddDays(1);
        var agence = await _context.Agences.FindAsync(agenceId) ?? throw new InvalidOperationException("Agence introuvable.");
        var virements = await _context.VirementsInterAgences.AsNoTracking().Where(v => v.DateCreation >= jour && v.DateCreation < fin && (v.AgenceSourceId == agenceId || v.AgenceDestinationId == agenceId) && v.Statut == "EnCompensation").ToListAsync();
        var credits = virements.Where(v => v.AgenceDestinationId == agenceId).Sum(v => v.Montant); var debits = virements.Where(v => v.AgenceSourceId == agenceId).Sum(v => v.Montant);
        var account = await GetCompteInterneAsync(agenceId, "9000");
        var current = await _context.CompensationsAgences.FirstOrDefaultAsync(c => c.AgenceId == agenceId && c.DateCompensation >= jour && c.DateCompensation < fin);
        if (current is null) { current = new CompensationAgence { Reference = $"COMP-{jour:yyyyMMdd}-{agenceId:D4}", AgenceId = agenceId, CompteCompensationId = account.Id, DateCompensation = jour }; _context.CompensationsAgences.Add(current); }
        if (current.Statut == "Regle") throw new InvalidOperationException("Cette compensation est déjà réglée et ne peut plus être recalculée.");
        current.TotalDebits = debits; current.TotalCredits = credits; current.Solde = credits - debits; current.Statut = debits == credits ? "Equilibre" : "EnCours"; current.CompteCompensationId = account.Id;
        await _context.SaveChangesAsync(); return current;
    }
    public async Task<bool> ReglerCompensationAsync(int compensationId, int valideParId)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var c = await _context.CompensationsAgences.Include(x => x.CompteCompensation).FirstOrDefaultAsync(x => x.Id == compensationId);
            if (c is null) return false; if (c.Statut == "Regle") return true;
            if (c.TotalCredits - c.TotalDebits != c.Solde) throw new InvalidOperationException("Le solde de compensation ne correspond pas aux totaux.");
            if (c.Solde != 0)
            {
                var settlement = await GetCompteInterneAsync(c.AgenceId, "9200"); var montant = Math.Abs(c.Solde); var credit = c.Solde > 0;
                var op = new Operation { Reference = $"REG-{c.Reference}"[..30], TypeOperation = "ReglementCompensation", CompteId = c.CompteCompensationId, UtilisateurId = valideParId, AgenceId = c.AgenceId, Montant = montant, Devise = c.CompteCompensation.Devise, Statut = "Valide", DateOperation = DateTime.UtcNow, DateValidation = DateTime.UtcNow, Commentaire = $"Règlement de la compensation {c.Reference}" };
                _context.Operations.Add(op); await _context.SaveChangesAsync();
                var a = new Ecriture { OperationId = op.Id, CompteId = c.CompteCompensationId, Sens = credit ? "Debit" : "Credit", Montant = montant, Devise = c.CompteCompensation.Devise, DateEcriture = DateTime.UtcNow, Libelle = "Règlement compensation" };
                var b = new Ecriture { OperationId = op.Id, CompteId = settlement.Id, Sens = credit ? "Credit" : "Debit", Montant = montant, Devise = settlement.Devise, DateEcriture = DateTime.UtcNow, Libelle = "Compte de règlement inter-agences" };
                _context.Ecritures.AddRange(a, b); c.CompteCompensation.Solde -= c.Solde; settlement.Solde += c.Solde;
            }
            c.Statut = "Regle"; await _context.SaveChangesAsync();
            var day = c.DateCompensation.Date;
            var transfers = await _context.VirementsInterAgences.Where(v => v.DateCreation >= day && v.DateCreation < day.AddDays(1) && v.Statut == "EnCompensation").ToListAsync();
            var agencesAttendues = transfers.SelectMany(v => new[] { v.AgenceSourceId, v.AgenceDestinationId }).Distinct().ToList();
            var compensationsDuJour = await _context.CompensationsAgences.Where(x => x.DateCompensation >= day && x.DateCompensation < day.AddDays(1)).ToListAsync();
            if (agencesAttendues.Count > 0 && agencesAttendues.All(id => compensationsDuJour.Any(x => x.AgenceId == id && x.Statut == "Regle")))
            {
                foreach (var v in transfers) { v.Statut = "Regle"; v.DateReglement = DateTime.UtcNow; }
                await _context.SaveChangesAsync();
            }
            await tx.CommitAsync(); return true;
        }
        catch { await tx.RollbackAsync(); throw; }
    }
    public async Task<IEnumerable<CompensationAgence>> GetCompensationsAsync(int? agenceId, DateTime? date)
    {
        var q = _context.CompensationsAgences.AsNoTracking().Include(c => c.Agence).Include(c => c.CompteCompensation).AsQueryable();
        if (agenceId.HasValue) q = q.Where(c => c.AgenceId == agenceId.Value);
        if (date.HasValue) { var day = date.Value.Date; q = q.Where(c => c.DateCompensation >= day && c.DateCompensation < day.AddDays(1)); }
        return await q.OrderByDescending(c => c.DateCompensation).ToListAsync();
    }
    public async Task<bool> VerifierEquilibreCompensationsAsync(DateTime date)
    {
        var start = date.Date; var rows = await _context.CompensationsAgences.Where(c => c.DateCompensation >= start && c.DateCompensation < start.AddDays(1)).ToListAsync();
        return rows.Count == 0 || rows.Sum(c => c.TotalDebits) == rows.Sum(c => c.TotalCredits);
    }
    private async Task<Compte> GetCompteInterneAsync(int agenceId, string prefix)
    {
        var numero = $"{prefix}-{agenceId:D4}"; var c = await _context.Comptes.FirstOrDefaultAsync(x => x.NumeroCompte == numero);
        if (c is not null) return c;
        c = new Compte { NumeroCompte = numero, AgenceId = agenceId, Devise = "BIF", Statut = "Actif", EstCompteInterne = true, DateOuverture = DateTime.UtcNow, DateCreation = DateTime.UtcNow };
        _context.Comptes.Add(c); await _context.SaveChangesAsync(); return c;
    }
}
