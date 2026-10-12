using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Services;
using AkarangaBank.Web.ViewModels;

namespace AkarangaBank.Web.Controllers;

[Authorize(Policy = "AdminOnly")]
public class CartesController : Controller
{
    private readonly ICarteService _carteService;
    private readonly ICurrentUserService _currentUser;
    private readonly AkarangaBankContext _context;

    public CartesController(
        ICarteService carteService,
        ICurrentUserService currentUser,
        AkarangaBankContext context)
    {
        _carteService = carteService;
        _currentUser = currentUser;
        _context = context;
    }

    // GET: /Cartes
    public async Task<IActionResult> Index(string? recherche, string? statut, string? typeCarte, int page = 1)
    {
        const int taille = 10;
        var vm = await _carteService.GetPagedAsync(recherche, statut, typeCarte, page, taille);
        return View(vm);
    }

    // GET: /Cartes/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var vm = await _carteService.GetDetailsAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    // GET: /Cartes/Emettre
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Emettre(int? compteId)
    {
        await ChargerComptesActifsAsync(compteId);
        var vm = new CarteEmissionViewModel();
        if (compteId.HasValue) vm.CompteId = compteId.Value;
        return View(vm);
    }

    // POST: /Cartes/Emettre
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Emettre(CarteEmissionViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await ChargerComptesActifsAsync(vm.CompteId);
            return View(vm);
        }

        try
        {
            int userId = await _currentUser.GetUserIdAsync();
            var carte = await _carteService.EmettreCarteAsync(vm, userId);
            TempData["Success"] = $"Carte bancaire {carte.TypeCarte} émise avec succès (N° masqué : {carte.NumeroMasque}).";
            return RedirectToAction(nameof(Details), new { id = carte.Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await ChargerComptesActifsAsync(vm.CompteId);
            return View(vm);
        }
    }

    // POST: /Cartes/Bloquer/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "GestionnaireOrAdmin")]
    public async Task<IActionResult> Bloquer(CarteActionViewModel vm)
    {
        try
        {
            int userId = await _currentUser.GetUserIdAsync();
            var ok = await _carteService.BloquerCarteAsync(vm, userId);
            if (!ok) return NotFound();
            TempData["Success"] = $"Carte mise à jour : Statut={vm.NouveauStatut}, Motif={vm.Motif}.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id = vm.CarteId });
    }

    // POST: /Cartes/Renouveler/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Renouveler(int id)
    {
        try
        {
            int userId = await _currentUser.GetUserIdAsync();
            var nouvelleCarte = await _carteService.RenouvelerCarteAsync(id, userId);
            TempData["Success"] = $"Carte renouvelée avec succès. Nouvelle carte : {nouvelleCarte.NumeroMasque} (Expiration : {nouvelleCarte.DateExpiration:MM/yyyy}).";
            return RedirectToAction(nameof(Details), new { id = nouvelleCarte.Id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    // GET: /Cartes/Transaction?carteId=5
    public async Task<IActionResult> Transaction(int? carteId)
    {
        var vm = new CarteTransactionViewModel();
        if (carteId.HasValue) vm.CarteId = carteId.Value;
        await ChargerCartesActivesAsync(carteId);
        return View(vm);
    }

    // POST: /Cartes/Transaction
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "GuichetierOnly")]
    public async Task<IActionResult> Transaction(CarteTransactionViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await ChargerCartesActivesAsync(vm.CarteId);
            return View(vm);
        }

        try
        {
            var res = await _carteService.AutoriserTransactionAsync(vm, await _currentUser.GetUserIdAsync());
            if (res.Reussi)
            {
                TempData["Success"] = $"Transaction autorisée ({res.Transaction.TypeTransaction} de {res.Transaction.Montant:N2} BIF).";
            }
            else
            {
                TempData["Error"] = $"Transaction refusée : {res.Message}";
            }
            return RedirectToAction(nameof(Details), new { id = vm.CarteId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await ChargerCartesActivesAsync(vm.CarteId);
            return View(vm);
        }
    }

    private async Task ChargerComptesActifsAsync(int? selectedCompteId = null)
    {
        var comptes = await _context.Comptes
            .Include(c => c.Client)
            .Where(c => c.Statut == "Actif" && !c.EstCompteInterne)
            .OrderBy(c => c.NumeroCompte)
            .Select(c => new
            {
                c.Id,
                Libelle = $"{c.NumeroCompte} — {(c.Client != null ? (c.Client.Nom + " " + c.Client.Prenom) : "Sans Client")} ({c.Solde:N2} {c.Devise})"
            })
            .ToListAsync();
        ViewBag.Comptes = new SelectList(comptes, "Id", "Libelle", selectedCompteId);
    }

    private async Task ChargerCartesActivesAsync(int? selectedCarteId = null)
    {
        var cartes = await _context.Cartes
            .Include(c => c.Client)
            .Include(c => c.Compte)
            .Where(c => c.Statut == "Active" && c.DateExpiration > DateTime.UtcNow && c.Compte.Statut == "Actif")
            .OrderBy(c => c.NumeroCarte)
            .Select(c => new
            {
                c.Id,
                Libelle = $"{c.NumeroMasque} ({c.TypeCarte}) — {c.Client.Nom} {c.Client.Prenom} — {c.Statut}"
            })
            .ToListAsync();
        ViewBag.Cartes = new SelectList(cartes, "Id", "Libelle", selectedCarteId);
    }
}
