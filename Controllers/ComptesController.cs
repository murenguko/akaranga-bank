using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.Controllers;

[Authorize(Policy = "ClientAccountReaders")]
public class ComptesController : Controller
{
    private readonly AkarangaBankContext _context;

    public ComptesController(AkarangaBankContext context)
    {
        _context = context;
    }

    // GET: /Comptes
    public async Task<IActionResult> Index(string? recherche, int? agenceId, string? statut, int page = 1)
    {
        const int taille = 10;
        var query = _context.Comptes
            .Include(c => c.Client)
            .Include(c => c.TypeCompte)
            .Include(c => c.Agence)
            .Where(c => !c.EstCompteInterne)
            .AsNoTracking()
            .AsQueryable();

        if (User.IsInRole("client"))
        {
            var clientIdUtilisateur = await GetClientIdAsync();
            if (!clientIdUtilisateur.HasValue)
            {
                ViewBag.RequiresClientLink = true;
                return View(new List<Compte>());
            }
            query = query.Where(c => c.ClientId == clientIdUtilisateur.Value);
            agenceId = null;
        }
        else if (!User.IsInRole("admin"))
        {
            var agenceIdUtilisateur = await GetAgenceIdAsync();
            query = query.Where(c => c.AgenceId == agenceIdUtilisateur);
            agenceId = agenceIdUtilisateur;
        }

        if (!string.IsNullOrWhiteSpace(recherche))
        {
            var r = recherche.Trim();
            query = query.Where(c => c.NumeroCompte.Contains(r) ||
                                     (c.Client != null && (c.Client.Nom.Contains(r) || c.Client.Prenom.Contains(r))));
        }

        if (agenceId.HasValue)
            query = query.Where(c => c.AgenceId == agenceId.Value);

        if (!string.IsNullOrWhiteSpace(statut))
            query = query.Where(c => c.Statut == statut);

        var total = await query.CountAsync();
        var comptes = await query
            .OrderBy(c => c.NumeroCompte)
            .Skip((page - 1) * taille)
            .Take(taille)
            .ToListAsync();

        var agencesQuery = _context.Agences.AsNoTracking();
        if (!User.IsInRole("admin"))
        {
            var agenceIdUtilisateur = await GetAgenceIdAsync();
            agencesQuery = agencesQuery.Where(a => a.Id == agenceIdUtilisateur);
        }
        ViewBag.Agences = await agencesQuery.OrderBy(a => a.Ville).ToListAsync();
        ViewBag.Recherche = recherche;
        ViewBag.AgenceId = agenceId;
        ViewBag.Statut = statut;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / taille);
        ViewBag.Total = total;

