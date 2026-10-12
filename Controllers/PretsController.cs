using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Services;
using AkarangaBank.Web.ViewModels;

namespace AkarangaBank.Web.Controllers;

[Authorize(Policy = "GestionnaireOrAdmin")]
public class PretsController : Controller
{
    private readonly IPretService _pretService;
    private readonly ICurrentUserService _currentUser;
    private readonly AkarangaBankContext _context;

    public PretsController(
        IPretService pretService,
        ICurrentUserService currentUser,
        AkarangaBankContext context)
    {
        _pretService = pretService;
        _currentUser = currentUser;
        _context = context;
    }

    // GET: /Prets
    public async Task<IActionResult> Index(string? recherche, string? statut, string? typePret, int page = 1)
    {
        const int taille = 10;
        int? agenceId = User.IsInRole("admin") ? null : await _currentUser.GetAgenceIdAsync();
        var vm = await _pretService.GetPagedAsync(recherche, statut, typePret, page, taille, agenceId);
        return View(vm);
    }

    // GET: /Prets/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var vm = await _pretService.GetDetailsAsync(id);
        if (vm is null) return NotFound();
        if (!User.IsInRole("admin"))
        {
            var userId = await _currentUser.GetUserIdAsync();
            var agenceId = await _context.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.AgenceId).SingleAsync();
            if (vm.Pret.Client.AgenceId != agenceId) return Forbid();
        }
        return View(vm);
    }

    // GET: /Prets/Demande
    [Authorize(Policy = "GuichetierOnly")]
    public async Task<IActionResult> Demande(int? clientId)
    {
        if (clientId.HasValue && !await ClientDansAgenceAsync(clientId.Value)) return Forbid();
        await ChargerClientsEtComptesAsync(clientId);
        var vm = new PretDemandeViewModel();
        if (clientId.HasValue)
        {
            vm.ClientId = clientId.Value;
        }
        vm.MensualiteEstimee = _pretService.CalculerMensualite(vm.Montant, vm.TauxIndicatif, vm.DureeMois);
        return View(vm);
    }

    // POST: /Prets/Demande
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "GuichetierOnly")]
    public async Task<IActionResult> Demande(PretDemandeViewModel vm)
    {
        if (!await ClientDansAgenceAsync(vm.ClientId)) return Forbid();
        if (!ModelState.IsValid)
        {
            await ChargerClientsEtComptesAsync(vm.ClientId, vm.CompteDebitId);
            return View(vm);
        }

        try
        {
            int userId = await _currentUser.GetUserIdAsync();
            var pret = await _pretService.CreerDemandeAsync(vm, userId);
            TempData["Success"] = $"Demande de prêt #{pret.Reference} créée avec succès. En attente de validation.";
            return RedirectToAction(nameof(Details), new { id = pret.Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await ChargerClientsEtComptesAsync(vm.ClientId, vm.CompteDebitId);
            return View(vm);
        }
    }

    // POST: /Prets/Valider/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "GestionnaireOrAdmin")]
    public async Task<IActionResult> Valider(int id, decimal? tauxPersonnalise)
    {
        if (!await PretDansAgenceAsync(id)) return Forbid();
        try
        {
            int userId = await _currentUser.GetUserIdAsync();
            var ok = await _pretService.ValiderPretAsync(id, tauxPersonnalise, userId);
            if (!ok) return NotFound();
            TempData["Success"] = "Prêt validé avec succès. Prêt pour déblocage.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: /Prets/Rejeter/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "GestionnaireOrAdmin")]
    public async Task<IActionResult> Rejeter(int id, string motif)
    {
        if (!await PretDansAgenceAsync(id)) return Forbid();
        if (string.IsNullOrWhiteSpace(motif))
        {
            TempData["Error"] = "Le motif de rejet est obligatoire.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            int userId = await _currentUser.GetUserIdAsync();
            var ok = await _pretService.RejeterPretAsync(id, motif, userId);
            if (!ok) return NotFound();
            TempData["Success"] = "Prêt rejeté.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: /Prets/Debloquer/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "GestionnaireOrAdmin")]
    public async Task<IActionResult> Debloquer(int id)
    {
        if (!await PretDansAgenceAsync(id)) return Forbid();
        try
        {
            int userId = await _currentUser.GetUserIdAsync();
            var op = await _pretService.DebloquerAsync(id, userId);
            TempData["Success"] = $"Fonds débloqués avec succès (Opération réf: {op.Reference}). L'échéancier a été généré.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: /Prets/RembourserEcheance/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "GuichetierOnly")]
    public async Task<IActionResult> RembourserEcheance(int echeanceId, int pretId)
    {
        if (!await PretDansAgenceAsync(pretId)) return Forbid();
        try
        {
            int userId = await _currentUser.GetUserIdAsync();
            var op = await _pretService.RembourserAsync(echeanceId, userId);
            TempData["Success"] = $"Échéance remboursée avec succès (Opération réf: {op.Reference}).";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id = pretId });
    }

    // POST: /Prets/ActualiserRetards
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ActualiserRetards()
    {
        int count = await _pretService.MettreAJourRetardsAsync();
        TempData["Success"] = $"Actualisation effectuée : {count} échéance(s) marquée(s) en retard avec calcul des pénalités.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ChargerClientsEtComptesAsync(int? selectedClientId = null, int? selectedCompteId = null)
    {
        var agenceId = await _currentUser.GetAgenceIdAsync();
        var clients = await _context.Clients
            .Where(c => c.Statut == "Actif" && (User.IsInRole("admin") || c.AgenceId == agenceId))
            .OrderBy(c => c.Nom)
            .Select(c => new { c.Id, Libelle = $"{c.NumeroClient} — {c.Nom} {c.Prenom}" })
            .ToListAsync();
        ViewBag.Clients = new SelectList(clients, "Id", "Libelle", selectedClientId);

        var comptesQuery = _context.Comptes
            .Include(c => c.Client)
            .Where(c => c.Statut == "Actif" && !c.EstCompteInterne && (User.IsInRole("admin") || c.AgenceId == agenceId));

        if (selectedClientId.HasValue)
        {
            comptesQuery = comptesQuery.Where(c => c.ClientId == selectedClientId.Value);
        }

        var comptes = await comptesQuery
            .OrderBy(c => c.NumeroCompte)
            .Select(c => new { c.Id, Libelle = $"{c.NumeroCompte} ({c.Solde:N2} {c.Devise})" })
            .ToListAsync();
        ViewBag.Comptes = new SelectList(comptes, "Id", "Libelle", selectedCompteId);
    }

    private async Task<bool> ClientDansAgenceAsync(int clientId)
    {
        if (User.IsInRole("admin")) return true;
        var agenceId = await _currentUser.GetAgenceIdAsync();
        return await _context.Clients.AnyAsync(c => c.Id == clientId && c.AgenceId == agenceId);
    }

    private async Task<bool> PretDansAgenceAsync(int pretId)
    {
        if (User.IsInRole("admin")) return await _context.Prets.AnyAsync(p => p.Id == pretId);
        var agenceId = await _currentUser.GetAgenceIdAsync();
        return await _context.Prets.AnyAsync(p => p.Id == pretId && p.Client.AgenceId == agenceId);
    }
}
