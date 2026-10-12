using AkarangaBank.Web.Models;
using AkarangaBank.Web.ViewModels;

namespace AkarangaBank.Web.Services;

public interface IDocumentService
{
    Task<Document> UploadAsync(DocumentUploadViewModel vm, int utilisateurId);
    Task<(byte[] Bytes, string ContentType, string NomFichier)> TelechargerAsync(int id, int utilisateurId, string? ip, string? userAgent);
    Task<bool> ArchiverAsync(int id, int utilisateurId);
    Task<bool> SupprimerAsync(int id, int utilisateurId);
    Task<int> PurgerDocumentsSupprimesAsync(int delaiJours = 30);
    Task<DocumentListViewModel> GetPagedAsync(string? recherche, string? categorie, string? statut, int? clientId, int page, int taille);
    Task<DocumentDetailsViewModel?> GetDetailsAsync(int id);
    Task<Document?> GetByIdAsync(int id);
    Task<IEnumerable<DocumentAcces>> GetHistoriqueAccesAsync(int documentId);
}
