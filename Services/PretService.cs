using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;
using AkarangaBank.Web.ViewModels;

namespace AkarangaBank.Web.Services;

public class PretService : IPretService
{
    private readonly AkarangaBankContext _context;
    private readonly IServiceProvider _serviceProvider;

    public PretService(AkarangaBankContext context, IServiceProvider serviceProvider)
    {
        _context = context;
        _serviceProvider = serviceProvider;
    }

    public decimal CalculerMensualite(decimal capital, decimal tauxAnnuel, int dureeMois)
    {
        if (dureeMois <= 0)
            throw new ArgumentException("La durée doit être supérieure à 0 mois.", nameof(dureeMois));
        if (capital <= 0)
            return 0m;

        if (tauxAnnuel <= 0m)
        {
            return Math.Round(capital / dureeMois, 2);
        }

        double r = (double)(tauxAnnuel / 100m) / 12.0;
        double p = (double)capital;
        double m = p * (r / (1.0 - Math.Pow(1.0 + r, -dureeMois)));
        return Math.Round((decimal)m, 2);
    }

    public List<EcheancePret> GenererEcheancier(decimal capital, decimal tauxAnnuel, int dureeMois, DateTime dateDebut)
    {
        var echeances = new List<EcheancePret>();
        if (dureeMois <= 0 || capital <= 0) return echeances;

        decimal mensualite = CalculerMensualite(capital, tauxAnnuel, dureeMois);
        decimal capitalRestant = capital;
        decimal r = (tauxAnnuel / 100m) / 12m;

        for (int i = 1; i <= dureeMois; i++)
        {
            decimal interet = Math.Round(capitalRestant * r, 2);
            decimal partCapital;

            if (i == dureeMois)
            {
                // Dernier mois : ajustement exact pour que la somme des capitaux == capital principal
                partCapital = capitalRestant;
                mensualite = partCapital + interet;
                capitalRestant = 0m;
            }
            else
            {
                partCapital = Math.Round(mensualite - interet, 2);
                if (partCapital > capitalRestant)
                    partCapital = capitalRestant;
                capitalRestant -= partCapital;
            }

            var echeance = new EcheancePret
            {
                NumeroEcheance = i,
                Capital = partCapital,
                Interet = interet,
                MontantTotal = partCapital + interet,
                SoldeRestant = Math.Max(0m, capitalRestant),
                DateEcheance = dateDebut.AddMonths(i),
                Statut = "EnAttente"
            };

            echeances.Add(echeance);
        }

        return echeances;
    }

    public async Task<Pret> CreerDemandeAsync(PretDemandeViewModel vm, int utilisateurId)
    {
        var client = await _context.Clients.FindAsync(vm.ClientId)
            ?? throw new InvalidOperationException("Client introuvable.");

        var compte = await _context.Comptes.FindAsync(vm.CompteDebitId)
            ?? throw new InvalidOperationException("Compte bancaire introuvable.");

        if (compte.EstCompteInterne || compte.ClientId != vm.ClientId)
            throw new InvalidOperationException("Le compte sélectionné n'appartient pas au client demandeur.");

        if (compte.Statut != "Actif")
            throw new InvalidOperationException("Le compte de débit n'est pas actif.");

        decimal taux = vm.TauxIndicatif > 0 ? vm.TauxIndicatif : 12m;
        decimal mensualite = CalculerMensualite(vm.Montant, taux, vm.DureeMois);

        var pret = new Pret
        {
            Reference = GenererReference("PRT"),
            ClientId = vm.ClientId,
            CompteDebitId = vm.CompteDebitId,
            TypePret = vm.TypePret,
            MontantPrincipal = vm.Montant,
            TauxInteret = taux,
            DureeMois = vm.DureeMois,
            Mensualite = mensualite,
            MontantRembourse = 0m,
            SoldeRestant = vm.Montant,
            Commentaire = vm.Commentaire?.Trim(),
            Statut = "EnAttente",
            DateDemande = DateTime.UtcNow
        };

        _context.Prets.Add(pret);
        await _context.SaveChangesAsync();
        return pret;
    }

