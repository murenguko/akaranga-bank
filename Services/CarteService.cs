using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;
using AkarangaBank.Web.ViewModels;

namespace AkarangaBank.Web.Services;

public class CarteService : ICarteService
{
    private readonly AkarangaBankContext _context;
    private readonly IConformiteService _conformite;

    public CarteService(AkarangaBankContext context, IConformiteService conformite)
    {
        _context = context;
        _conformite = conformite;
    }

    public async Task<Carte> EmettreCarteAsync(CarteEmissionViewModel vm, int utilisateurId)
    {
        var compte = await _context.Comptes
            .Include(c => c.Client)
            .FirstOrDefaultAsync(c => c.Id == vm.CompteId)
            ?? throw new InvalidOperationException("Compte bancaire introuvable.");

        if (compte.Statut != "Actif" || compte.EstCompteInterne || !compte.ClientId.HasValue)
            throw new InvalidOperationException("Impossible d'émettre une carte sur un compte non actif.");

        string numero = GenererNumeroCarte(vm.TypeCarte);
        string numeroMasque = MaskNumero(numero);

        var carte = new Carte
        {
            NumeroCarte = numero,
            NumeroMasque = numeroMasque,
            CompteId = vm.CompteId,
            ClientId = compte.ClientId ?? (compte.Client?.Id ?? 0),
            TypeCarte = vm.TypeCarte,
            DateExpiration = DateTime.UtcNow.AddYears(3),
            PlafondRetrait = vm.PlafondRetrait,
            PlafondPaiement = vm.PlafondPaiement,
            PlafondLigne = vm.PlafondLigne,
            Statut = "Active",
            DateCreation = DateTime.UtcNow
        };

        _context.Cartes.Add(carte);
        await _context.SaveChangesAsync();
        return carte;
    }

