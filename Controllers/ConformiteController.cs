using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AkarangaBank.Web.Services;
namespace AkarangaBank.Web.Controllers;
[Authorize(Policy="AmlReaders")]
public class ConformiteController : Controller
{
    private readonly IDeclarationService _declaration; private readonly ICurrentUserService _current; private readonly IConformiteService _conformite;
    public ConformiteController(IDeclarationService declaration, ICurrentUserService current, IConformiteService conformite) { _declaration=declaration; _current=current; _conformite=conformite; }
    public async Task<IActionResult> Alertes(string? statut,string? niveau) { ViewBag.Statut=statut; ViewBag.Niveau=niveau; return View(await _declaration.GetAlertesAsync(statut,niveau)); }
    public async Task<IActionResult> Details(int id) { var a=await _declaration.GetAlerteAsync(id); return a is null?NotFound():View(a); }
    [HttpPost,ValidateAntiForgeryToken,Authorize(Policy="AdminOnly")] public async Task<IActionResult> Traiter(int id,string statut,string? commentaire) { await _declaration.TraiterAlerteAsync(id,await _current.GetUserIdAsync(),statut,commentaire); return RedirectToAction(nameof(Alertes)); }
    [Authorize(Policy="AdminOnly")] public async Task<IActionResult> RisqueClient(int id) { ViewBag.Risque=await _conformite.EvaluerRisqueClientAsync(id); ViewBag.ClientId=id; return View(); }
}


