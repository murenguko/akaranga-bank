using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.Services;

public class CsvService : ICsvService
{
    public byte[] GenererJournalCsv(IEnumerable<Operation> operations)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",
            HasHeaderRecord = true
        };

        using var memoryStream = new MemoryStream();
        // UTF8 with BOM ensures characters and accents render properly in Microsoft Excel
        using (var writer = new StreamWriter(memoryStream, new UTF8Encoding(true), leaveOpen: true))
        using (var csv = new CsvWriter(writer, config))
        {
            csv.WriteField("Date");
            csv.WriteField("Reference");
            csv.WriteField("TypeOperation");
            csv.WriteField("Compte");
            csv.WriteField("Montant");
            csv.WriteField("Devise");
            csv.WriteField("Statut");
            csv.WriteField("Utilisateur");
            csv.WriteField("Commentaire");
            csv.NextRecord();

            foreach (var op in operations)
            {
                csv.WriteField(op.DateOperation.ToString("yyyy-MM-dd HH:mm:ss"));
                csv.WriteField(op.Reference);
                csv.WriteField(op.TypeOperation);
                csv.WriteField(op.Compte?.NumeroCompte ?? "-");
                csv.WriteField(op.Montant.ToString("F2", CultureInfo.InvariantCulture));
                csv.WriteField(op.Devise);
                csv.WriteField(op.Statut);
                csv.WriteField(op.Utilisateur?.NomComplet ?? "-");
                csv.WriteField(op.Commentaire ?? "");
                csv.NextRecord();
            }
        }

        return memoryStream.ToArray();
    }
}
