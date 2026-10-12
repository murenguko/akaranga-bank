using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Services;

namespace AkarangaBank.Web.Controllers;

[Authorize(Policy = "FinancialReports")]
public class ReportingController : Controller
{
    private readonly IReportService _reportService;
    private readonly IExcelService _excelService;
    private readonly ICsvService _csvService;
    private readonly AkarangaBankContext _context;

    public ReportingController(
        IReportService reportService,
        IExcelService excelService,
        ICsvService csvService,
        AkarangaBankContext context)
    {
        _reportService = reportService;
        _excelService = excelService;
        _csvService = csvService;
        _context = context;
    }

    // GET: /Reporting/Dashboard
    public async Task<IActionResult> Dashboard()
    {
        var vm = await _reportService.GetTableauDeBordAsync();
        return View(vm);
    }

    // GET: /Reporting/Balance
    public async Task<IActionResult> Balance()
    {
        var vm = await _reportService.GetBalanceGeneraleAsync();
        return View(vm);
    }

    // GET: /Reporting/CompteResultat
    public async Task<IActionResult> CompteResultat(DateTime? du, DateTime? au)
    {
        var vm = await _reportService.GetCompteDeResultatAsync(du, au);
        return View(vm);
    }

    // GET: /Reporting/Bilan
    public async Task<IActionResult> Bilan(DateTime? date)
    {
        var vm = await _reportService.GetBilanAsync(date);
        return View(vm);
    }

    // GET: /Reporting/RapportAML
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> RapportAML(int? mois, int? annee)
    {
        int m = mois ?? DateTime.UtcNow.Month;
        int y = annee ?? DateTime.UtcNow.Year;
        var vm = await _reportService.GetRapportAmlMensuelAsync(m, y);
        return View(vm);
    }

    // GET: /Reporting/ExportExcelJournal
    public async Task<IActionResult> ExportExcelJournal()
    {
        var ops = await _context.Operations
            .Include(o => o.Compte)
            .Include(o => o.Utilisateur)
            .OrderByDescending(o => o.DateOperation)
            .Take(1000)
            .ToListAsync();

        var bytes = _excelService.GenererJournalExcel(ops);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Journal-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    // GET: /Reporting/ExportExcelBalance
    public async Task<IActionResult> ExportExcelBalance()
    {
        var balance = await _reportService.GetBalanceGeneraleAsync();
        var bytes = _excelService.GenererBalanceExcel(balance.Lignes);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Balance-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    // GET: /Reporting/ExportExcelAlertes
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ExportExcelAlertes()
    {
        var alertes = await _context.AlertesAML
            .Include(a => a.Client)
            .Include(a => a.TraitePar)
            .OrderByDescending(a => a.DateAlerte)
            .ToListAsync();

        var bytes = _excelService.GenererAlertesAmlExcel(alertes);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"AlertesAML-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    // GET: /Reporting/ExportExcelPrets
    public async Task<IActionResult> ExportExcelPrets()
    {
        var prets = await _context.Prets
            .Include(p => p.Client)
            .Include(p => p.CompteDebit)
            .OrderByDescending(p => p.DateDemande)
            .ToListAsync();

        var bytes = _excelService.GenererPortefeuillePretsExcel(prets);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"PortefeuillePrets-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    // GET: /Reporting/ExportCsvJournal
    public async Task<IActionResult> ExportCsvJournal()
    {
        var ops = await _context.Operations
            .Include(o => o.Compte)
            .Include(o => o.Utilisateur)
            .OrderByDescending(o => o.DateOperation)
            .Take(1000)
            .ToListAsync();

        var bytes = _csvService.GenererJournalCsv(ops);
        return File(bytes, "text/csv; charset=utf-8", $"Journal-{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
