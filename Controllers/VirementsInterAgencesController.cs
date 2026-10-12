using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Services;
using AkarangaBank.Web.ViewModels;
namespace AkarangaBank.Web.Controllers;
[Authorize(Policy="GuichetierOnly")]
public class VirementsInterAgencesController : Controller
{
    private readonly IVirementInterAgenceService _service; private readonly ICurrentUserService _current; private readonly AkarangaBankContext _context;
    public VirementsInterAgencesController(IVirementInterAgenceService service, ICurrentUserService current, AkarangaBankContext context) { _service=service; _current=current; _context=context; }
    public async Task<IActionResult> Index()
    {
        var agenceId = User.IsInRole("admin") ? (int?)null : await _current.GetAgenceIdAsync();
        return View(await _service.GetEnAttenteAsync(agenceId));
    }
    public async Task<IActionResult> Initier() { await ChargerComptesAsync(); return View(new VirementInterAgenceViewModel()); }
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Initier(VirementInterAgenceViewModel vm) { if(!ModelState.IsValid){await ChargerComptesAsync(vm.CompteSourceId,vm.CompteDestinationId);return View(vm);} try {var v=await _service.InitierAsync(vm,await _current.GetUserIdAsync());TempData["Success"]=$"Virement initié : {v.Reference}";return RedirectToAction(nameof(Index));}catch(InvalidOperationException e){ModelState.AddModelError("",e.Message);await ChargerComptesAsync(vm.CompteSourceId,vm.CompteDestinationId);return View(vm);} }
    [HttpPost,ValidateAntiForgeryToken,Authorize(Policy="AdminOnly")] public async Task<IActionResult> Valider(int id) { try {if(!await _service.ValiderAsync(id,await _current.GetUserIdAsync()))TempData["Error"]="Virement introuvable ou déjà traité.";else TempData["Success"]="Virement validé et mis en compensation.";}catch(InvalidOperationException e){TempData["Error"]=e.Message;}return RedirectToAction(nameof(Index)); }
    [HttpPost,ValidateAntiForgeryToken,Authorize(Policy="AdminOnly")] public async Task<IActionResult> Rejeter(int id,string motif) { await _service.RejeterAsync(id,await _current.GetUserIdAsync(),motif);return RedirectToAction(nameof(Index)); }
    public async Task<IActionResult> Historique(DateTime? du,DateTime? au) {ViewBag.Du=du;ViewBag.Au=au;return View(await _service.GetHistoriqueAsync(await _current.GetAgenceIdAsync(),du,au));}
    private async Task ChargerComptesAsync(int? source=null,int? destination=null) {var agency=await _current.GetAgenceIdAsync();var accounts=_context.Comptes.AsNoTracking().Include(c=>c.Client).Include(c=>c.Agence).Where(c=>!c.EstCompteInterne&&c.Statut=="Actif").OrderBy(c=>c.NumeroCompte);var sources=await accounts.Where(c=>c.AgenceId==agency).Select(c=>new {c.Id,Libelle=c.NumeroCompte+" — "+c.Client!.Nom+" "+c.Client.Prenom+" ("+c.Agence.Code+")"}).ToListAsync();var destinations=await accounts.Where(c=>c.AgenceId!=agency).Select(c=>new {c.Id,Libelle=c.NumeroCompte+" — "+c.Client!.Nom+" "+c.Client.Prenom+" ("+c.Agence.Code+")"}).ToListAsync();ViewBag.ComptesSource=new SelectList(sources,"Id","Libelle",source);ViewBag.ComptesDestination=new SelectList(destinations,"Id","Libelle",destination);}
}
