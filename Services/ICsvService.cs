using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.Services;

public interface ICsvService
{
    byte[] GenererJournalCsv(IEnumerable<Operation> operations);
}
