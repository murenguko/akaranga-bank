using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using AkarangaBank.Web.Services;
using AkarangaBank.Web.ViewModels;


namespace AkarangaBank.Web.Controllers;

public class AgencesController : Controller
{
 private readonly IAgenceService _agenceService;
 private readonly ICurrentUserService _currentUser;
 public AgencesController(IAgenceService agenceService, ICurrentUserService currentUser)
   {
      _agenceService = agenceService;
      _currentUser = currentUser;
   }
 public async Task<IActionResult> Index(string? recherche, int page=1)
 {
   const int taille = 10;
   var vm = await _agenceService.GetPagedAsync(recherche,page,taille);
   return View(vm);
 }
 [Microsoft.AspNetCore.Authorization.Authorize(Policy = "BankingStaff")]
 public async Task<IActionResult> Details(int id)
   {
      if (!User.IsInRole("admin"))
      {
         var agenceUtilisateur = await _currentUser.GetAgenceIdAsync();
         if (id != agenceUtilisateur) return Forbid();
      }
      var vm= await _agenceService.GetDetailsAsync(id);
      if (vm is null) return NotFound();
      return View(vm);
   }

[Microsoft.AspNetCore.Authorization.Authorize(Policy = "AdminOnly")]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = "RequireMfa")]
 public IActionResult Create()
   {
      return View(new AgenceCreateViewModel());
   }


[HttpPost]
[ValidateAntiForgeryToken]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = "AdminOnly")]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = "RequireMfa")]

public async Task<IActionResult> Create(AgenceCreateViewModel vm)
   {
      if (!ModelState.IsValid) return View(vm);

      // VÃ©rification d'unicitÃ© du code

      if (await _agenceService.CodeExisteAsync(vm.Code.Trim().ToUpperInvariant()))
      {
         ModelState.AddModelError(nameof(vm.Code), "Ce code d'agence est dÃ©jÃ  utilisÃ©. ");
         return View(vm);
      }

      try
      {
         await _agenceService.CreateAsync(vm);
         TempData["Success"] = "Agence crÃ©Ã©e avec succÃ¨s.";
         return RedirectToAction(nameof(Index));

      }
      catch (DbUpdateException ex) when (ex.InnerException is PostgresException postgres && postgres.SqlState == PostgresErrorCodes.UniqueViolation){
         // Contrainte UNIQUE violÃ©e cÃ´tÃ© base
         ModelState.AddModelError(nameof(vm.Code), "Ce code d'agence est dÃ©jÃ  utilisÃ©.");
         return View(vm);
      }

   }

// -------------------------------------------------------------
 // GET: /Agences/Edit/5
 // -------------------------------------------------------------
 [Microsoft.AspNetCore.Authorization.Authorize(Policy = "AdminOnly")]
 [Microsoft.AspNetCore.Authorization.Authorize(Policy = "RequireMfa")]
 public async Task<IActionResult> Edit(int id)
 {
 var agence = await _agenceService.GetByIdAsync(id);
 if (agence is null) return NotFound();
 var vm = new AgenceEditViewModel
 {
 Id = agence.Id,
 Code = agence.Code,
 Nom = agence.Nom,
 Ville = agence.Ville,
 EstSiege = agence.EstSiege
 };
 return View(vm);
 }


// -------------------------------------------------------------
 // POST: /Agences/Edit/5
 // -------------------------------------------------------------
 [HttpPost]
 [ValidateAntiForgeryToken]
 [Microsoft.AspNetCore.Authorization.Authorize(Policy = "AdminOnly")]
 [Microsoft.AspNetCore.Authorization.Authorize(Policy = "RequireMfa")]
 public async Task<IActionResult> Edit(int id, AgenceEditViewModel vm)
 {
      if (id != vm.Id) return BadRequest();
      if (!ModelState.IsValid) return View(vm);
      if (await _agenceService.CodeExisteAsync(vm.Code.Trim().ToUpperInvariant(), vm.Id))
      {
      ModelState.AddModelError(nameof(vm.Code),
      "Ce code d'agence est dÃ©jÃ  utilisÃ©.");
      return View(vm);
      }
      try
      {
         var ok = await _agenceService.UpdateAsync(vm);
         if (!ok) return NotFound();
         TempData["Success"] = "Agence modifiÃ©e avec succÃ¨s.";
         return RedirectToAction(nameof(Index));
      }
      catch (DbUpdateException ex)
      when (ex.InnerException is PostgresException postgres && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
      {
         ModelState.AddModelError(nameof(vm.Code),
         "Ce code d'agence est dÃ©jÃ  utilisÃ©.");
         return View(vm);
      }
 }

 // -------------------------------------------------------------
 // GET: /Agences/Delete/5
 // -------------------------------------------------------------
 [Microsoft.AspNetCore.Authorization.Authorize(Policy = "AdminOnly")]
 [Microsoft.AspNetCore.Authorization.Authorize(Policy = "RequireMfa")]
 public async Task<IActionResult> Delete(int id)
 {
 var agence = await _agenceService.GetByIdAsync(id);
 if (agence is null) return NotFound();
 return View(agence);
 }

 // -------------------------------------------------------------
 // POST: /Agences/Delete/5
 // -------------------------------------------------------------
 [HttpPost, ActionName("Delete")]
 [ValidateAntiForgeryToken]
 [Microsoft.AspNetCore.Authorization.Authorize(Policy = "AdminOnly")]
 [Microsoft.AspNetCore.Authorization.Authorize(Policy = "RequireMfa")]
 public async Task<IActionResult> DeleteConfirmed(int id)
 {
 try
 {
 var ok = await _agenceService.DeleteAsync(id);
 if (!ok) return NotFound();
 TempData["Success"] = "Agence supprimÃ©e avec succÃ¨s.";
 return RedirectToAction(nameof(Index));
 }
 catch (InvalidOperationException ex)
 {
 TempData["Error"] = ex.Message;
 return RedirectToAction(nameof(Index));
 }
 }
}


