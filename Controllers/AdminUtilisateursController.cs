using Microsoft.AspNetCore.Authorization; 
using Microsoft.AspNetCore.Mvc; 
using Microsoft.AspNetCore.Mvc.Rendering; 
using AkarangaBank.Web.Services; 
using AkarangaBank.Web.ViewModels; 
using AkarangaBank.Web.Data;
using Microsoft.EntityFrameworkCore;
 
namespace AkarangaBank.Web.Controllers; 
 
[Authorize(Policy = "AdminOnly")] 
public class AdminUtilisateursController : Controller { 
    private readonly IUtilisateurService _utilisateurService; 
    private readonly IAgenceService _agenceService; 
    private readonly AkarangaBankContext _context;
 
    public AdminUtilisateursController( 
        IUtilisateurService utilisateurService, 
        IAgenceService agenceService,
        AkarangaBankContext context) 
    { 
        _utilisateurService = utilisateurService; 
        _agenceService = agenceService; 
        _context = context;
    } 
 
    public async Task<IActionResult> Index() 
    { 
        var users = await _utilisateurService.GetAllAsync(); 
        ViewBag.Clients = await _context.Clients.AsNoTracking().Where(c => c.Statut == "Actif")
            .OrderBy(c => c.Nom).ThenBy(c => c.Prenom).ToListAsync();
        return View(users); 
    } 

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssocierClient(int userId, int? clientId)
    {
        var (success, error) = await _utilisateurService.AssocierClientAsync(userId, clientId);
        TempData[success ? "Success" : "Error"] = success ? "Association client mise à jour." : error;
        return RedirectToAction(nameof(Index));
    }
 
    public async Task<IActionResult> Create() 
    { 
        await ChargerAgencesAsync(); 
        return View(new UtilisateurCreateViewModel()); 
    } 
 
    [HttpPost] 
    [ValidateAntiForgeryToken] 
    public async Task<IActionResult> Create(UtilisateurCreateViewModel vm) 
    { 
        if (!ModelState.IsValid) 
        { 
            await ChargerAgencesAsync(vm.AgenceId, vm.ClientId);
            return View(vm); 
        } 

 
 
        var (success, error) = await _utilisateurService.CreateAsync(vm); 
        if (!success) 
        { 
            ModelState.AddModelError("", error ?? "Erreur inconnue."); 
            await ChargerAgencesAsync(vm.AgenceId, vm.ClientId);
            return View(vm); 
        } 
 
        TempData["Success"] = "Utilisateur créé avec succès."; 
        return RedirectToAction(nameof(Index)); 
    } 
 
    [HttpPost] 
    [ValidateAntiForgeryToken] 
    public async Task<IActionResult> ToggleActif(int id) 
    { 
        await _utilisateurService.ToggleActifAsync(id); 
        TempData["Success"] = "Statut modifié."; 
        return RedirectToAction(nameof(Index)); 
    } 
 
    private async Task ChargerAgencesAsync(int? selectedId = null, int? selectedClientId = null) 
    { 
        var agencesVm = await _agenceService.GetPagedAsync(null, 1, 
int.MaxValue); 
        ViewBag.Agences = new SelectList( 
            agencesVm.Agences.Select(a => new { a.Id, Libelle = $"{a.Code} — {a.Ville}" }), "Id", "Libelle", selectedId); 
 
        ViewBag.Roles = new SelectList(new[] 
        { 
            "admin", "gestionnaire", "auditeur", "guichetier", "client" 
        }); 
        var clients = await _context.Clients.AsNoTracking().Where(c => c.Statut == "Actif")
            .OrderBy(c => c.Nom).ThenBy(c => c.Prenom)
            .Select(c => new { c.Id, Nom = c.Nom + " " + c.Prenom + " — " + c.NumeroClient })
            .ToListAsync();
        ViewBag.Clients = new SelectList(clients, "Id", "Nom", selectedClientId);
    } 
}
