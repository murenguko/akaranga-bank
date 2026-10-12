using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;
namespace AkarangaBank.Web.Services;
public class ConformiteService : IConformiteService
{
    private readonly AkarangaBankContext _context;
    private readonly ISeuilAMLService _seuils;
    public ConformiteService(AkarangaBankContext context, ISeuilAMLService seuils) { _context = context; _seuils = seuils; }
    public Task<decimal> GetSeuilAMLAsync() => _seuils.GetSeuilVigilanceAsync();
    public async Task<ResultatConformite> AnalyserTransactionAsync(int compteId, decimal montant, string typeOperation)
    {
        if (montant <= 0) throw new InvalidOperationException("Le montant doit être positif.");
        var compte = await _context.Comptes.Include(c => c.Client).FirstOrDefaultAsync(c => c.Id == compteId && !c.EstCompteInterne) ?? throw new InvalidOperationException("Compte introuvable.");
        var vigilance = await _seuils.GetSeuilVigilanceAsync(); var declaration = (await _seuils.GetSeuilAsync("DECLARATION", typeOperation))?.Montant ?? await _seuils.GetSeuilDeclarationAsync(); var blocage = await _seuils.GetSeuilBlocageAsync();
        var r = new ResultatConformite { CompteId = compteId, Montant = montant, DateAnalyse = DateTime.UtcNow };
        var espece = typeOperation is "Depot" or "Retrait";
        if (espece && montant >= declaration) { r.NecessiteDeclaration = true; r.Motif = $"Transaction en espèces au-dessus du seuil de déclaration de {declaration:N0} BIF."; r.Niveau = NiveauAlerte.Eleve; }
        if (montant >= vigilance) { r.NecessiteVigilanceRenforcee = true; r.Motif ??= $"Montant au-dessus du seuil de vigilance de {vigilance:N0} BIF."; if (!r.NecessiteDeclaration) r.Niveau = NiveauAlerte.Moyen; }
        if (montant >= blocage) { r.EstBloquee = true; r.Motif = $"Montant au-dessus du seuil de blocage de {blocage:N0} BIF."; r.Niveau = NiveauAlerte.Critique; }
        if ((typeOperation is "Retrait" or "Virement" or "VirementInterAgence") && compte.Solde < montant) { r.EstBloquee = true; r.Motif = "Solde insuffisant."; r.Niveau = NiveauAlerte.Critique; }
        if (compte.Statut != "Actif") { r.EstBloquee = true; r.Motif = $"Compte {compte.Statut}. Opération impossible."; r.Niveau = NiveauAlerte.Critique; }
        return r;
    }
    public async Task<bool> DetecterFractionnementAsync(int compteId, decimal montant, DateTime date)
    {
        var seuil = await GetSeuilAMLAsync(); var start = date.AddHours(-24);
        var total = await _context.Operations.Where(o => o.CompteId == compteId && o.DateOperation >= start && o.DateOperation < date && o.Montant < seuil && o.Statut == "Valide").SumAsync(o => (decimal?)o.Montant) ?? 0m;
        return montant < seuil && total + montant >= seuil;
    }
    public async Task<NiveauRisqueClient> EvaluerRisqueClientAsync(int clientId)
    {
        var client = await _context.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == clientId); if (client is null) return NiveauRisqueClient.Inconnu;
        var accounts = await _context.Comptes.Where(c => c.ClientId == clientId && !c.EstCompteInterne).Select(c => c.Id).ToListAsync();
        var nbAlertes = await _context.AlertesAML.CountAsync(a => a.ClientId == clientId && a.Statut != "Classee");
        var kycValide = await _context.VerificationsKYC.AnyAsync(v => v.ClientId == clientId && v.Statut == "Valide");
        var score = nbAlertes * 2 + (accounts.Count >= 4 ? 1 : 0) + (!kycValide ? 2 : 0);
        return score >= 5 ? NiveauRisqueClient.Eleve : score >= 2 ? NiveauRisqueClient.Moyen : NiveauRisqueClient.Faible;
    }
}
