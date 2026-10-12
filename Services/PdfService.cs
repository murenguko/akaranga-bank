using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using AkarangaBank.Web.Models;
using AkarangaBank.Web.ViewModels;

namespace AkarangaBank.Web.Services;

public class PdfService : IPdfService
{
    private static readonly string CouleurBleuMarine = "#1F3864";
    private static readonly string CouleurOr = "#C9A227";

    public PdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenererReleveComptePdf(ReleveViewModel vm)
    {
        var doc = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Lato"));

                // En-tête officiel
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("AKARANGA BANK").FontSize(18).Bold().FontColor(CouleurBleuMarine);
                            c.Item().Text("Siège Social : Boulevard de l'Indépendance, Bujumbura").FontSize(8).FontColor(Colors.Grey.Medium);
                            c.Item().Text("Agrément BRB N° 2012/04 - République du Burundi").FontSize(8).FontColor(Colors.Grey.Medium);
                        });
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text("RELEVÉ DE COMPTE").FontSize(14).Bold().FontColor(CouleurOr);
                            c.Item().Text($"Date d'édition : {DateTime.UtcNow:dd/MM/yyyy HH:mm}").FontSize(9);
                        });
                    });
                    col.Item().PaddingVertical(5).LineHorizontal(1.5f).LineColor(CouleurBleuMarine);
                });

                // Contenu
                page.Content().PaddingVertical(10).Column(col =>
                {
                    // Encadré client / compte
                    col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Titulaire : {vm.Compte.Client?.Nom} {vm.Compte.Client?.Prenom}").Bold();
                            c.Item().Text($"Numéro Client : {vm.Compte.Client?.NumeroClient}");
                            c.Item().Text($"Adresse : {vm.Compte.Client?.Adresse ?? "Bujumbura, Burundi"}");
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Compte N° : {vm.Compte.NumeroCompte}").Bold().FontColor(CouleurBleuMarine);
                            c.Item().Text($"Type de compte : {vm.Compte.TypeCompte?.Libelle ?? "Courant"}");
                            c.Item().Text($"Devise : {vm.Compte.Devise}");
                        });
                    });

                    // Soldes initial et final
                    col.Item().PaddingVertical(10).Row(row =>
                    {
                        row.RelativeItem().Text($"Solde Initial : {vm.SoldeInitial:N2} {vm.Compte.Devise}").SemiBold();
                        row.RelativeItem().AlignRight().Text($"Solde Final au {DateTime.UtcNow:dd/MM/yyyy} : {vm.SoldeFinal:N2} {vm.Compte.Devise}").Bold().FontColor(CouleurBleuMarine);
                    });

                    // Tableau des écritures
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(75); // Date
                            columns.RelativeColumn(3);  // Libellé
                            columns.RelativeColumn(2);  // Opération
                            columns.ConstantColumn(85); // Débit
                            columns.ConstantColumn(85); // Crédit
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Background(CouleurBleuMarine).Padding(4).Text("Date").Bold().FontColor(Colors.White);
                            header.Cell().Background(CouleurBleuMarine).Padding(4).Text("Libellé").Bold().FontColor(Colors.White);
                            header.Cell().Background(CouleurBleuMarine).Padding(4).Text("Réf Opération").Bold().FontColor(Colors.White);
                            header.Cell().Background(CouleurBleuMarine).Padding(4).AlignRight().Text("Débit").Bold().FontColor(Colors.White);
                            header.Cell().Background(CouleurBleuMarine).Padding(4).AlignRight().Text("Crédit").Bold().FontColor(Colors.White);
                        });

                        foreach (var e in vm.Ecritures)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(e.DateEcriture.ToString("dd/MM/yyyy"));
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(e.Libelle ?? "-");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(e.Operation?.Reference ?? "-");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(e.Sens == "Debit" ? $"{e.Montant:N2}" : "");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(e.Sens == "Credit" ? $"{e.Montant:N2}" : "");
                        }
                    });
                });

                // Pied de page avec numérotation
                page.Footer().Column(col =>
                {
                    col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                    col.Item().PaddingTop(4).Row(row =>
                    {
                        row.RelativeItem().Text("Akaranga Bank — Document officiel et infalsifiable").FontSize(8).FontColor(Colors.Grey.Medium);
                        row.RelativeItem().AlignRight().Text(text =>
                        {
                            text.Span("Page ");
                            text.CurrentPageNumber();
                            text.Span(" / ");
                            text.TotalPages();
                        });
                    });
                });
            });
        });

        return doc.GeneratePdf();
    }

    public byte[] GenererAttestationSoldePdf(Compte compte, string? motif = null)
    {
        var doc = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Lato"));

                page.Header().Column(col =>
                {
                    col.Item().Text("AKARANGA BANK").FontSize(20).Bold().FontColor(CouleurBleuMarine);
                    col.Item().Text("Département des Opérations et Gestion des Comptes").FontSize(10).FontColor(CouleurOr);
                    col.Item().Text("Bujumbura, République du Burundi").FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(10).LineHorizontal(1.5f).LineColor(CouleurBleuMarine);
                });

                page.Content().PaddingVertical(25).Column(col =>
                {
                    col.Item().AlignCenter().Text("ATTESTATION DE SOLDE BANCAIRE").FontSize(16).Bold().FontColor(CouleurBleuMarine);
                    col.Item().PaddingTop(20).Text($"Réf : ATT-{DateTime.UtcNow:yyyyMMdd}-{compte.Id:D4}").FontSize(9).FontColor(Colors.Grey.Darken1);

                    col.Item().PaddingTop(25).Text("La Direction d'Akaranga Bank certifie par la présente que :").FontSize(11);

                    col.Item().PaddingTop(15).PaddingLeft(20).Column(c =>
                    {
                        c.Item().Text($"Titulaire du compte : Monsieur / Madame {compte.Client?.Nom} {compte.Client?.Prenom}").Bold();
                        c.Item().Text($"Numéro d'Identification Nationale (CNI) : {compte.Client?.NumeroCNI}");
                        c.Item().Text($"Numéro de compte : {compte.NumeroCompte}").Bold().FontColor(CouleurBleuMarine);
                        c.Item().Text($"Agence de tenue de compte : {compte.Agence?.Nom ?? "Bujumbura"} ({compte.Agence?.Code ?? "AKR-BJM"})");
                    });

                    col.Item().PaddingTop(20).Text(text =>
                    {
                        text.Span("Présente à la date du ");
                        text.Span(DateTime.UtcNow.ToString("dd MMMM yyyy")).Bold();
                        text.Span(" un solde créditeur disponible de :");
                    });

                    col.Item().PaddingTop(10).AlignCenter().Border(1.5f).BorderColor(CouleurBleuMarine).Padding(12).Column(c =>
                    {
                        c.Item().AlignCenter().Text($"{compte.Solde:N2} {compte.Devise}").FontSize(18).Bold().FontColor(CouleurBleuMarine);
                        c.Item().AlignCenter().Text($"Soit en toutes lettres : {ConvertirMontantEnLettres(compte.Solde)} Francs Burundais").FontSize(10).Italic();
                    });

                    if (!string.IsNullOrWhiteSpace(motif))
                    {
                        col.Item().PaddingTop(15).Text($"Motif de la demande : {motif}").FontSize(10).FontColor(Colors.Grey.Darken2);
                    }

                    col.Item().PaddingTop(20).Text("La présente attestation est délivrée à l'intéressé(e) pour servir et valoir ce que de droit.").FontSize(10);

                    // Bloc signature et tampon officiel
                    col.Item().PaddingTop(40).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Fait à Bujumbura, le {DateTime.UtcNow:dd/MM/yyyy}");
                            c.Item().PaddingTop(5).Text("Pour le Directeur Général,").Bold();
                            c.Item().Text("Le Chef du Département Opérations").FontSize(10);
                        });
                        row.RelativeItem().AlignRight().Border(1).BorderColor(CouleurOr).Padding(10).Column(c =>
                        {
                            c.Item().AlignCenter().Text("TAMPON OFFICIEL").FontSize(9).Bold().FontColor(CouleurOr);
                            c.Item().AlignCenter().Text("AKARANGA BANK").FontSize(11).Bold().FontColor(CouleurBleuMarine);
                            c.Item().AlignCenter().Text("Visa et Contrôle Bancaire").FontSize(8);
                        });
                    });
                });

                page.Footer().AlignCenter().Text("Akaranga Bank S.A. - Capital social : 500 000 000 BIF - Siège social : Bujumbura").FontSize(8).FontColor(Colors.Grey.Medium);
            });
        });

        return doc.GeneratePdf();
    }

    public byte[] GenererContratPretPdf(Pret pret)
    {
        var doc = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.8f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontFamily("Lato"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("AKARANGA BANK").FontSize(16).Bold().FontColor(CouleurBleuMarine);
                        row.RelativeItem().AlignRight().Text($"CONTRAT DE CRÉDIT N° {pret.Reference}").FontSize(11).Bold().FontColor(CouleurOr);
                    });
                    col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(CouleurBleuMarine);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().AlignCenter().Text("CONTRAT DE PRÊT BANCAIRE").FontSize(14).Bold().FontColor(CouleurBleuMarine);

                    col.Item().PaddingTop(10).Text("ENTRE LES SOUSSIGNÉS :").Bold();
                    col.Item().PaddingLeft(10).Text("1. AKARANGA BANK S.A., représentée par son représentant légal, ci-après dénommée « La Banque ».");
                    col.Item().PaddingLeft(10).Text($"2. Monsieur / Madame {pret.Client?.Nom} {pret.Client?.Prenom}, titulaire de la CNI N° {pret.Client?.NumeroCNI}, ci-après dénommé(e) « L'Emprunteur ».");

                    col.Item().PaddingTop(10).Text("IL A ÉTÉ CONVENU CE QUI SUIT :").Bold();

                    col.Item().PaddingTop(5).Text("ARTICLE 1 : OBJET DU FINANCEMENT").Bold().FontColor(CouleurBleuMarine);
                    col.Item().Text($"La Banque accorde à l'Emprunteur un crédit de type {pret.TypePret} d'un montant principal de {pret.MontantPrincipal:N2} BIF ({ConvertirMontantEnLettres(pret.MontantPrincipal)} Francs Burundais).");

                    col.Item().PaddingTop(5).Text("ARTICLE 2 : DURÉE ET MODALITÉS DE REMBOURSEMENT").Bold().FontColor(CouleurBleuMarine);
                    col.Item().Text($"Le crédit est consenti pour une durée ferme de {pret.DureeMois} mois consécutifs. Le remboursement s'effectuera par {pret.DureeMois} mensualités constantes de {pret.Mensualite:N2} BIF chacune, prélevées sur le compte N° {pret.CompteDebit?.NumeroCompte}.");

                    col.Item().PaddingTop(5).Text("ARTICLE 3 : TAUX D'INTÉRÊT").Bold().FontColor(CouleurBleuMarine);
                    col.Item().Text($"Le taux d'intérêt conventionnel annuel fixe est fixé à {pret.TauxInteret:0.00} % l'an.");

                    col.Item().PaddingTop(5).Text("ARTICLE 4 : DÉFAUT DE PAIEMENT ET PÉNALITÉS").Bold().FontColor(CouleurBleuMarine);
                    col.Item().Text("Toute échéance impayée à sa date entraîne de plein droit une pénalité de retard de 0,5 % par mois de retard (minimum 500 BIF). À partir de trois (3) échéances impayées, la créance devient immédiatement exigible dans sa totalité.");

                    col.Item().PaddingTop(5).Text("ARTICLE 5 : ENGAGEMENT ET GARANTIES").Bold().FontColor(CouleurBleuMarine);
                    col.Item().Text("L'Emprunteur autorise irrévocablement la Banque à prélever toute somme due sur l'ensemble de ses avoirs en cas de défaillance.");

                    // Signatures
                    col.Item().PaddingTop(30).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Pour l'Emprunteur,").Bold();
                            c.Item().Text("(Mention manuscrite 'Lu et approuvé')").FontSize(8).Italic();
                            c.Item().PaddingTop(30).Text($"{pret.Client?.Nom} {pret.Client?.Prenom}");
                        });
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text("Pour Akaranga Bank,").Bold();
                            c.Item().Text("Le Représentant de l'Agence").FontSize(8).Italic();
                            c.Item().PaddingTop(30).Text("Signature et Cachet Officiel");
                        });
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Akaranga Bank — Page ");
                    t.CurrentPageNumber();
                    t.Span(" / ");
                    t.TotalPages();
                });
            });
        });

        return doc.GeneratePdf();
    }

    public byte[] GenererDeclarationCnrPdf(DeclarationSoupcon dos)
    {
        var doc = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.8f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Lato"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("RÉPUBLIQUE DU BURUNDI").FontSize(11).Bold();
                            c.Item().Text("Cellule Nationale de Renseignement Financier (CNRF)").FontSize(9);
                        });
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text("CONFIDENTIEL DÉFENSE").FontSize(11).Bold().FontColor(Colors.Red.Medium);
                            c.Item().Text($"Réf DOS : {dos.Reference}").FontSize(9);
                        });
                    });
                    col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Red.Medium);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().AlignCenter().Text("DÉCLARATION D'OPÉRATION SUSPECTE (DOS)").FontSize(14).Bold().FontColor(CouleurBleuMarine);
                    col.Item().PaddingTop(15).Text(dos.Contenu).FontSize(9).FontFamily("Lato");

                    col.Item().PaddingTop(20).Text("CADRE LÉGAL RÉGLEMENTAIRE :").Bold();
                    col.Item().Text("Déclaration souscrite en application de la Loi N° 1/02 du 4 février 2008 portant lutte contre le blanchiment de capitaux et le financement du terrorisme, modifiée par la Loi N° 1/08 du 27 mars 2025.").FontSize(8.5f).Italic();
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Transmission confidentielle CNRF — Page ");
                    t.CurrentPageNumber();
                });
            });
        });

        return doc.GeneratePdf();
    }

    public byte[] GenererRecuOperationPdf(Operation operation)
    {
        var doc = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5.Landscape()); // Format A5 requis par EF-15.5
                page.Margin(1.2f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Lato"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("AKARANGA BANK").FontSize(14).Bold().FontColor(CouleurBleuMarine);
                            c.Item().Text($"Agence : {operation.Agence?.Nom ?? "Bujumbura"} ({operation.Agence?.Code ?? "AKR-BJM"})").FontSize(8);
                        });
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text("REÇU D'OPÉRATION").FontSize(12).Bold().FontColor(CouleurOr);
                            c.Item().Text($"Réf : {operation.Reference}").FontSize(9).Bold();
                        });
                    });
                    col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(CouleurBleuMarine);
                });

                page.Content().PaddingVertical(8).Column(col =>
                {
                    col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Date : {operation.DateOperation:dd/MM/yyyy HH:mm:ss}");
                            c.Item().Text($"Compte : {operation.Compte?.NumeroCompte}").Bold();
                            c.Item().Text($"Client : {operation.Compte?.Client?.Nom} {operation.Compte?.Client?.Prenom}");
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Type d'opération : {operation.TypeOperation}").Bold();
                            c.Item().Text($"Guichetier : {operation.Utilisateur?.NomComplet ?? "Agent de caisse"}");
                            c.Item().Text($"Statut : {operation.Statut}");
                        });
                    });

                    col.Item().PaddingTop(12).AlignCenter().Border(1.5f).BorderColor(CouleurBleuMarine).Padding(10).Column(c =>
                    {
                        c.Item().AlignCenter().Text($"MONTANT : {operation.Montant:N2} {operation.Devise}").FontSize(16).Bold().FontColor(CouleurBleuMarine);
                        c.Item().AlignCenter().Text($"{ConvertirMontantEnLettres(operation.Montant)} Francs Burundais").FontSize(9).Italic();
                    });

                    if (!string.IsNullOrEmpty(operation.Commentaire))
                    {
                        col.Item().PaddingTop(6).Text($"Motif / Observation : {operation.Commentaire}").FontSize(8.5f);
                    }

                    col.Item().PaddingTop(15).Row(row =>
                    {
                        row.RelativeItem().Text("Signature du Client :").FontSize(8.5f);
                        row.RelativeItem().AlignRight().Text("Cachet du Guichet :").FontSize(8.5f);
                    });
                });

                page.Footer().AlignCenter().Text("Conservez ce reçu pour tout recours éventuel. Merci de faire confiance à Akaranga Bank.").FontSize(7.5f).FontColor(Colors.Grey.Medium);
            });
        });

        return doc.GeneratePdf();
    }

    private static string ConvertirMontantEnLettres(decimal montant)
    {
        long n = (long)Math.Floor(montant);
        if (n == 0) return "Zéro";
        if (n >= 1_000_000_000) return $"{n:N0}";

        string[] unites = { "", "Un", "Deux", "Trois", "Quatre", "Cinq", "Six", "Sept", "Huit", "Neuf", "Dix", "Onze", "Douze", "Treize", "Quatorze", "Quinze", "Seize", "Dix-Sept", "Dix-Huit", "Dix-Neuf" };
        string[] dizaines = { "", "", "Vingt", "Trente", "Quarante", "Cinquante", "Soixante", "Soixante-Dix", "Quatre-Vingt", "Quatre-Vingt-Dix" };

        if (n < 20) return unites[n];
        if (n < 100) return dizaines[n / 10] + (n % 10 > 0 ? "-" + unites[n % 10] : "");
        if (n < 1000) return (n / 100 == 1 ? "Cent" : unites[n / 100] + " Cent") + (n % 100 > 0 ? " " + ConvertirMontantEnLettres(n % 100) : "");
        if (n < 1_000_000) return (n / 1000 == 1 ? "Mille" : ConvertirMontantEnLettres(n / 1000) + " Mille") + (n % 1000 > 0 ? " " + ConvertirMontantEnLettres(n % 1000) : "");
        return ConvertirMontantEnLettres(n / 1_000_000) + " Million(s)" + (n % 1_000_000 > 0 ? " " + ConvertirMontantEnLettres(n % 1_000_000) : "");
    }
}
