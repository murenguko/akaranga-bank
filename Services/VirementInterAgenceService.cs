using System.Data;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;
using AkarangaBank.Web.ViewModels;
namespace AkarangaBank.Web.Services;
public class VirementInterAgenceService : IVirementInterAgenceService
{
    private readonly AkarangaBankContext _context; private readonly IConformiteService _conformite; private readonly ISurveillanceService _surveillance; private readonly IDeclarationService _declaration;
    public VirementInterAgenceService(AkarangaBankContext context, IConformiteService conformite, ISurveillanceService surveillance, IDeclarationService declaration) { _context = context; _conformite = conformite; _surveillance = surveillance; _declaration = declaration; }
    public async Task<VirementInterAgence> InitierAsync(VirementInterAgenceViewModel vm, int utilisateurId)
    {
        if (vm.Montant <= 0 || vm.CompteSourceId == vm.CompteDestinationId) throw new InvalidOperationException("Comptes différents et montant positif requis.");
        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var source = await _context.Comptes.FirstOrDefaultAsync(c => c.Id == vm.CompteSourceId && !c.EstCompteInterne) ?? throw new InvalidOperationException("Compte source introuvable.");
            var dest = await _context.Comptes.FirstOrDefaultAsync(c => c.Id == vm.CompteDestinationId && !c.EstCompteInterne) ?? throw new InvalidOperationException("Compte destination introuvable.");
            var user = await _context.Users.FindAsync(utilisateurId) ?? throw new InvalidOperationException("Utilisateur introuvable.");
            if (source.AgenceId == dest.AgenceId) throw new InvalidOperationException("Pour un virement intra-agence, utilisez le virement standard.");
            if (source.Statut != "Actif" || dest.Statut != "Actif") throw new InvalidOperationException("Les deux comptes doivent être actifs.");
            if (source.Devise != dest.Devise) throw new InvalidOperationException("Les devises des comptes doivent être identiques.");
            if (user.AgenceId != source.AgenceId) throw new InvalidOperationException("Le compte source doit appartenir à votre agence.");
            if (source.Solde < vm.Montant) throw new InvalidOperationException("Solde insuffisant.");
            var analyse = await _conformite.AnalyserTransactionAsync(source.Id, vm.Montant, "VirementInterAgence");
            if (analyse.EstBloquee) throw new InvalidOperationException(analyse.Motif ?? "Virement bloqué.");
            var fractionnement = await _conformite.DetecterFractionnementAsync(source.Id, vm.Montant, DateTime.UtcNow);
            var now = DateTime.UtcNow; var reference = Ref("VIR");
            var virement = new Virement { Reference = reference, CompteSourceId = source.Id, CompteDestinationId = dest.Id, UtilisateurId = utilisateurId, Montant = vm.Montant, Devise = source.Devise, Statut = "EnAttente", Motif = vm.Motif?.Trim(), ControleAML = analyse.NecessiteVigilanceRenforcee, DateVirement = now };
            _context.Virements.Add(virement); await _context.SaveChangesAsync();
            var entity = new VirementInterAgence { Reference = reference, VirementId = virement.Id, AgenceSourceId = source.AgenceId, AgenceDestinationId = dest.AgenceId, Montant = vm.Montant, Devise = source.Devise, Statut = "EnAttente", DateCreation = now };
            _context.VirementsInterAgences.Add(entity); await _context.SaveChangesAsync();
            var monitoring = await _surveillance.AnalyserTransactionAsync(source.Id, vm.Montant, "VirementInterAgence", now);
            if (analyse.NecessiteDeclaration || analyse.NecessiteVigilanceRenforcee) await _declaration.CreerAlerteAsync(source.ClientId!.Value, null, analyse.NecessiteDeclaration ? "DepassementSeuil" : "VigilanceRenforcee", analyse.Niveau.ToString(), analyse.Motif ?? "Vérification AML requise.");
            if (fractionnement) await _declaration.CreerAlerteAsync(source.ClientId!.Value, null, "Fractionnement", "Eleve", "Suspicion de fractionnement sur le compte source.");
            foreach (var a in monitoring.Alertes) await _declaration.CreerAlerteAsync(source.ClientId!.Value, null, a.Regle, a.Niveau.ToString(), a.Description);
            await tx.CommitAsync(); return entity;
        }
        catch { await tx.RollbackAsync(); throw; }
    }
    public async Task<bool> ValiderAsync(int id, int valideParId)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var item = await _context.VirementsInterAgences.Include(v => v.Virement).ThenInclude(v => v.CompteSource).Include(v => v.Virement).ThenInclude(v => v.CompteDestination).FirstOrDefaultAsync(v => v.Id == id);
            if (item is null || item.Statut != "EnAttente") return false;
            var source = item.Virement.CompteSource; var dest = item.Virement.CompteDestination;
            if (source.Statut != "Actif" || dest.Statut != "Actif" || source.Solde < item.Montant) throw new InvalidOperationException("Les comptes doivent être actifs et le solde source suffisant.");
            var analyse = await _conformite.AnalyserTransactionAsync(source.Id, item.Montant, "VirementInterAgence");
            if (analyse.EstBloquee) throw new InvalidOperationException(analyse.Motif ?? "Virement bloqué par les règles AML.");
            var caisseSource = await GetCompteInterneAsync(item.AgenceSourceId, "9000"); var caisseDest = await GetCompteInterneAsync(item.AgenceDestinationId, "9000");
            var op = new Operation { Reference = item.Reference, TypeOperation = "VirementInterAgence", CompteId = source.Id, UtilisateurId = valideParId, AgenceId = item.AgenceSourceId, VirementId = item.VirementId, Montant = item.Montant, Devise = item.Devise, Statut = "Valide", DateOperation = DateTime.UtcNow, DateValidation = DateTime.UtcNow, Commentaire = item.Virement.Motif };
            _context.Operations.Add(op); await _context.SaveChangesAsync();
            var rows = new[] {
                Row(op, source, "Debit", item.Montant, $"Virement inter-agence vers {dest.NumeroCompte}"),
                Row(op, caisseSource, "Credit", item.Montant, "Crédit compte de compensation agence source"),
                Row(op, caisseDest, "Debit", item.Montant, "Débit compte de compensation agence destination"),
                Row(op, dest, "Credit", item.Montant, $"Virement inter-agence depuis {source.NumeroCompte}") };
            if (rows.Where(e => e.Sens == "Debit").Sum(e => e.Montant) != rows.Where(e => e.Sens == "Credit").Sum(e => e.Montant)) throw new InvalidOperationException("Écritures de compensation déséquilibrées.");
            _context.Ecritures.AddRange(rows); source.Solde -= item.Montant; dest.Solde += item.Montant; caisseSource.Solde += item.Montant; caisseDest.Solde -= item.Montant;
            var now = DateTime.UtcNow; source.DateModification = dest.DateModification = caisseSource.DateModification = caisseDest.DateModification = now;
            item.Statut = "EnCompensation"; item.ValideParId = valideParId; item.DateCompensation = now; item.Virement.Statut = "Valide"; item.Virement.DateValidation = now;
            await _context.SaveChangesAsync(); await tx.CommitAsync(); return true;
        }
        catch { await tx.RollbackAsync(); throw; }
    }
    public async Task<bool> RejeterAsync(int id, int valideParId, string motif)
    {
        var item = await _context.VirementsInterAgences.Include(v => v.Virement).FirstOrDefaultAsync(v => v.Id == id);
        if (item is null || item.Statut != "EnAttente") return false;
        item.Statut = "Rejete"; item.MotifRejet = string.IsNullOrWhiteSpace(motif) ? "Motif non précisé." : motif.Trim(); item.ValideParId = valideParId; item.Virement.Statut = "Rejete";
        await _context.SaveChangesAsync(); return true;
    }
    public async Task<IEnumerable<VirementInterAgence>> GetEnAttenteAsync(int? agenceId)
    {
        var query = _context.VirementsInterAgences.AsNoTracking()
            .Include(v => v.AgenceSource)
            .Include(v => v.AgenceDestination)
            .Where(v => v.Statut == "EnAttente");
        if (agenceId.HasValue) query = query.Where(v => v.AgenceDestinationId == agenceId.Value);
        return await query.OrderByDescending(v => v.DateCreation).ToListAsync();
    }
    public async Task<IEnumerable<VirementInterAgence>> GetHistoriqueAsync(int agenceId, DateTime? du, DateTime? au)
    {
        var q = _context.VirementsInterAgences.AsNoTracking().Include(v => v.AgenceSource).Include(v => v.AgenceDestination).Where(v => v.AgenceSourceId == agenceId || v.AgenceDestinationId == agenceId);
        if (du.HasValue) q = q.Where(v => v.DateCreation >= du.Value.Date); if (au.HasValue) q = q.Where(v => v.DateCreation < au.Value.Date.AddDays(1));
        return await q.OrderByDescending(v => v.DateCreation).ToListAsync();
    }
    private async Task<Compte> GetCompteInterneAsync(int agenceId, string prefix)
    {
        var numero = $"{prefix}-{agenceId:D4}"; var compte = await _context.Comptes.FirstOrDefaultAsync(c => c.NumeroCompte == numero);
        if (compte is not null) return compte;
        compte = new Compte { NumeroCompte = numero, AgenceId = agenceId, Devise = "BIF", Statut = "Actif", EstCompteInterne = true, DateOuverture = DateTime.UtcNow, DateCreation = DateTime.UtcNow };
        _context.Comptes.Add(compte); await _context.SaveChangesAsync(); return compte;
    }
    private static Ecriture Row(Operation o, Compte c, string sens, decimal amount, string label) => new() { OperationId = o.Id, CompteId = c.Id, Sens = sens, Montant = amount, Devise = c.Devise, DateEcriture = DateTime.UtcNow, Libelle = label };
    private static string Ref(string prefix) => $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..19].ToUpperInvariant();
}
