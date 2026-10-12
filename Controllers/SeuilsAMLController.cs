using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AkarangaBank.Web.Services;
namespace AkarangaBank.Web.Controllers;
[Authorize(Policy="AdminOnly")]
public class SeuilsAMLController : Controller
{
    private readonly ISeuilAMLService _service; private readonly ICurrentUserService _current;
    public SeuilsAMLController(ISeuilAMLService service,ICurrentUserService current){_service=service;_current=current;}
    public async Task<IActionResult> Index()=>View(await _service.GetAllAsync());
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Modifier(int id,decimal montant){try{if(!await _service.MettreAJourAsync(id,montant,await _current.GetUserIdAsync()))TempData["Error"]="Seuil introuvable.";else TempData["Success"]="Seuil mis à jour et modification auditée.";}catch(InvalidOperationException e){TempData["Error"]=e.Message;}return RedirectToAction(nameof(Index));}
}