    public async Task<bool> BloquerCarteAsync(CarteActionViewModel vm, int utilisateurId)
    {
        if (vm.NouveauStatut is not ("Bloquee" or "Opposition"))
            throw new InvalidOperationException("Le nouveau statut de carte est invalide.");
        if (string.IsNullOrWhiteSpace(vm.Motif))
            throw new InvalidOperationException("Le motif de blocage est obligatoire.");

        var carte = await _context.Cartes.FindAsync(vm.CarteId);
        if (carte is null) return false;
        if (carte.Statut != "Active")
            throw new InvalidOperationException("Seule une carte active peut être bloquée ou mise en opposition.");

        carte.Statut = vm.NouveauStatut; // Bloquee, Perdue, Volee, Opposition
        carte.MotifOpposition = vm.Motif;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Carte> RenouvelerCarteAsync(int carteId, int utilisateurId)
    {
        var ancienneCarte = await _context.Cartes
            .Include(c => c.Compte)
            .FirstOrDefaultAsync(c => c.Id == carteId)
            ?? throw new InvalidOperationException("Carte introuvable.");

        ancienneCarte.Statut = "Expiree";

        var nouvelleCarte = new Carte
        {
            NumeroCarte = GenererNumeroCarte(ancienneCarte.TypeCarte),
            NumeroMasque = string.Empty, // will be populated
            CompteId = ancienneCarte.CompteId,
            ClientId = ancienneCarte.ClientId,
            TypeCarte = ancienneCarte.TypeCarte,
            DateExpiration = DateTime.UtcNow.AddYears(3),
            PlafondRetrait = ancienneCarte.PlafondRetrait,
            PlafondPaiement = ancienneCarte.PlafondPaiement,
            PlafondLigne = ancienneCarte.PlafondLigne,
            Statut = "Active",
            DateCreation = DateTime.UtcNow
        };
        nouvelleCarte.NumeroMasque = MaskNumero(nouvelleCarte.NumeroCarte);

        _context.Cartes.Add(nouvelleCarte);
        await _context.SaveChangesAsync();
        return nouvelleCarte;
    }

    public async Task<(bool Reussi, string Message, TransactionCarte Transaction)> AutoriserTransactionAsync(CarteTransactionViewModel vm, int utilisateurId)
    {
        var carte = await _context.Cartes
            .Include(c => c.Compte)
            .ThenInclude(cp => cp.Agence)
            .FirstOrDefaultAsync(c => c.Id == vm.CarteId);

        if (carte is null)
            throw new InvalidOperationException("Carte introuvable.");

        DateTime now = DateTime.UtcNow;

        // 1. Vérification du statut
        if (carte.Statut != "Active")
        {
            return await EnregistrerRefusAsync(carte, vm, $"Carte non active (statut: {carte.Statut}).");
        }

        // 2. Vérification expiration
        if (carte.DateExpiration < now)
        {
            return await EnregistrerRefusAsync(carte, vm, "Carte expirée.");
        }

        // 3. Vérification compte actif
        if (carte.Compte.Statut != "Actif")
        {
            return await EnregistrerRefusAsync(carte, vm, $"Compte rattaché non actif ({carte.Compte.Statut}).");
        }

        // 4. Vérification du solde
        if (carte.Compte.Solde < vm.Montant)
        {
            return await EnregistrerRefusAsync(carte, vm, "Solde insuffisant.");
        }

        // 5. Vérification des plafonds journaliers
        DateTime debutJour = now.Date;
        decimal depenseAujourdhui = await _context.TransactionsCarte
            .Where(t => t.CarteId == carte.Id && t.Statut == "Validee" && t.TypeTransaction == vm.TypeTransaction && t.DateTransaction >= debutJour)
            .SumAsync(t => (decimal?)t.Montant) ?? 0m;

        decimal plafondApplicable = vm.TypeTransaction switch
        {
            "Retrait" => carte.PlafondRetrait,
            "PaiementEnLigne" => carte.PlafondLigne,
            _ => carte.PlafondPaiement
        };

        if (depenseAujourdhui + vm.Montant > plafondApplicable)
        {
            return await EnregistrerRefusAsync(carte, vm, $"Dépassement du plafond journalier ({plafondApplicable:N2} BIF).");
        }

        // 6. Transaction atomique
        await using var dbTx = await _context.Database.BeginTransactionAsync();
        try
        {
            string opType = vm.TypeTransaction == "Retrait" ? "RetraitCarte" : "PaiementCarte";

            var operation = new Operation
            {
                Reference = GenererReference("CARTE"),
                TypeOperation = opType,
                CompteId = carte.CompteId,
                UtilisateurId = utilisateurId,
                AgenceId = carte.Compte.AgenceId,
                Montant = vm.Montant,
                Devise = carte.Compte.Devise,
                Statut = "Valide",
                DateOperation = now,
                DateValidation = now,
                Commentaire = $"{vm.TypeTransaction} carte {carte.NumeroMasque} chez {vm.Commercant ?? "DAB/TPE"}"
            };
            _context.Operations.Add(operation);
            await _context.SaveChangesAsync();

            // Écriture débit
            var ecritureDebit = new Ecriture
            {
                OperationId = operation.Id,
                CompteId = carte.CompteId,
                Sens = "Debit",
                Montant = vm.Montant,
                Devise = carte.Compte.Devise,
                DateEcriture = now,
                Libelle = $"{vm.TypeTransaction} carte chez {vm.Commercant ?? "DAB"}"
            };
            _context.Ecritures.Add(ecritureDebit);

            // Mise à jour du solde
            carte.Compte.Solde -= vm.Montant;
            carte.Compte.DateModification = now;

            // Enregistrement transaction carte validée
            var transactionCarte = new TransactionCarte
            {
                CarteId = carte.Id,
                Montant = vm.Montant,
                TypeTransaction = vm.TypeTransaction,
                Statut = "Validee",
                Commercant = vm.Commercant,
                OperationId = operation.Id,
                DateTransaction = now
            };
            _context.TransactionsCarte.Add(transactionCarte);

            await _context.SaveChangesAsync();
            await dbTx.CommitAsync();

            return (true, "Transaction autorisée et débitée avec succès.", transactionCarte);
        }
        catch
        {
            await dbTx.RollbackAsync();
            throw;
        }
    }

    private async Task<(bool Reussi, string Message, TransactionCarte Transaction)> EnregistrerRefusAsync(Carte carte, CarteTransactionViewModel vm, string motif)
    {
        var tx = new TransactionCarte
        {
            CarteId = carte.Id,
            Montant = vm.Montant,
            TypeTransaction = vm.TypeTransaction,
            Statut = "Refusee",
            MotifRefus = motif,
            Commercant = vm.Commercant,
            DateTransaction = DateTime.UtcNow
        };
        _context.TransactionsCarte.Add(tx);
        await _context.SaveChangesAsync();
        return (false, motif, tx);
    }

    public async Task<CarteListViewModel> GetPagedAsync(string? recherche, string? statut, string? typeCarte, int page, int taille)
    {
        var query = _context.Cartes
            .Include(c => c.Client)
            .Include(c => c.Compte)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(recherche))
        {
            var r = recherche.Trim();
            query = query.Where(c =>
                c.NumeroCarte.Contains(r) ||
                c.NumeroMasque.Contains(r) ||
                c.Client.Nom.Contains(r) ||
                c.Client.Prenom.Contains(r) ||
                c.Compte.NumeroCompte.Contains(r));
        }

        if (!string.IsNullOrWhiteSpace(statut))
            query = query.Where(c => c.Statut == statut);

        if (!string.IsNullOrWhiteSpace(typeCarte))
            query = query.Where(c => c.TypeCarte == typeCarte);

        var total = await query.CountAsync();
        var cartes = await query
            .OrderByDescending(c => c.DateCreation)
            .Skip((page - 1) * taille)
            .Take(taille)
            .ToListAsync();

        return new CarteListViewModel
        {
            Cartes = cartes,
            Recherche = recherche,
            Statut = statut,
            TypeCarte = typeCarte,
            Page = page,
            TaillePage = taille,
            Total = total
        };
    }

