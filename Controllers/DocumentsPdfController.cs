using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Services;

namespace AkarangaBank.Web.Controllers;

[Authorize(Policy = "ClientAccountReaders")]
public class DocumentsPdfController : Controller
{
    private readonly IPdfService _pdfService;
    private readonly IComptabiliteService _comptabiliteService;
    private readonly AkarangaBankContext _context;

    public DocumentsPdfController(
        IPdfService pdfService,
        IComptabiliteService comptabiliteService,
        AkarangaBankContext context)
    {
        _pdfService = pdfService;
        _comptabiliteService = comptabiliteService;
        _context = context;
    }

    // GET: /DocumentsPdf/Releve/5
    public async Task<IActionResult> Releve(int id)
    {
        var releveVm = await _comptabiliteService.GetReleveAsync(id);
        if (releveVm is null) return NotFound("Compte introuvable.");
        if (!await PeutConsulterCompteAsync(releveVm.Compte)) return Forbid();

        var pdfBytes = _pdfService.GenererReleveComptePdf(releveVm);
        return File(pdfBytes, "application/pdf", $"Releve-{releveVm.Compte.NumeroCompte}.pdf");
    }

    // GET: /DocumentsPdf/Attestation/5
    public async Task<IActionResult> Attestation(int id, string? motif)
    {
        var compte = await _context.Comptes
            .Include(c => c.Client)
            .Include(c => c.Agence)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (compte is null) return NotFound("Compte introuvable.");
        if (!await PeutConsulterCompteAsync(compte)) return Forbid();

        var pdfBytes = _pdfService.GenererAttestationSoldePdf(compte, motif);
        return File(pdfBytes, "application/pdf", $"AttestationSolde-{compte.NumeroCompte}.pdf");
    }

    // GET: /DocumentsPdf/ContratPret/5
    [Authorize(Policy = "GestionnaireOrAdmin")]
    public async Task<IActionResult> ContratPret(int id)
    {
        var pret = await _context.Prets
            .Include(p => p.Client)
            .Include(p => p.CompteDebit)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pret is null) return NotFound("Prêt introuvable.");
        if (!User.IsInRole("admin"))
        {
            var userId = await CurrentUserIdAsync();
            var agenceId = await _context.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.AgenceId).SingleAsync();
            if (pret.Client.AgenceId != agenceId) return Forbid();
        }

        var pdfBytes = _pdfService.GenererContratPretPdf(pret);
        return File(pdfBytes, "application/pdf", $"ContratPret-{pret.Reference}.pdf");
    }

    // GET: /DocumentsPdf/DeclarationCnr/5
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeclarationCnr(int id)
    {
        var dos = await _context.DeclarationsSoupcon
            .Include(d => d.Client)
            .Include(d => d.Alerte)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (dos is null) return NotFound("Déclaration introuvable.");

        var pdfBytes = _pdfService.GenererDeclarationCnrPdf(dos);
        return File(pdfBytes, "application/pdf", $"Declaration-CNRF-{dos.Reference}.pdf");
    }

    // GET: /DocumentsPdf/Recu/5
    public async Task<IActionResult> Recu(int id)
    {
        var op = await _context.Operations
            .Include(o => o.Compte)
            .ThenInclude(c => c.Client)
            .Include(o => o.Agence)
            .Include(o => o.Utilisateur)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (op is null) return NotFound("Opération introuvable.");
        if (!await PeutConsulterCompteAsync(op.Compte)) return Forbid();

        var pdfBytes = _pdfService.GenererRecuOperationPdf(op);
        return File(pdfBytes, "application/pdf", $"Recu-{op.Reference}.pdf");
    }

    private async Task<int> CurrentUserIdAsync() => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

    private async Task<bool> PeutConsulterCompteAsync(AkarangaBank.Web.Models.Compte compte)
    {
        if (User.IsInRole("admin")) return true;
        var userId = await CurrentUserIdAsync();
        var user = await _context.Users.AsNoTracking().SingleAsync(u => u.Id == userId);
        return User.IsInRole("client") ? user.ClientId.HasValue && compte.ClientId == user.ClientId : compte.AgenceId == user.AgenceId;
    }
}
