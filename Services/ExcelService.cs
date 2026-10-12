using ClosedXML.Excel;
using AkarangaBank.Web.Models;
using AkarangaBank.Web.ViewModels;

namespace AkarangaBank.Web.Services;

public class ExcelService : IExcelService
{
    private static readonly XLColor CouleurBleuMarine = XLColor.FromHtml("#1F3864");
    private static readonly XLColor CouleurOr = XLColor.FromHtml("#C9A227");

    public byte[] GenererJournalExcel(IEnumerable<Operation> operations)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Journal des Opérations");

        // Titre
        ws.Cell(1, 1).Value = "AKARANGA BANK — JOURNAL GÉNÉRAL DES OPÉRATIONS";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(1, 1).Style.Font.FontColor = CouleurBleuMarine;

        // En-têtes
        string[] headers = { "Date", "Référence", "Type Opération", "Compte", "Montant (BIF)", "Statut", "Utilisateur", "Commentaire" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(3, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = CouleurBleuMarine;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        int row = 4;
        foreach (var op in operations)
        {
            ws.Cell(row, 1).Value = op.DateOperation.ToString("yyyy-MM-dd HH:mm");
            ws.Cell(row, 2).Value = op.Reference;
            ws.Cell(row, 3).Value = op.TypeOperation;
            ws.Cell(row, 4).Value = op.Compte?.NumeroCompte ?? "-";
            ws.Cell(row, 5).Value = (double)op.Montant;
            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 6).Value = op.Statut;
            ws.Cell(row, 7).Value = op.Utilisateur?.NomComplet ?? "-";
            ws.Cell(row, 8).Value = op.Commentaire ?? "";
            row++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] GenererBalanceExcel(IEnumerable<BalanceCompteItem> balance)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Balance Générale");

        ws.Cell(1, 1).Value = "AKARANGA BANK — BALANCE GÉNÉRALE DES COMPTES";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(1, 1).Style.Font.FontColor = CouleurBleuMarine;

        string[] headers = { "Numéro Compte", "Intitulé / Client", "Agence", "Total Débits (BIF)", "Total Crédits (BIF)", "Solde Débiteur", "Solde Créditeur" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(3, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = CouleurBleuMarine;
            cell.Style.Font.FontColor = XLColor.White;
        }

        int row = 4;
        foreach (var b in balance)
        {
            ws.Cell(row, 1).Value = b.NumeroCompte;
            ws.Cell(row, 2).Value = b.Intitule;
            ws.Cell(row, 3).Value = b.AgenceCode;
            ws.Cell(row, 4).Value = (double)b.TotalDebits;
            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 5).Value = (double)b.TotalCredits;
            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 6).Value = (double)b.SoldeDebiteur;
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 7).Value = (double)b.SoldeCrediteur;
            ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
            row++;
        }

        // Totaux
        ws.Cell(row, 1).Value = "TOTAUX GÉNÉRAUX";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 4).FormulaA1 = $"SUM(D4:D{row - 1})";
        ws.Cell(row, 4).Style.Font.Bold = true;
        ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 5).FormulaA1 = $"SUM(E4:E{row - 1})";
        ws.Cell(row, 5).Style.Font.Bold = true;
        ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 6).FormulaA1 = $"SUM(F4:F{row - 1})";
        ws.Cell(row, 6).Style.Font.Bold = true;
        ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 7).FormulaA1 = $"SUM(G4:G{row - 1})";
        ws.Cell(row, 7).Style.Font.Bold = true;
        ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] GenererAlertesAmlExcel(IEnumerable<AlerteAML> alertes)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Alertes AML");

        ws.Cell(1, 1).Value = "AKARANGA BANK — REGISTRE DES ALERTES LBC/FT";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(1, 1).Style.Font.FontColor = CouleurBleuMarine;

        string[] headers = { "Date Alerte", "Client", "Numéro CNI", "Type d'Alerte", "Niveau", "Statut", "Description", "Traité Par" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(3, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = CouleurBleuMarine;
            cell.Style.Font.FontColor = XLColor.White;
        }

        int row = 4;
        foreach (var a in alertes)
        {
            ws.Cell(row, 1).Value = a.DateAlerte.ToString("yyyy-MM-dd HH:mm");
            ws.Cell(row, 2).Value = $"{a.Client?.Nom} {a.Client?.Prenom}";
            ws.Cell(row, 3).Value = a.Client?.NumeroCNI ?? "-";
            ws.Cell(row, 4).Value = a.TypeAlerte;
            ws.Cell(row, 5).Value = a.Niveau;
            ws.Cell(row, 6).Value = a.Statut;
            ws.Cell(row, 7).Value = a.Description ?? "";
            ws.Cell(row, 8).Value = a.TraitePar?.NomComplet ?? "-";
            row++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] GenererPortefeuillePretsExcel(IEnumerable<Pret> prets)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Portefeuille Crédits");

        ws.Cell(1, 1).Value = "AKARANGA BANK — PORTEFEUILLE DES CRÉDITS ET PRÊTS";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(1, 1).Style.Font.FontColor = CouleurBleuMarine;

        string[] headers = { "Référence", "Client", "Compte", "Type Prêt", "Capital Emprunté", "Taux Annuel", "Durée (Mois)", "Mensualité", "Capital Remboursé", "Solde Restant", "Statut" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(3, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = CouleurBleuMarine;
            cell.Style.Font.FontColor = XLColor.White;
        }

        int row = 4;
        foreach (var p in prets)
        {
            ws.Cell(row, 1).Value = p.Reference;
            ws.Cell(row, 2).Value = $"{p.Client?.Nom} {p.Client?.Prenom}";
            ws.Cell(row, 3).Value = p.CompteDebit?.NumeroCompte ?? "-";
            ws.Cell(row, 4).Value = p.TypePret;
            ws.Cell(row, 5).Value = (double)p.MontantPrincipal;
            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 6).Value = (double)p.TauxInteret / 100.0;
            ws.Cell(row, 6).Style.NumberFormat.Format = "0.00%";
            ws.Cell(row, 7).Value = p.DureeMois;
            ws.Cell(row, 8).Value = (double)p.Mensualite;
            ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 9).Value = (double)p.MontantRembourse;
            ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 10).Value = (double)p.SoldeRestant;
            ws.Cell(row, 10).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 11).Value = p.Statut;
            row++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}