    public async Task<bool> ValiderPretAsync(int pretId, decimal? tauxPersonnalise, int utilisateurId)
    {
        var pret = await _context.Prets.FindAsync(pretId);
        if (pret is null || pret.Statut != "EnAttente") return false;

        if (tauxPersonnalise.HasValue)
        {
            pret.TauxInteret = tauxPersonnalise.Value;
            pret.Mensualite = CalculerMensualite(pret.MontantPrincipal, pret.TauxInteret, pret.DureeMois);
        }

        pret.Statut = "Valide";
        pret.ValideParId = utilisateurId;
        pret.DateValidation = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Notification email au client (EF-14.18)
        try
        {
            var notifService = (INotificationService?)_serviceProvider.GetService(typeof(INotificationService));
            if (notifService != null)
            {
                var client = await _context.Clients.FindAsync(pret.ClientId);
                if (client != null && !string.IsNullOrEmpty(client.Email))
                {
                    var vars = new Dictionary<string, string>
                    {
                        { "NomClient", $"{client.Nom} {client.Prenom}" },
                        { "Reference", pret.Reference },
                        { "Montant", pret.MontantPrincipal.ToString("N0") },
                        { "Taux", pret.TauxInteret.ToString("0.00") },
                        { "Mensualite", pret.Mensualite.ToString("N0") },
                        { "Duree", pret.DureeMois.ToString() }
                    };
                    _ = notifService.EnvoyerDepuisTemplateAsync("PRET_VALIDE", "Email", client.Email, "fr-BI", vars, client.Id);
                }
            }
        }
        catch { }

        return true;
    }

