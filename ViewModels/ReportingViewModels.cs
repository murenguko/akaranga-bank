using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.ViewModels;

public class DashboardViewModel
{
    public int NombreClients { get; set; }
    public int NombreComptes { get; set; }
    public decimal SoldeTotalDepots { get; set; }
    public int OperationsJour { get; set; }
    public decimal MontantOperationsJour { get; set; }
    public int PretsEnCours { get; set; }
    public decimal EncoursPrets { get; set; }
    public int AlertesAmlOuvertes { get; set; }
    public int CartesActives { get; set; }
    public IEnumerable<Operation> DernieresOperations { get; set; } = new List<Operation>();
    public IEnumerable<AlerteAML> DernieresAlertes { get; set; } = new List<AlerteAML>();
}

public class BalanceCompteItem
{
    public int CompteId { get; set; }
    public string NumeroCompte { get; set; } = string.Empty;
    public string Intitule { get; set; } = string.Empty;
    public string AgenceCode { get; set; } = string.Empty;
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal SoldeDebiteur { get; set; }
    public decimal SoldeCrediteur { get; set; }
}

public class BalanceGeneraleViewModel
{
    public IEnumerable<BalanceCompteItem> Lignes { get; set; } = new List<BalanceCompteItem>();
    public decimal TotalDebitsGlobal => Lignes.Sum(l => l.TotalDebits);
    public decimal TotalCreditsGlobal => Lignes.Sum(l => l.TotalCredits);
    public decimal TotalSoldesDebiteurs => Lignes.Sum(l => l.SoldeDebiteur);
    public decimal TotalSoldesCrediteurs => Lignes.Sum(l => l.SoldeCrediteur);
    public bool EstEquilibree => TotalDebitsGlobal == TotalCreditsGlobal;
}

public class CompteResultatViewModel
{
    public DateTime Du { get; set; }
    public DateTime Au { get; set; }

    // Produits (Gains de la banque)
    public decimal InteretsPerçusPrets { get; set; }
    public decimal CommissionsEtFraisTenueCompte { get; set; }
    public decimal TotalProduits => InteretsPerçusPrets + CommissionsEtFraisTenueCompte;

    // Charges (Coûts supportés par la banque)
    public decimal InteretsVersesEpargne { get; set; }
    public decimal ChargesExploitationEstimees { get; set; }
    public decimal TotalCharges => InteretsVersesEpargne + ChargesExploitationEstimees;

    // Résultat net = Produits - Charges
    public decimal ResultatNet => TotalProduits - TotalCharges;
}

public class BilanViewModel
{
    public DateTime DateArrete { get; set; }

    // Actif
    public decimal ReservesTresorerieEtBanqueCentrale { get; set; }
    public decimal PretsEtCreancesClients { get; set; }
    public decimal TotalActif => ReservesTresorerieEtBanqueCentrale + PretsEtCreancesClients;

    // Passif
    public decimal DepotsComptesCourants { get; set; }
    public decimal DepotsComptesEpargne { get; set; }
    public decimal TotalPassifDettes => DepotsComptesCourants + DepotsComptesEpargne;

    // Capitaux propres
    public decimal CapitalSocial { get; set; } = 500_000_000m;
    public decimal ReservesEtResultatReporte => TotalActif - (TotalPassifDettes + CapitalSocial);
    public decimal TotalCapitauxPropres => CapitalSocial + ReservesEtResultatReporte;

    public decimal TotalPassifEtCapitaux => TotalPassifDettes + TotalCapitauxPropres;
    public bool EstEquilibre => TotalActif == TotalPassifEtCapitaux;
}

public class RapportAmlMensuelViewModel
{
    public int Mois { get; set; }
    public int Annee { get; set; }
    public int TotalAlertes { get; set; }
    public int AlertesCritiques { get; set; }
    public int AlertesElevees { get; set; }
    public int AlertesMoyennes { get; set; }
    public int AlertesFaibles { get; set; }
    public int DosTransmises { get; set; }
    public decimal MontantTotalSuspect { get; set; }
    public IEnumerable<ClientRisqueItem> TopClientsARisque { get; set; } = new List<ClientRisqueItem>();
}

public class ClientRisqueItem
{
    public int ClientId { get; set; }
    public string NumeroClient { get; set; } = string.Empty;
    public string NomComplet { get; set; } = string.Empty;
    public string AgenceCode { get; set; } = string.Empty;
    public string NiveauRisque { get; set; } = string.Empty;
    public int NombreAlertes { get; set; }
    public decimal TotalMouvements { get; set; }
}
