using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Services;
using AkarangaBank.Web.ViewModels;

namespace AkarangaBank.Web.Controllers;

[Authorize(Policy = "AdminOnly")]
public class DocumentsController : Controller
{
    private readonly IDocumentService _documentService;
    private readonly ICurrentUserService _currentUser;
    private readonly AkarangaBankContext _context;

    public DocumentsController(
        IDocumentService documentService,
        ICurrentUserService currentUser,
        AkarangaBankContext context)
    {
        _documentService = documentService;
        _currentUser = currentUser;
        _context = context;
    }

    // GET: /Documents
    public async Task<IActionResult> Index(string? recherche, string? categorie, string? statut, int? clientId, int page = 1)
    {
        const int taille = 10;
        var vm = await _documentService.GetPagedAsync(recherche, categorie, statut, clientId, page, taille);
        ViewBag.ClientId = clientId;
        ViewBag.Clients = await _context.Clients.AsNoTracking().OrderBy(c => c.Nom).ThenBy(c => c.Prenom).ToListAsync();
        return View(vm);
    }

    // GET: /Documents/Upload
    [Authorize(Policy = "GuichetierOnly")]
    public async Task<IActionResult> Upload(int? clientId, int? compteId, int? pretId)
    {
        await ChargerSelectionsAsync(clientId, compteId, pretId);
        var vm = new DocumentUploadViewModel
        {
            ClientId = clientId,
            CompteId = compteId,
            PretId = pretId
        };
        return View(vm);
    }

    // POST: /Documents/Upload
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "GuichetierOnly")]
    public async Task<IActionResult> Upload(DocumentUploadViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await ChargerSelectionsAsync(vm.ClientId, vm.CompteId, vm.PretId);
            return View(vm);
        }

        try
        {
            int userId = await _currentUser.GetUserIdAsync();
            var doc = await _documentService.UploadAsync(vm, userId);
            TempData["Success"] = $"Document '{doc.Titre}' téléversé avec succès (SHA-256: {doc.HashSha256[..12]}...).";
            return RedirectToAction(nameof(Details), new { id = doc.Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await ChargerSelectionsAsync(vm.ClientId, vm.CompteId, vm.PretId);
            return View(vm);
        }
    }

    // GET: /Documents/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var vm = await _documentService.GetDetailsAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    // GET: /Documents/Telecharger/5
    public async Task<IActionResult> Telecharger(int id)
    {
        try
        {
            int userId = await _currentUser.GetUserIdAsync();
            string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            string? userAgent = Request.Headers["User-Agent"].ToString();

            var (bytes, contentType, nomFichier) = await _documentService.TelechargerAsync(id, userId, ip, userAgent);
            return File(bytes, contentType, nomFichier);
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: /Documents/Archiver/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archiver(int id)
    {
        int userId = await _currentUser.GetUserIdAsync();
        var ok = await _documentService.ArchiverAsync(id, userId);
        if (!ok) return NotFound();

        TempData["Success"] = "Document archivé.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: /Documents/Supprimer/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Supprimer(int id)
    {
        int userId = await _currentUser.GetUserIdAsync();
        var ok = await _documentService.SupprimerAsync(id, userId);
        if (!ok) return NotFound();

        TempData["Success"] = "Document placé en corbeille (suppression logique).";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Documents/Purger
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Purger(int jours = 30)
    {
        int count = await _documentService.PurgerDocumentsSupprimesAsync(jours);
        TempData["Success"] = $"{count} document(s) purgé(s) définitivement du disque.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ChargerSelectionsAsync(int? selectedClientId = null, int? selectedCompteId = null, int? selectedPretId = null)
    {
        var clients = await _context.Clients
            .OrderBy(c => c.Nom)
            .Select(c => new { c.Id, Libelle = $"{c.NumeroClient} — {c.Nom} {c.Prenom}" })
            .ToListAsync();
        ViewBag.Clients = new SelectList(clients, "Id", "Libelle", selectedClientId);

        var comptes = await _context.Comptes
            .Where(c => !c.EstCompteInterne)
            .OrderBy(c => c.NumeroCompte)
            .Select(c => new { c.Id, Libelle = c.NumeroCompte })
            .ToListAsync();
        ViewBag.Comptes = new SelectList(comptes, "Id", "Libelle", selectedCompteId);

        var prets = await _context.Prets
            .OrderByDescending(p => p.DateDemande)
            .Select(p => new { p.Id, Libelle = $"{p.Reference} ({p.MontantPrincipal:N0} BIF)" })
            .ToListAsync();
        ViewBag.Prets = new SelectList(prets, "Id", "Libelle", selectedPretId);
    }
}