    public async Task<CarteDetailsViewModel?> GetDetailsAsync(int id)
    {
        var carte = await _context.Cartes
            .Include(c => c.Client)
            .Include(c => c.Compte)
            .Include(c => c.Transactions)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (carte is null) return null;

        DateTime debutJour = DateTime.UtcNow.Date;
        decimal totalJour = carte.Transactions
            .Where(t => t.Statut == "Validee" && t.DateTransaction >= debutJour)
            .Sum(t => t.Montant);

        return new CarteDetailsViewModel
        {
            Carte = carte,
            Transactions = carte.Transactions.OrderByDescending(t => t.DateTransaction).ToList(),
            TotalDepenseJour = totalJour
        };
    }

    public async Task<Carte?> GetByIdAsync(int id)
    {
        return await _context.Cartes
            .Include(c => c.Client)
            .Include(c => c.Compte)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<TransactionCarte>> GetHistoriqueTransactionsAsync(int carteId)
    {
        return await _context.TransactionsCarte
            .Where(t => t.CarteId == carteId)
            .OrderByDescending(t => t.DateTransaction)
            .ToListAsync();
    }

    private static string GenererNumeroCarte(string typeCarte)
    {
        string prefix = typeCarte switch
        {
            "Visa" => "4",
            "MasterCard" => "5",
            _ => "9"
        };

        var rng = RandomNumberGenerator.Create();
        byte[] bytes = new byte[8];
        rng.GetBytes(bytes);
        long randNum = Math.Abs(BitConverter.ToInt64(bytes, 0)) % 1_000_000_000_000_000L;
        string remainder = randNum.ToString("D15");
        return prefix + remainder;
    }

    private static string MaskNumero(string numero)
    {
        if (numero.Length < 4) return numero;
        return "**** **** **** " + numero[^4..];
    }

    private static string GenererReference(string prefix)
    {
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"{prefix}-{date}-{random}";
    }
}
