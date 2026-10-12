using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Services;
namespace AkarangaBank.Web.Controllers;
[Authorize(Policy="AdminOnly")]
public class CompensationsController : Controller
{
    private readonly ICompensationService _service; private readonly AkarangaBankContext _context; private readonly ICurrentUserService _current;
    public CompensationsController(ICompensationService service,AkarangaBankContext context,ICurrentUserService current){_service=service;_context=context;_current=current;}
    public async Task<IActionResult> Index(int? agenceId,DateTime? date){ViewBag.Agences=new SelectList(await _context.Agences.AsNoTracking().OrderBy(a=>a.Code).ToListAsync(),"Id","Code",agenceId);ViewBag.AgenceId=agenceId;ViewBag.Date=date?.ToString("yyyy-MM-dd");ViewBag.Equilibre=await _service.VerifierEquilibreCompensationsAsync(date??DateTime.UtcNow);return View(await _service.GetCompensationsAsync(agenceId,date));}
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Calculer(int agenceId,DateTime date){try{await _service.CalculerCompensationAsync(agenceId,date);TempData["Success"]="Compensation calculée.";}catch(InvalidOperationException e){TempData["Error"]=e.Message;}return RedirectToAction(nameof(Index),new{agenceId,date});}
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Regler(int id){try{if(!await _service.ReglerCompensationAsync(id,await _current.GetUserIdAsync()))TempData["Error"]="Compensation introuvable.";else TempData["Success"]="Compensation réglée.";}catch(InvalidOperationException e){TempData["Error"]=e.Message;}return RedirectToAction(nameof(Index));}
}
