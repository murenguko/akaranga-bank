using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkarangaBank.Web.Migrations
{
    /// <inheritdoc />
    public partial class AjoutChapitres12Et14 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Operations_Type",
                table: "Operations");

            migrationBuilder.AddColumn<string>(
                name: "Commentaire",
                table: "Prets",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontantRembourse",
                table: "Prets",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "MotifRejet",
                table: "Prets",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SoldeRestant",
                table: "Prets",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TypePret",
                table: "Prets",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Consommation");

            migrationBuilder.AddColumn<decimal>(
                name: "Penalite",
                table: "EcheancesPret",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SoldeRestant",
                table: "EcheancesPret",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "MotifOpposition",
                table: "Cartes",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroMasque",
                table: "Cartes",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PlafondLigne",
                table: "Cartes",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 500000m);

            migrationBuilder.AddColumn<decimal>(
                name: "PlafondPaiement",
                table: "Cartes",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 1000000m);

            migrationBuilder.AddColumn<decimal>(
                name: "PlafondRetrait",
                table: "Cartes",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 500000m);

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NomFichierOriginal = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    NomFichierStocke = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Extension = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Categorie = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TailleOctets = table.Column<long>(type: "bigint", nullable: false),
                    HashSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CheminFichier = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Actif"),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    DateSuppression = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    CompteId = table.Column<int>(type: "int", nullable: true),
                    PretId = table.Column<int>(type: "int", nullable: true),
                    UploadeParId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Documents_Comptes_CompteId",
                        column: x => x.CompteId,
                        principalTable: "Comptes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Documents_Prets_PretId",
                        column: x => x.PretId,
                        principalTable: "Prets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Documents_Utilisateurs_UploadeParId",
                        column: x => x.UploadeParId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Destinataire = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    UtilisateurId = table.Column<int>(type: "int", nullable: true),
                    TypeNotification = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Sujet = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "EnAttente"),
                    NombreTentatives = table.Column<int>(type: "int", nullable: false),
                    DerniereErreur = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    DateEnvoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateLecture = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Notifications_Utilisateurs_UtilisateurId",
                        column: x => x.UtilisateurId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "NotificationTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TypeNotification = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Langue = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Sujet = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContenuHtml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContenuTexte = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    DateModification = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransactionsCarte",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarteId = table.Column<int>(type: "int", nullable: false),
                    Montant = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TypeTransaction = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Validee"),
                    MotifRefus = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Commercant = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OperationId = table.Column<int>(type: "int", nullable: true),
                    DateTransaction = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionsCarte", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionsCarte_Cartes_CarteId",
                        column: x => x.CarteId,
                        principalTable: "Cartes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransactionsCarte_Operations_OperationId",
                        column: x => x.OperationId,
                        principalTable: "Operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DocumentAcces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    UtilisateurId = table.Column<int>(type: "int", nullable: true),
                    DateAcces = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    TypeAction = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AdresseIP = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentAcces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentAcces_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentAcces_Utilisateurs_UtilisateurId",
                        column: x => x.UtilisateurId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Operations_Type",
                table: "Operations",
                sql: "[TypeOperation] IN ('Depot', 'Retrait', 'Virement', 'VirementInterAgence', 'ReglementCompensation', 'DeblocagePret', 'RemboursementPret', 'Frais', 'Interet', 'PaiementCarte', 'RetraitCarte')");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAcces_DocumentId",
                table: "DocumentAcces",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAcces_UtilisateurId",
                table: "DocumentAcces",
                column: "UtilisateurId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ClientId",
                table: "Documents",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_CompteId",
                table: "Documents",
                column: "CompteId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_PretId",
                table: "Documents",
                column: "PretId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_UploadeParId",
                table: "Documents",
                column: "UploadeParId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ClientId",
                table: "Notifications",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UtilisateurId",
                table: "Notifications",
                column: "UtilisateurId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_Code_TypeNotification_Langue",
                table: "NotificationTemplates",
                columns: new[] { "Code", "TypeNotification", "Langue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransactionsCarte_CarteId",
                table: "TransactionsCarte",
                column: "CarteId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionsCarte_OperationId",
                table: "TransactionsCarte",
                column: "OperationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentAcces");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "NotificationTemplates");

            migrationBuilder.DropTable(
                name: "TransactionsCarte");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Operations_Type",
                table: "Operations");

            migrationBuilder.DropColumn(
                name: "Commentaire",
                table: "Prets");

            migrationBuilder.DropColumn(
                name: "MontantRembourse",
                table: "Prets");

            migrationBuilder.DropColumn(
                name: "MotifRejet",
                table: "Prets");

            migrationBuilder.DropColumn(
                name: "SoldeRestant",
                table: "Prets");

            migrationBuilder.DropColumn(
                name: "TypePret",
                table: "Prets");

            migrationBuilder.DropColumn(
                name: "Penalite",
                table: "EcheancesPret");

            migrationBuilder.DropColumn(
                name: "SoldeRestant",
                table: "EcheancesPret");

            migrationBuilder.DropColumn(
                name: "MotifOpposition",
                table: "Cartes");

            migrationBuilder.DropColumn(
                name: "NumeroMasque",
                table: "Cartes");

            migrationBuilder.DropColumn(
                name: "PlafondLigne",
                table: "Cartes");

            migrationBuilder.DropColumn(
                name: "PlafondPaiement",
                table: "Cartes");

            migrationBuilder.DropColumn(
                name: "PlafondRetrait",
                table: "Cartes");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Operations_Type",
                table: "Operations",
                sql: "[TypeOperation] IN ('Depot', 'Retrait', 'Virement', 'VirementInterAgence', 'ReglementCompensation', 'DeblocagePret', 'RemboursementPret', 'Frais', 'Interet')");
        }
    }
}