    public async Task<bool> RejeterPretAsync(int pretId, string motif, int utilisateurId)
    {
        if (string.IsNullOrWhiteSpace(motif))
            throw new InvalidOperationException("Le motif de rejet est obligatoire.");

        var pret = await _context.Prets.FindAsync(pretId);
        if (pret is null || pret.Statut != "EnAttente") return false;

        pret.Statut = "Rejete";
        pret.MotifRejet = motif.Trim();
        pret.ValideParId = utilisateurId;
        pret.DateValidation = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Operation> DebloquerAsync(int pretId, int utilisateurId)
    {
        var pret = await _context.Prets
            .Include(p => p.CompteDebit)
            .ThenInclude(c => c.Agence)
            .FirstOrDefaultAsync(p => p.Id == pretId)
            ?? throw new InvalidOperationException("Prêt introuvable.");

        if (pret.Statut != "Valide")
            throw new InvalidOperationException("Seul un prêt validé peut être débloqué.");

        var compte = pret.CompteDebit;
        if (compte.Statut != "Actif")
            throw new InvalidOperationException("Le compte bénéficiaire n'est pas actif.");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Mise à jour statut prêt & dates
            DateTime now = DateTime.UtcNow;
            pret.Statut = "EnCours";
            pret.DateDebut = now;
            pret.DateFin = now.AddMonths(pret.DureeMois);
            pret.SoldeRestant = pret.MontantPrincipal;
            pret.MontantRembourse = 0m;

            // 2. Génération automatique de l'échéancier
            var echeances = GenererEcheancier(pret.MontantPrincipal, pret.TauxInteret, pret.DureeMois, now);
            foreach (var ech in echeances)
            {
                ech.PretId = pret.Id;
                _context.EcheancesPret.Add(ech);
            }

            // 3. Opération financière de déblocage
            var operation = new Operation
            {
                Reference = GenererReference("DBL"),
                TypeOperation = "DeblocagePret",
                CompteId = compte.Id,
                UtilisateurId = utilisateurId,
                AgenceId = compte.AgenceId,
                PretId = pret.Id,
                Montant = pret.MontantPrincipal,
                Devise = compte.Devise,
                Statut = "Valide",
                DateOperation = now,
                DateValidation = now,
                Commentaire = $"Déblocage prêt {pret.Reference}"
            };
            _context.Operations.Add(operation);
            await _context.SaveChangesAsync();

            // 4. Écritures en partie double
            int compteCompensationId = await GetComptePassageIdAsync(compte.AgenceId);
            var debit = new Ecriture
            {
                OperationId = operation.Id,
                CompteId = compteCompensationId,
                Sens = "Debit",
                Montant = pret.MontantPrincipal,
                Devise = compte.Devise,
                DateEcriture = now,
                Libelle = $"Financement prêt {pret.Reference}"
            };
            var credit = new Ecriture
            {
                OperationId = operation.Id,
                CompteId = compte.Id,
                Sens = "Credit",
                Montant = pret.MontantPrincipal,
                Devise = compte.Devise,
                DateEcriture = now,
                Libelle = $"Crédit déblocage prêt {pret.Reference}"
            };
            _context.Ecritures.AddRange(debit, credit);

            // 5. Crédit du solde du client
            compte.Solde += pret.MontantPrincipal;
            compte.DateModification = now;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return operation;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Operation> RembourserAsync(int echeanceId, int utilisateurId)
    {
        var echeance = await _context.EcheancesPret
            .Include(e => e.Pret)
            .ThenInclude(p => p.CompteDebit)
            .FirstOrDefaultAsync(e => e.Id == echeanceId)
            ?? throw new InvalidOperationException("Échéance introuvable.");

        if (echeance.Statut == "Payee")
            throw new InvalidOperationException("Cette échéance est déjà payée.");

        var pret = echeance.Pret;
        var compte = pret.CompteDebit;

        decimal montantARembourser = echeance.MontantTotal + echeance.Penalite;

        if (compte.Solde < montantARembourser)
            throw new InvalidOperationException($"Solde insuffisant pour le remboursement ({compte.Solde:N2} < {montantARembourser:N2} BIF).");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            DateTime now = DateTime.UtcNow;

            // 1. Marquer échéance Payee
            echeance.Statut = "Payee";
            echeance.DatePaiement = now;

            // 2. Mettre à jour prêt
            pret.MontantRembourse += echeance.Capital;
            pret.SoldeRestant = Math.Max(0m, pret.SoldeRestant - echeance.Capital);

            // Vérifier si toutes les échéances sont payées
            var resteAPayer = await _context.EcheancesPret
                .Where(e => e.PretId == pret.Id && e.Id != echeanceId)
                .AnyAsync(e => e.Statut != "Payee");

            if (!resteAPayer)
            {
                pret.Statut = "Solde";
            }

            // 3. Opération comptable
            var operation = new Operation
            {
                Reference = GenererReference("RMB"),
                TypeOperation = "RemboursementPret",
                CompteId = compte.Id,
                UtilisateurId = utilisateurId,
                AgenceId = compte.AgenceId,
                PretId = pret.Id,
                Montant = montantARembourser,
                Devise = compte.Devise,
                Statut = "Valide",
                DateOperation = now,
                DateValidation = now,
                Commentaire = $"Remboursement échéance #{echeance.NumeroEcheance} ({pret.Reference})"
            };
            _context.Operations.Add(operation);
            await _context.SaveChangesAsync();

            // 4. Écritures en partie double
            int comptePassageId = await GetComptePassageIdAsync(compte.AgenceId);
            var debit = new Ecriture
            {
                OperationId = operation.Id,
                CompteId = compte.Id,
                Sens = "Debit",
                Montant = montantARembourser,
                Devise = compte.Devise,
                DateEcriture = now,
                Libelle = $"Débit remboursement prêt {pret.Reference}"
            };
            var credit = new Ecriture
            {
                OperationId = operation.Id,
                CompteId = comptePassageId,
                Sens = "Credit",
                Montant = montantARembourser,
                Devise = compte.Devise,
                DateEcriture = now,
                Libelle = $"Règlement échéance #{echeance.NumeroEcheance}"
            };
            _context.Ecritures.AddRange(debit, credit);

            // 5. Débit compte client
            compte.Solde -= montantARembourser;
            compte.DateModification = now;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return operation;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<int> MettreAJourRetardsAsync()
    {
        DateTime now = DateTime.UtcNow;
        var echeancesEnRetard = await _context.EcheancesPret
            .Include(e => e.Pret)
            .Where(e => e.Statut == "EnAttente" && e.DateEcheance < now)
            .ToListAsync();

        int count = 0;
        foreach (var ech in echeancesEnRetard)
        {
            ech.Statut = "EnRetard";

            // Pénalité : 0.5% par mois de retard, min 500 BIF
            int moisRetard = Math.Max(1, (int)Math.Ceiling((now - ech.DateEcheance).TotalDays / 30.0));
            decimal penalite = Math.Max(500m, ech.MontantTotal * 0.005m * moisRetard);
            ech.Penalite = Math.Round(penalite, 2);
            count++;
        }

        // Vérifier les prêts avec >= 3 échéances en retard -> statut Impaye
        var pretsIds = echeancesEnRetard.Select(e => e.PretId).Distinct().ToList();
        foreach (var pId in pretsIds)
        {
            var nbRetards = await _context.EcheancesPret
                .CountAsync(e => e.PretId == pId && e.Statut == "EnRetard");

            if (nbRetards >= 3)
            {
                var p = await _context.Prets.FindAsync(pId);
                if (p != null && p.Statut == "EnCours")
                {
                    p.Statut = "Impaye";
                }
            }
        }

        await _context.SaveChangesAsync();
        return count;
    }

    public async Task<PretListViewModel> GetPagedAsync(string? recherche, string? statut, string? typePret, int page, int taille, int? agenceId = null)
    {
        var query = _context.Prets
            .Include(p => p.Client)
            .Include(p => p.CompteDebit)
            .AsNoTracking()
            .AsQueryable();

        if (agenceId.HasValue)
            query = query.Where(p => p.Client.AgenceId == agenceId.Value);

        if (!string.IsNullOrWhiteSpace(recherche))
        {
            var r = recherche.Trim();
            query = query.Where(p =>
                p.Reference.Contains(r) ||
                p.Client.Nom.Contains(r) ||
                p.Client.Prenom.Contains(r) ||
                p.CompteDebit.NumeroCompte.Contains(r));
        }

        if (!string.IsNullOrWhiteSpace(statut))
            query = query.Where(p => p.Statut == statut);

        if (!string.IsNullOrWhiteSpace(typePret))
            query = query.Where(p => p.TypePret == typePret);

        var total = await query.CountAsync();
        var prets = await query
            .OrderByDescending(p => p.DateDemande)
            .Skip((page - 1) * taille)
            .Take(taille)
            .ToListAsync();

        return new PretListViewModel
        {
            Prets = prets,
            Recherche = recherche,
            Statut = statut,
            TypePret = typePret,
            Page = page,
            TaillePage = taille,
            Total = total
        };
    }

    public async Task<PretDetailsViewModel?> GetDetailsAsync(int id)
    {
        var pret = await _context.Prets
            .Include(p => p.Client)
            .Include(p => p.CompteDebit)
            .Include(p => p.ValidePar)
            .Include(p => p.Echeances)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pret is null) return null;

        return new PretDetailsViewModel
        {
            Pret = pret,
            Echeances = pret.Echeances.OrderBy(e => e.NumeroEcheance).ToList()
        };
    }

    public async Task<Pret?> GetByIdAsync(int id)
    {
        return await _context.Prets
            .Include(p => p.Client)
            .Include(p => p.CompteDebit)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    private async Task<int> GetComptePassageIdAsync(int agenceId)
    {
        var compte = await _context.Comptes
            .FirstOrDefaultAsync(c => c.NumeroCompte == $"9000-{agenceId:D4}");

        if (compte is null)
        {
            compte = new Compte
            {
                NumeroCompte = $"9000-{agenceId:D4}",
                AgenceId = agenceId,
                Solde = 0m,
                Devise = "BIF",
                Statut = "Actif",
                EstCompteInterne = true,
                DateOuverture = DateTime.UtcNow
            };
            _context.Comptes.Add(compte);
            await _context.SaveChangesAsync();
        }

        return compte.Id;
    }

    private static string GenererReference(string prefix)
    {
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"{prefix}-{date}-{random}";
    }
}
