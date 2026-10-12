using AkarangaBank.Web.Models;
using AkarangaBank.Web.ViewModels;

namespace AkarangaBank.Web.Services;

public interface IExcelService
{
    byte[] GenererJournalExcel(IEnumerable<Operation> operations);
    byte[] GenererBalanceExcel(IEnumerable<BalanceCompteItem> balance);
    byte[] GenererAlertesAmlExcel(IEnumerable<AlerteAML> alertes);
    byte[] GenererPortefeuillePretsExcel(IEnumerable<Pret> prets);
}
