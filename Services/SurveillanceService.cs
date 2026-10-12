using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;
namespace AkarangaBank.Web.Services;
public class SurveillanceService : ISurveillanceService
{
    private readonly AkarangaBankContext _context; private readonly IDetectionAvanceeService _detection;
    public SurveillanceService(AkarangaBankContext context, IDetectionAvanceeService detection) { _context = context; _detection = detection; }
    public async Task<ResultatSurveillance> AnalyserTransactionAsync(int compteId, decimal montant, string typeOperation, DateTime date)
    {
        var compte = await _context.Comptes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == compteId) ?? throw new InvalidOperationException("Compte introuvable.");
        var results = new List<ResultatSurveillance>();
        if (compte.ClientId.HasValue)
        {
            results.Add(await _detection.DetecterStructuringAsync(compte.ClientId.Value, date));
            results.Add(await _detection.DetecterSmurfingAsync(compte.ClientId.Value, date));
            results.Add(await _detection.DetecterLayeringAsync(compte.ClientId.Value, date));
            results.Add(await _detection.DetecterVelociteAnormaleAsync(compte.ClientId.Value, date));
        }
        return new ResultatSurveillance { CompteId = compteId, DateAnalyse = date, Alertes = results.SelectMany(x => x.Alertes).ToList(), NecessiteDeclaration = results.Any(x => x.NecessiteDeclaration) };
    }
}
