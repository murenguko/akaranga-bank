using System.Data;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;
using AkarangaBank.Web.ViewModels;
namespace AkarangaBank.Web.Services;

public class ComptabiliteService : IComptabiliteService
{
    private readonly AkarangaBankContext _context;
    private readonly IConformiteService _conformite;
    private readonly ISurveillanceService _surveillance;
    private readonly IDeclarationService _declaration;
    private readonly IServiceProvider _serviceProvider;
    public ComptabiliteService(AkarangaBankContext context, IConformiteService conformite, ISurveillanceService surveillance, IDeclarationService declaration, IServiceProvider serviceProvider)
    { _context = context; _conformite = conformite; _surveillance = surveillance; _declaration = declaration; _serviceProvider = serviceProvider; }

    public Task<Operation> DeposerAsync(DepotViewModel vm, int utilisateurId) => ExecuterCaisseAsync(vm.CompteId, vm.Montant, vm.Commentaire, utilisateurId, depot: true);
    public Task<Operation> RetirerAsync(RetraitViewModel vm, int utilisateurId) => ExecuterCaisseAsync(vm.CompteId, vm.Montant, vm.Commentaire, utilisateurId, depot: false);

    private async Task<Operation> ExecuterCaisseAsync(int compteId, decimal montant, string? commentaire, int utilisateurId, bool depot)
    {
        if (montant <= 0) throw new InvalidOperationException("Le montant doit être positif.");
        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var compte = await _context.Comptes.Include(c => c.Client).FirstOrDefaultAsync(c => c.Id == compteId && !c.EstCompteInterne)
                ?? throw new InvalidOperationException("Compte introuvable.");
            var utilisateur = await _context.Users.FindAsync(utilisateurId) ?? throw new InvalidOperationException("Utilisateur introuvable.");
            if (compte.Statut != "Actif") throw new InvalidOperationException("Le compte doit être actif pour effectuer cette opération.");
            if (utilisateur.AgenceId != compte.AgenceId) throw new InvalidOperationException("L'opération doit être effectuée dans l'agence du compte.");
            if (!depot && compte.Solde < montant) throw new InvalidOperationException("Solde insuffisant.");
            var type = depot ? "Depot" : "Retrait";
            var analyse = await _conformite.AnalyserTransactionAsync(compteId, montant, type);
            if (analyse.EstBloquee) throw new InvalidOperationException(analyse.Motif ?? "Opération bloquée par les règles de conformité.");
            var fractionnement = await _conformite.DetecterFractionnementAsync(compteId, montant, DateTime.UtcNow);
            var caisse = await GetOrCreateCompteInterneAsync(compte.AgenceId, "9100");
            var now = DateTime.UtcNow;
            var operation = new Operation { Reference = GenererReference("AKR"), TypeOperation = type, CompteId = compte.Id, UtilisateurId = utilisateurId, AgenceId = compte.AgenceId, Montant = montant, Devise = compte.Devise, Statut = "Valide", DateOperation = now, DateValidation = now, Commentaire = commentaire?.Trim() };
            _context.Operations.Add(operation);
            await _context.SaveChangesAsync();
            var ecritures = depot
                ? new[] { Ecriture(operation, caisse, "Debit", montant, "Versement espèces"), Ecriture(operation, compte, "Credit", montant, $"Dépôt sur compte {compte.NumeroCompte}") }
                : new[] { Ecriture(operation, compte, "Debit", montant, $"Retrait du compte {compte.NumeroCompte}"), Ecriture(operation, caisse, "Credit", montant, "Décaissement espèces") };
            VerifierEquilibre(ecritures);
            _context.Ecritures.AddRange(ecritures);
            compte.Solde += depot ? montant : -montant;
            caisse.Solde += depot ? montant : -montant;
            compte.DateModification = caisse.DateModification = now;
            await _context.SaveChangesAsync();
            // Run rolling-pattern checks after persisting this operation inside the
            // transaction so the triggering operation is included in the window.
            var surveillance = await _surveillance.AnalyserTransactionAsync(compteId, montant, type, now);
            await CreerAlertesAsync(compte.ClientId!.Value, operation.Id, analyse, fractionnement, surveillance);
            await tx.CommitAsync();
            return operation;
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task<Operation> VirerAsync(VirementViewModel vm, int utilisateurId)
    {
        if (vm.Montant <= 0) throw new InvalidOperationException("Le montant doit être positif.");
        if (vm.CompteSourceId == vm.CompteDestinationId) throw new InvalidOperationException("Les comptes source et destination doivent être différents.");
        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var source = await _context.Comptes.Include(c => c.Agence).FirstOrDefaultAsync(c => c.Id == vm.CompteSourceId && !c.EstCompteInterne) ?? throw new InvalidOperationException("Compte source introuvable.");
            var destination = await _context.Comptes.FirstOrDefaultAsync(c => c.Id == vm.CompteDestinationId && !c.EstCompteInterne) ?? throw new InvalidOperationException("Compte destination introuvable.");
            var user = await _context.Users.FindAsync(utilisateurId) ?? throw new InvalidOperationException("Utilisateur introuvable.");
            if (source.AgenceId != destination.AgenceId) throw new InvalidOperationException("Pour un virement entre agences, utilisez le parcours inter-agences.");
            if (user.AgenceId != source.AgenceId) throw new InvalidOperationException("L'opération doit être effectuée dans l'agence du compte source.");
            if (source.Statut != "Actif" || destination.Statut != "Actif") throw new InvalidOperationException("Les deux comptes doivent être actifs.");
            if (source.Devise != destination.Devise) throw new InvalidOperationException("Les devises des deux comptes doivent être identiques.");
            if (source.Solde < vm.Montant) throw new InvalidOperationException("Solde insuffisant sur le compte source.");
            var analyse = await _conformite.AnalyserTransactionAsync(source.Id, vm.Montant, "Virement");
            if (analyse.EstBloquee) throw new InvalidOperationException(analyse.Motif ?? "Virement bloqué par les règles de conformité.");
            var fractionnement = await _conformite.DetecterFractionnementAsync(source.Id, vm.Montant, DateTime.UtcNow);
            var now = DateTime.UtcNow;
            var virement = new Virement { Reference = GenererReference("VIR"), CompteSourceId = source.Id, CompteDestinationId = destination.Id, UtilisateurId = utilisateurId, Montant = vm.Montant, Devise = source.Devise, Statut = "Valide", Motif = vm.Motif?.Trim(), ControleAML = analyse.NecessiteVigilanceRenforcee || fractionnement, DateVirement = now, DateValidation = now };
            _context.Virements.Add(virement);
            await _context.SaveChangesAsync();
            var operation = new Operation { Reference = virement.Reference, TypeOperation = "Virement", CompteId = source.Id, UtilisateurId = utilisateurId, AgenceId = source.AgenceId, VirementId = virement.Id, Montant = vm.Montant, Devise = source.Devise, Statut = "Valide", DateOperation = now, DateValidation = now, Commentaire = vm.Motif?.Trim() };
            _context.Operations.Add(operation);
            await _context.SaveChangesAsync();
            var ecritures = new[] { Ecriture(operation, source, "Debit", vm.Montant, $"Virement sortant vers {destination.NumeroCompte}"), Ecriture(operation, destination, "Credit", vm.Montant, $"Virement entrant depuis {source.NumeroCompte}") };
            VerifierEquilibre(ecritures);
            _context.Ecritures.AddRange(ecritures);
            source.Solde -= vm.Montant; destination.Solde += vm.Montant;
            source.DateModification = destination.DateModification = now;
            await _context.SaveChangesAsync();
            var surveillance = await _surveillance.AnalyserTransactionAsync(source.Id, vm.Montant, "Virement", now);
            virement.ControleAML = analyse.NecessiteVigilanceRenforcee || fractionnement || surveillance.NecessiteDeclaration;
            await _context.SaveChangesAsync();
            await CreerAlertesAsync(source.ClientId!.Value, operation.Id, analyse, fractionnement, surveillance);
            await tx.CommitAsync();

            try
            {
                var notifService = (INotificationService?)_serviceProvider.GetService(typeof(INotificationService));
                if (notifService != null)
                {
                    if (destination.ClientId.HasValue)
                    {
                        var destClient = await _context.Clients.FindAsync(destination.ClientId.Value);
                        if (destClient != null && !string.IsNullOrEmpty(destClient.Email))
                        {
                            var vars = new Dictionary<string, string>
                            {
                                { "NomClient", $"{destClient.Nom} {destClient.Prenom}" },
                                { "NumeroCompte", destination.NumeroCompte },
                                { "Montant", vm.Montant.ToString("N0") },
                                { "Motif", vm.Motif ?? "Virement bancaire" },
                                { "Reference", virement.Reference }
                            };
                            _ = notifService.EnvoyerDepuisTemplateAsync("VIREMENT_RECU", "Email", destClient.Email, "fr-BI", vars, destClient.Id);
                        }
                    }

                    if (analyse.Niveau == NiveauAlerte.Critique)
                    {
                        var vars = new Dictionary<string, string>
                        {
                            { "NomClient", source.NumeroCompte },
                            { "NumeroClient", source.NumeroCompte },
                            { "TypeAlerte", "Dépassement ou opération bloquée" },
                            { "Description", analyse.Motif ?? "Alerte critique lors d'un virement" },
                            { "Montant", vm.Montant.ToString("N0") }
                        };
                        _ = notifService.EnvoyerDepuisTemplateAsync("ALERTE_AML_CRITIQUE", "Email", "conformite@akaranga.bi", "fr-BI", vars);
                    }
                }
            }
            catch { }

            return operation;
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task<JournalViewModel> GetJournalAsync(DateTime? du, DateTime? au, string? typeOperation, int page, int taille, int? agenceId = null)
    {
        page = Math.Max(1, page); taille = Math.Clamp(taille, 1, 100);
        var query = _context.Operations.AsNoTracking().Include(o => o.Compte).ThenInclude(c => c.Client).Include(o => o.Utilisateur).Include(o => o.Ecritures).AsQueryable();
        if (agenceId.HasValue) query = query.Where(o => o.AgenceId == agenceId.Value);
        if (du.HasValue) query = query.Where(o => o.DateOperation >= du.Value.Date);
        if (au.HasValue) query = query.Where(o => o.DateOperation < au.Value.Date.AddDays(1));
        if (!string.IsNullOrWhiteSpace(typeOperation)) query = query.Where(o => o.TypeOperation == typeOperation);
        var total = await query.CountAsync();
        var operations = await query.OrderByDescending(o => o.DateOperation).Skip((page - 1) * taille).Take(taille).ToListAsync();
        return new JournalViewModel { Operations = operations, Du = du, Au = au, TypeOperation = typeOperation, Page = page, TaillePage = taille, Total = total };
    }

    public async Task<ReleveViewModel?> GetReleveAsync(int compteId)
    {
        var compte = await _context.Comptes.AsNoTracking().Include(c => c.Client).FirstOrDefaultAsync(c => c.Id == compteId && !c.EstCompteInterne);
        if (compte is null) return null;
        var ecritures = await _context.Ecritures.AsNoTracking().Include(e => e.Operation).Where(e => e.CompteId == compteId).OrderBy(e => e.DateEcriture).ThenBy(e => e.Id).ToListAsync();
        var delta = ecritures.Sum(e => e.Sens == "Credit" ? e.Montant : -e.Montant);
        return new ReleveViewModel { Compte = compte, SoldeInitial = compte.Solde - delta, SoldeFinal = compte.Solde, Ecritures = ecritures };
    }
    public async Task<decimal> GetSoldeAsync(int compteId) => await _context.Comptes.AsNoTracking().Where(c => c.Id == compteId && !c.EstCompteInterne).Select(c => (decimal?)c.Solde).FirstOrDefaultAsync() ?? throw new InvalidOperationException("Compte introuvable.");
    public async Task<bool> VerifierEquilibreGlobalAsync()
    {
        var desEquilibres = await _context.Ecritures
            .GroupBy(e => e.OperationId)
            .AnyAsync(g => g.Count() < 2 ||
                g.Sum(e => e.Sens == "Debit" ? e.Montant : -e.Montant) != 0m);
        if (desEquilibres) return false;

        var operationSansEcriture = await _context.Operations
            .AnyAsync(o => !_context.Ecritures.Any(e => e.OperationId == o.Id));
        if (operationSansEcriture) return false;

        return !await _context.Ecritures.AnyAsync(e =>
            (e.Sens != "Debit" && e.Sens != "Credit") || e.Montant <= 0);
    }

    private async Task<Compte> GetOrCreateCompteInterneAsync(int agenceId, string prefix)
    {
        var numero = $"{prefix}-{agenceId:D4}";
        var compte = await _context.Comptes.FirstOrDefaultAsync(c => c.NumeroCompte == numero);
        if (compte is not null) return compte;
        compte = new Compte { NumeroCompte = numero, AgenceId = agenceId, Solde = 0, Devise = "BIF", Statut = "Actif", EstCompteInterne = true, DateOuverture = DateTime.UtcNow, DateCreation = DateTime.UtcNow };
        _context.Comptes.Add(compte); await _context.SaveChangesAsync(); return compte;
    }
    private static Ecriture Ecriture(Operation operation, Compte compte, string sens, decimal montant, string libelle) => new() { OperationId = operation.Id, CompteId = compte.Id, Sens = sens, Montant = montant, Devise = compte.Devise, DateEcriture = DateTime.UtcNow, Libelle = libelle };
    private async Task CreerAlertesAsync(int clientId, int operationId, ResultatConformite analyse, bool fractionnement, ResultatSurveillance surveillance)
    {
        if (analyse.NecessiteDeclaration || analyse.NecessiteVigilanceRenforcee) await _declaration.CreerAlerteAsync(clientId, operationId, analyse.NecessiteDeclaration ? "DepassementSeuil" : "VigilanceRenforcee", analyse.Niveau.ToString(), analyse.Motif ?? "Vérification complémentaire requise.");
        if (fractionnement) await _declaration.CreerAlerteAsync(clientId, operationId, "Fractionnement", "Eleve", "Suspicion de fractionnement : plusieurs opérations proches du seuil sur 24 heures.");
        foreach (var a in surveillance.Alertes) await _declaration.CreerAlerteAsync(clientId, operationId, a.Regle, a.Niveau.ToString(), a.Description);
    }
    private static void VerifierEquilibre(IEnumerable<Ecriture> ecritures)
    {
        var lignes = ecritures.ToList(); var debit = lignes.Where(e => e.Sens == "Debit").Sum(e => e.Montant); var credit = lignes.Where(e => e.Sens == "Credit").Sum(e => e.Montant);
        if (lignes.Count < 2 || debit != credit) throw new InvalidOperationException("Les écritures de l'opération ne sont pas équilibrées.");
    }
    private static string GenererReference(string prefix) => $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..(prefix.Length + 16)].ToUpperInvariant();
}
