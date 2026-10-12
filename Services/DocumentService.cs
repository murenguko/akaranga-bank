using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;
using AkarangaBank.Web.ViewModels;

namespace AkarangaBank.Web.Services;

public class DocumentService : IDocumentService
{
    private readonly AkarangaBankContext _context;
    private readonly string _storagePath;

    private static readonly HashSet<string> ExtensionsAutorisees = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".docx", ".xlsx"
    };

    private static readonly HashSet<string> CategoriesAutorisees = new(StringComparer.OrdinalIgnoreCase)
    {
        "KYC", "Contrat", "Comptable", "AML", "RH", "Audit"
    };

    public DocumentService(AkarangaBankContext context, IWebHostEnvironment environment)
    {
        _context = context;
        // Storage is securely outside wwwroot (ENF-14.1)
        _storagePath = Path.Combine(environment.ContentRootPath, "Storage", "Documents");
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    public async Task<Document> UploadAsync(DocumentUploadViewModel vm, int utilisateurId)
    {
        if (vm.Fichier == null || vm.Fichier.Length == 0)
            throw new InvalidOperationException("Le fichier est vide.");

        const long maxSizeBytes = 10 * 1024 * 1024; // 10 Mo
        if (vm.Fichier.Length > maxSizeBytes)
            throw new InvalidOperationException("Le fichier dépasse la taille maximale autorisée de 10 Mo.");

        string ext = Path.GetExtension(vm.Fichier.FileName);
        if (!ExtensionsAutorisees.Contains(ext))
            throw new InvalidOperationException($"Extension non autorisée ({ext}). Seuls les formats PDF, JPG, PNG, DOCX, XLSX sont acceptés.");

        if (!CategoriesAutorisees.Contains(vm.Categorie))
            throw new InvalidOperationException($"Catégorie '{vm.Categorie}' non reconnue.");

        // Calcul du hash SHA-256
        string hashSha256;
        using (var stream = vm.Fichier.OpenReadStream())
        using (var sha256 = SHA256.Create())
        {
            byte[] hashBytes = await sha256.ComputeHashAsync(stream);
            hashSha256 = Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        // Nom unique (GUID)
        string nomStocke = $"{Guid.NewGuid():N}{ext}";
        string cheminComplet = Path.Combine(_storagePath, nomStocke);

        using (var targetStream = new FileStream(cheminComplet, FileMode.Create, FileAccess.Write))
        {
            await vm.Fichier.CopyToAsync(targetStream);
        }

        var doc = new Document
        {
            Titre = vm.Titre.Trim(),
            NomFichierOriginal = Path.GetFileName(vm.Fichier.FileName),
            NomFichierStocke = nomStocke,
            Extension = ext.ToLowerInvariant(),
            Categorie = vm.Categorie,
            TailleOctets = vm.Fichier.Length,
            HashSha256 = hashSha256,
            CheminFichier = cheminComplet,
            Statut = "Actif",
            ClientId = vm.ClientId,
            CompteId = vm.CompteId,
            PretId = vm.PretId,
            UploadeParId = utilisateurId,
            DateCreation = DateTime.UtcNow
        };

        _context.Documents.Add(doc);
        await _context.SaveChangesAsync();

        // Trace l'accès initial (Upload)
        _context.DocumentAcces.Add(new DocumentAcces
        {
            DocumentId = doc.Id,
            UtilisateurId = utilisateurId,
            TypeAction = "Upload",
            DateAcces = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return doc;
    }

    public async Task<(byte[] Bytes, string ContentType, string NomFichier)> TelechargerAsync(int id, int utilisateurId, string? ip, string? userAgent)
    {
        var doc = await _context.Documents.FindAsync(id)
            ?? throw new InvalidOperationException("Document introuvable.");

        if (doc.Statut == "Supprime")
            throw new InvalidOperationException("Ce document a été supprimé.");

        if (!File.Exists(doc.CheminFichier))
            throw new FileNotFoundException("Le fichier physique est introuvable sur le serveur de stockage.");

        byte[] bytes = await File.ReadAllBytesAsync(doc.CheminFichier);

        // Tracer l'accès dans DocumentAcces (EF-14.4 & ENF-14.2)
        _context.DocumentAcces.Add(new DocumentAcces
        {
            DocumentId = doc.Id,
            UtilisateurId = utilisateurId,
            TypeAction = "Telechargement",
            DateAcces = DateTime.UtcNow,
            AdresseIP = ip,
            UserAgent = userAgent
        });
        await _context.SaveChangesAsync();

        string contentType = doc.Extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream"
        };

        return (bytes, contentType, doc.NomFichierOriginal);
    }

    public async Task<bool> ArchiverAsync(int id, int utilisateurId)
    {
        var doc = await _context.Documents.FindAsync(id);
        if (doc is null) return false;

        doc.Statut = "Archive";
        _context.DocumentAcces.Add(new DocumentAcces
        {
            DocumentId = doc.Id,
            UtilisateurId = utilisateurId,
            TypeAction = "Archivage",
            DateAcces = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SupprimerAsync(int id, int utilisateurId)
    {
        var doc = await _context.Documents.FindAsync(id);
        if (doc is null) return false;

        // Suppression logique (EF-14.6)
        doc.Statut = "Supprime";
        doc.DateSuppression = DateTime.UtcNow;

        _context.DocumentAcces.Add(new DocumentAcces
        {
            DocumentId = doc.Id,
            UtilisateurId = utilisateurId,
            TypeAction = "Suppression",
            DateAcces = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> PurgerDocumentsSupprimesAsync(int delaiJours = 30)
    {
        DateTime seuilDate = DateTime.UtcNow.AddDays(-delaiJours);
        var docsAPurger = await _context.Documents
            .Where(d => d.Statut == "Supprime" && d.DateSuppression <= seuilDate)
            .ToListAsync();

        int purgedCount = 0;
        foreach (var d in docsAPurger)
        {
            try
            {
                if (File.Exists(d.CheminFichier))
                {
                    File.Delete(d.CheminFichier);
                }
                _context.Documents.Remove(d);
                purgedCount++;
            }
            catch
            {
                // Continue with next documents
            }
        }

        await _context.SaveChangesAsync();
        return purgedCount;
    }

    public async Task<DocumentListViewModel> GetPagedAsync(string? recherche, string? categorie, string? statut, int? clientId, int page, int taille)
    {
        var query = _context.Documents
            .Include(d => d.Client)
            .Include(d => d.Compte)
            .Include(d => d.Pret)
            .Include(d => d.UploadePar)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(recherche))
        {
            var r = recherche.Trim();
            query = query.Where(d =>
                d.Titre.Contains(r) ||
                d.NomFichierOriginal.Contains(r) ||
                (d.Client != null && (d.Client.Nom.Contains(r) || d.Client.Prenom.Contains(r))));
        }

        if (!string.IsNullOrWhiteSpace(categorie))
            query = query.Where(d => d.Categorie == categorie);

        if (!string.IsNullOrWhiteSpace(statut))
            query = query.Where(d => d.Statut == statut);
        else
            query = query.Where(d => d.Statut != "Supprime"); // Par défaut cacher les supprimés

        if (clientId.HasValue)
            query = query.Where(d => d.ClientId == clientId.Value);

        var total = await query.CountAsync();
        var documents = await query
            .OrderByDescending(d => d.DateCreation)
            .Skip((page - 1) * taille)
            .Take(taille)
            .ToListAsync();

        return new DocumentListViewModel
        {
            Documents = documents,
            Recherche = recherche,
            Categorie = categorie,
            Statut = statut,
            ClientId = clientId,
            Page = page,
            TaillePage = taille,
            Total = total
        };
    }

    public async Task<DocumentDetailsViewModel?> GetDetailsAsync(int id)
    {
        var doc = await _context.Documents
            .Include(d => d.Client)
            .Include(d => d.Compte)
            .Include(d => d.Pret)
            .Include(d => d.UploadePar)
            .Include(d => d.Acces)
            .ThenInclude(a => a.Utilisateur)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);

        if (doc is null) return null;

        return new DocumentDetailsViewModel
        {
            Document = doc,
            Acces = doc.Acces.OrderByDescending(a => a.DateAcces).ToList()
        };
    }

    public async Task<Document?> GetByIdAsync(int id)
    {
        return await _context.Documents
            .Include(d => d.Client)
            .Include(d => d.Compte)
            .Include(d => d.Pret)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<IEnumerable<DocumentAcces>> GetHistoriqueAccesAsync(int documentId)
    {
        return await _context.DocumentAcces
            .Include(a => a.Utilisateur)
            .Where(a => a.DocumentId == documentId)
            .OrderByDescending(a => a.DateAcces)
            .ToListAsync();
    }
}
