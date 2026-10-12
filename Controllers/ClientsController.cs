using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.Controllers;

[Authorize(Policy = "GestionnaireOrAdmin")]
public class ClientsController : Controller
{
    private readonly AkarangaBankContext _context;

    public ClientsController(AkarangaBankContext context)
    {
        _context = context;
    }

    // GET: /Clients
    public async Task<IActionResult> Index(string? recherche, int? agenceId, string? statut, int page = 1)
    {
        const int taille = 10;
        var query = _context.Clients
            .Include(c => c.Agence)
            .AsNoTracking()
            .AsQueryable();

        if (!User.IsInRole("admin"))
        {
            var agenceIdUtilisateur = await _context.Users.AsNoTracking()
                .Where(u => u.Id == int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value))
                .Select(u => u.AgenceId).SingleAsync();
            query = query.Where(c => c.AgenceId == agenceIdUtilisateur);
            agenceId = agenceIdUtilisateur;
        }

        if (!string.IsNullOrWhiteSpace(recherche))
        {
            var r = recherche.Trim();
            query = query.Where(c => c.NumeroClient.Contains(r) || c.Nom.Contains(r) || c.Prenom.Contains(r) || c.NumeroCNI.Contains(r));
        }

        if (agenceId.HasValue)
            query = query.Where(c => c.AgenceId == agenceId.Value);

        if (!string.IsNullOrWhiteSpace(statut))
            query = query.Where(c => c.Statut == statut);

        var total = await query.CountAsync();
        var clients = await query
            .OrderBy(c => c.Nom)
            .ThenBy(c => c.Prenom)
            .Skip((page - 1) * taille)
            .Take(taille)
            .ToListAsync();

        ViewBag.Agences = await _context.Agences.OrderBy(a => a.Ville).ToListAsync();
        ViewBag.Recherche = recherche;
        ViewBag.AgenceId = agenceId;
        ViewBag.Statut = statut;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / taille);
        ViewBag.Total = total;

        return View(clients);
    }

    // GET: /Clients/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var client = await _context.Clients
            .Include(c => c.Agence)
            .Include(c => c.Comptes)
            .ThenInclude(cp => cp.TypeCompte)
            .Include(c => c.VerificationsKYC)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (client is null) return NotFound();
        if (!User.IsInRole("admin"))
        {
            var agenceIdUtilisateur = await _context.Users.AsNoTracking()
                .Where(u => u.Id == int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value))
                .Select(u => u.AgenceId).SingleAsync();
            if (client.AgenceId != agenceIdUtilisateur) return Forbid();
        }

        return View(client);
    }

    // GET: /Clients/Create
    [Authorize(Policy = "GestionnaireOrAdmin")]
    public async Task<IActionResult> Create()
    {
        var agenceId = await GetAgenceIdAsync();
        var agences = User.IsInRole("admin") ? _context.Agences.AsNoTracking() : _context.Agences.AsNoTracking().Where(a => a.Id == agenceId);
        ViewBag.Agences = new SelectList(await agences.OrderBy(a => a.Nom).ToListAsync(), "Id", "Nom");
        return View(new Client());
    }

    // POST: /Clients/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "GestionnaireOrAdmin")]
    public async Task<IActionResult> Create(Client client)
    {
        if (!User.IsInRole("admin")) client.AgenceId = await GetAgenceIdAsync();
        if (client.AgenceId <= 0 || !await _context.Agences.AnyAsync(a => a.Id == client.AgenceId))
        {
            ModelState.AddModelError(nameof(client.AgenceId), "Veuillez sélectionner une agence existante.");
        }

        if (await _context.Clients.AnyAsync(c => c.NumeroClient == client.NumeroClient))
        {
            ModelState.AddModelError("NumeroClient", "Ce numéro client est déjà attribué.");
        }
        if (await _context.Clients.AnyAsync(c => c.NumeroCNI == client.NumeroCNI))
        {
            ModelState.AddModelError("NumeroCNI", "Ce numéro de CNI existe déjà.");
        }

        if (ModelState.IsValid)
        {
            client.DateCreation = DateTime.UtcNow;
            _context.Clients.Add(client);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Client créé avec succès.";
            return RedirectToAction(nameof(Details), new { id = client.Id });
        }

        var agences = User.IsInRole("admin") ? _context.Agences.AsNoTracking() : _context.Agences.AsNoTracking().Where(a => a.Id == client.AgenceId);
        ViewBag.Agences = new SelectList(await agences.OrderBy(a => a.Nom).ToListAsync(), "Id", "Nom", client.AgenceId);
        return View(client);
    }

    [Authorize(Policy = "RequireMfa")]
    public async Task<IActionResult> Delete(int id)
    {
        var client = await _context.Clients
            .Include(c => c.Agence)
            .Include(c => c.Comptes)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
        if (client is null) return NotFound();
        if (!User.IsInRole("admin") && client.AgenceId != await GetAgenceIdAsync()) return Forbid();
        return View(client);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "RequireMfa")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var client = await _context.Clients
            .Include(c => c.VerificationsKYC)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (client is null) return NotFound();
        if (!User.IsInRole("admin") && client.AgenceId != await GetAgenceIdAsync()) return Forbid();

        if (await _context.Comptes.AnyAsync(c => c.ClientId == id) ||
            await _context.Users.AnyAsync(u => u.ClientId == id))
        {
            TempData["Error"] = "Le client possède encore un compte bancaire ou un accès utilisateur et ne peut pas être supprimé.";
            return RedirectToAction(nameof(Details), new { id });
        }

        _context.VerificationsKYC.RemoveRange(client.VerificationsKYC);
        _context.Clients.Remove(client);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Client supprimé.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<int> GetAgenceIdAsync() => await _context.Users.AsNoTracking()
        .Where(u => u.Id == int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value))
        .Select(u => u.AgenceId).SingleAsync();
}