        return View(comptes);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "client")]
    public async Task<IActionResult> LierMonDossier(string numeroClient, string numeroCni)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var user = await _context.Users.SingleAsync(u => u.Id == userId);
        var email = user.Email?.Trim().ToLower();
        var numeroClientNormalise = numeroClient?.Trim();
        var numeroCniNormalise = numeroCni?.Trim();

        var client = await _context.Clients.AsNoTracking().FirstOrDefaultAsync(c =>
            c.AgenceId == user.AgenceId &&
            c.NumeroClient == numeroClientNormalise &&
            c.NumeroCNI == numeroCniNormalise &&
            c.Email != null && c.Email.ToLower() == email);

        if (client is null)
        {
            TempData["Error"] = "Impossible de vérifier ces informations. Vérifiez vos références ou contactez votre agence.";
            return RedirectToAction(nameof(Index));
        }

        user.ClientId = client.Id;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Votre dossier client est maintenant associé à votre espace.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Comptes/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var compte = await _context.Comptes
            .Include(c => c.Client)
            .Include(c => c.TypeCompte)
            .Include(c => c.Agence)
            .Include(c => c.Cartes)
            .Include(c => c.Ecritures.OrderByDescending(e => e.DateEcriture).Take(50))
            .ThenInclude(e => e.Operation)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (compte is null) return NotFound();
        if (User.IsInRole("client"))
        {
            var clientIdUtilisateur = await GetClientIdAsync();
            if (!clientIdUtilisateur.HasValue || compte.ClientId != clientIdUtilisateur.Value) return Forbid();
        }
        else if (!User.IsInRole("admin") && compte.AgenceId != await GetAgenceIdAsync())
        {
            return Forbid();
        }

        return View(compte);
    }

    // GET: /Comptes/Create
    [Authorize(Policy = "GestionnaireOrAdmin")]
    public async Task<IActionResult> Create(int? clientId)
    {
        var agenceId = User.IsInRole("admin") ? (int?)null : await GetAgenceIdAsync();
        var clientsQuery = _context.Clients.Where(c => c.Statut == "Actif");
        if (agenceId.HasValue) clientsQuery = clientsQuery.Where(c => c.AgenceId == agenceId.Value);
        ViewBag.Clients = new SelectList(await clientsQuery.OrderBy(c => c.Nom).ToListAsync(), "Id", "Nom", clientId);
        ViewBag.TypesCompte = new SelectList(await _context.TypesCompte.OrderBy(t => t.Libelle).ToListAsync(), "Id", "Libelle");
        var agences = agenceId.HasValue ? _context.Agences.Where(a => a.Id == agenceId.Value) : _context.Agences;
        ViewBag.Agences = new SelectList(await agences.ToListAsync(), "Id", "Nom");

        var compte = new Compte();
        if (clientId.HasValue) compte.ClientId = clientId.Value;
        return View(compte);
    }

    // POST: /Comptes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "GestionnaireOrAdmin")]
    public async Task<IActionResult> Create(Compte compte)
    {
        if (!User.IsInRole("admin")) compte.AgenceId = await GetAgenceIdAsync();
        if (!await _context.Clients.AnyAsync(c => c.Id == compte.ClientId && c.AgenceId == compte.AgenceId && c.Statut == "Actif"))
            ModelState.AddModelError(nameof(compte.ClientId), "Le client doit être actif et rattaché à votre agence.");
        if (!compte.TypeCompteId.HasValue || !await _context.TypesCompte.AnyAsync(t => t.Id == compte.TypeCompteId.Value))
            ModelState.AddModelError(nameof(compte.TypeCompteId), "Veuillez sélectionner un type de compte valide.");
        if (!await _context.Agences.AnyAsync(a => a.Id == compte.AgenceId))
            ModelState.AddModelError(nameof(compte.AgenceId), "Veuillez sélectionner une agence valide.");
        if (await _context.Comptes.AnyAsync(c => c.NumeroCompte == compte.NumeroCompte))
        {
            ModelState.AddModelError("NumeroCompte", "Ce numéro de compte est déjà utilisé.");
        }

        if (ModelState.IsValid)
        {
            compte.DateOuverture = DateTime.UtcNow;
            compte.DateCreation = DateTime.UtcNow;
            compte.Statut = "Actif";
            _context.Comptes.Add(compte);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Compte bancaire ouvert avec succès.";
            return RedirectToAction(nameof(Details), new { id = compte.Id });
        }

        var clientsQuery = _context.Clients.Where(c => c.Statut == "Actif" && c.AgenceId == compte.AgenceId);
        ViewBag.Clients = new SelectList(await clientsQuery.OrderBy(c => c.Nom).ToListAsync(), "Id", "Nom", compte.ClientId);
        ViewBag.TypesCompte = new SelectList(await _context.TypesCompte.OrderBy(t => t.Libelle).ToListAsync(), "Id", "Libelle", compte.TypeCompteId);
        var agences = User.IsInRole("admin") ? _context.Agences.AsQueryable() : _context.Agences.Where(a => a.Id == compte.AgenceId);
        ViewBag.Agences = new SelectList(await agences.ToListAsync(), "Id", "Nom", compte.AgenceId);
        return View(compte);
    }

    [Authorize(Policy = "GestionnaireOrAdmin")]
    [Authorize(Policy = "RequireMfa")]
    public async Task<IActionResult> Delete(int id)
    {
        var compte = await _context.Comptes
            .Include(c => c.Client)
            .Include(c => c.Agence)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && !c.EstCompteInterne);
        if (compte is null) return NotFound();
        if (!User.IsInRole("admin") && compte.AgenceId != await GetAgenceIdAsync()) return Forbid();
        return View(compte);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "GestionnaireOrAdmin")]
    [Authorize(Policy = "RequireMfa")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var compte = await _context.Comptes.FirstOrDefaultAsync(c => c.Id == id && !c.EstCompteInterne);
        if (compte is null) return NotFound();
        if (!User.IsInRole("admin") && compte.AgenceId != await GetAgenceIdAsync()) return Forbid();

        var hasFinancialHistory = await _context.Ecritures.AnyAsync(e => e.CompteId == id) ||
            await _context.Virements.AnyAsync(v => v.CompteSourceId == id || v.CompteDestinationId == id) ||
            await _context.Prets.AnyAsync(p => p.CompteDebitId == id) ||
            await _context.Cartes.AnyAsync(c => c.CompteId == id);
        if (compte.Solde != 0m || hasFinancialHistory)
        {
            TempData["Error"] = "Un compte avec un solde ou un historique financier doit être conservé.";
            return RedirectToAction(nameof(Details), new { id });
        }

        _context.Comptes.Remove(compte);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Compte supprimé.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<int> GetAgenceIdAsync() => await _context.Users.AsNoTracking()
        .Where(u => u.Id == int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value))
        .Select(u => u.AgenceId).SingleAsync();

    private async Task<int?> GetClientIdAsync() => await _context.Users.AsNoTracking()
        .Where(u => u.Id == int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value))
        .Select(u => u.ClientId).SingleAsync();
}
