using AkarangaBank.Web.Models;
using AkarangaBank.Web.ViewModels;

namespace AkarangaBank.Web.Services;

public interface IPdfService
{
    byte[] GenererReleveComptePdf(ReleveViewModel vm);
    byte[] GenererAttestationSoldePdf(Compte compte, string? motif = null);
    byte[] GenererContratPretPdf(Pret pret);
    byte[] GenererDeclarationCnrPdf(DeclarationSoupcon dos);
    byte[] GenererRecuOperationPdf(Operation operation);
}
