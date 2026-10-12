using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;
namespace AkarangaBank.Web.Services;
public class DetectionAvanceeService : IDetectionAvanceeService
{
    private readonly AkarangaBankContext _context; private readonly ISeuilAMLService _seuils;
    public DetectionAvanceeService(AkarangaBankContext context, ISeuilAMLService seuils) { _context = context; _seuils = seuils; }
    public async Task<ResultatSurveillance> DetecterStructuringAsync(int clientId, DateTime date)
    {
        var seuil = await _seuils.GetSeuilVigilanceAsync(); var start = date.AddHours(-24);
        var ops = await _context.Operations.AsNoTracking().Where(o => o.DateOperation >= start && o.DateOperation <= date && o.Statut == "Valide" && o.TypeOperation == "Depot" && o.Compte.ClientId == clientId && o.Montant >= seuil * .8m && o.Montant < seuil).ToListAsync();
        var alerts = ops.Count >= 3 ? new List<AlerteSurveillance> { new() { Regle = "Structuring", Description = $"{ops.Count} opérations proches du seuil AML en 24 heures.", Niveau = NiveauAlerte.Eleve } } : new();
        return Result(clientId, date, alerts);
    }
    public async Task<ResultatSurveillance> DetecterSmurfingAsync(int clientId, DateTime date)
    {
        var seuil = await _seuils.GetSeuilVigilanceAsync(); var start = date.AddHours(-24);
        var accountIds = await _context.Comptes.Where(c => c.ClientId == clientId && !c.EstCompteInterne).Select(c => c.Id).ToListAsync();
        if (accountIds.Count < 2) return Result(clientId, date, new());
        var total = await _context.Operations.AsNoTracking().Where(o => accountIds.Contains(o.CompteId) && o.DateOperation >= start && o.DateOperation <= date && o.Statut == "Valide" && o.TypeOperation == "Depot" && o.Montant < seuil).SumAsync(o => (decimal?)o.Montant) ?? 0m;
        var alerts = total >= seuil * 3 ? new List<AlerteSurveillance> { new() { Regle = "Smurfing", Description = $"{total:N0} BIF répartis sur plusieurs comptes en 24 heures.", Niveau = NiveauAlerte.Critique } } : new();
        return Result(clientId, date, alerts);
    }
    public async Task<ResultatSurveillance> DetecterLayeringAsync(int clientId, DateTime date)
    {
        var start = date.AddDays(-14);
        var count = await _context.VirementsInterAgences.AsNoTracking().Where(v => v.DateCreation >= start && v.DateCreation <= date && v.Virement.CompteSource.ClientId == clientId && v.Statut != "Rejete").CountAsync();
        var alerts = count >= 5 ? new List<AlerteSurveillance> { new() { Regle = "Layering", Description = $"{count} virements inter-agences en 14 jours, schéma possible de superposition.", Niveau = NiveauAlerte.Eleve } } : new();
        return Result(clientId, date, alerts);
    }
    public async Task<ResultatSurveillance> DetecterVelociteAnormaleAsync(int clientId, DateTime date)
    {
        var start = date.AddHours(-1);
        var count = await _context.Operations.AsNoTracking().Where(o => o.DateOperation >= start && o.DateOperation <= date && o.Compte.ClientId == clientId && o.Statut == "Valide").CountAsync();
        var alerts = count >= 10 ? new List<AlerteSurveillance> { new() { Regle = "VelociteAnormale", Description = $"{count} opérations en une heure.", Niveau = NiveauAlerte.Eleve } } : new();
        return Result(clientId, date, alerts);
    }
    private static ResultatSurveillance Result(int clientId, DateTime date, List<AlerteSurveillance> alerts) => new() { CompteId = clientId, DateAnalyse = date, Alertes = alerts, NecessiteDeclaration = alerts.Count > 0 };
}
