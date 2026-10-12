using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AkarangaBank.Web.Services;
namespace AkarangaBank.Web.Controllers;
[Authorize(Policy="AmlReaders")]
public class CNRFController : Controller
{
    private readonly ICNRFService _service; private readonly ICurrentUserService _current; private readonly IDeclarationService _alertes;
    public CNRFController(ICNRFService service,ICurrentUserService current,IDeclarationService alertes){_service=service;_current=current;_alertes=alertes;}
    public async Task<IActionResult> Index(string? statut){ViewBag.Statut=statut;var declarations=await _service.GetDeclarationsAsync(statut);var existing=declarations.Select(d=>d.AlerteId).ToHashSet();ViewBag.Alertes=(await _alertes.GetAlertesAsync("Ouverte",null)).Where(a=>!existing.Contains(a.Id)).ToList();return View(declarations);}
    [HttpPost,ValidateAntiForgeryToken,Authorize(Policy="AdminOnly")] public async Task<IActionResult> Generer(int alerteId){try{var d=await _service.GenererDOSAsync(alerteId,await _current.GetUserIdAsync());TempData["Success"]=$"DOS gÃ©nÃ©rÃ©e : {d.Reference}";}catch(InvalidOperationException e){TempData["Error"]=e.Message;}return RedirectToAction(nameof(Index));}
    [HttpPost,ValidateAntiForgeryToken,Authorize(Policy="AdminOnly")] public async Task<IActionResult> Transmettre(int id){if(!await _service.TransmettreAsync(id))TempData["Error"]="Cette dÃ©claration ne peut pas Ãªtre transmise.";else TempData["Success"]="DÃ©claration marquÃ©e comme transmise Ã  la CNRF.";return RedirectToAction(nameof(Index));}
    [HttpPost,ValidateAntiForgeryToken,Authorize(Policy="AdminOnly")] public async Task<IActionResult> Accuser(int id,string reponse){if(!await _service.EnregistrerAccuseAsync(id,reponse))TempData["Error"]="AccusÃ© non enregistrÃ©.";else TempData["Success"]="AccusÃ© de rÃ©ception enregistrÃ©.";return RedirectToAction(nameof(Index));}
    public async Task<IActionResult> Details(int id){var d=(await _service.GetDeclarationsAsync(null)).FirstOrDefault(x=>x.Id==id);return d is null?NotFound():View(d);}
}

