using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.ViewModels;

public class DocumentDetailsViewModel
{
    public Document Document { get; set; } = null!;
    public IEnumerable<DocumentAcces> Acces { get; set; } = new List<DocumentAcces>();
}
