using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkarangaBank.Web.Migrations
{
    /// <inheritdoc />
    public partial class AjoutChapitres8Et9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AlertesAML_Operations_OperationId1",
                table: "AlertesAML");

            migrationBuilder.DropForeignKey(
                name: "FK_AlertesAML_Utilisateur_TraiteParId1",
                table: "AlertesAML");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLog_Agences_AgenceId1",
                table: "AuditLog");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLog_Utilisateur_UtilisateurId1",
                table: "AuditLog");

            migrationBuilder.DropForeignKey(
                name: "FK_Cartes_Comptes_CompteId1",
                table: "Cartes");

            migrationBuilder.DropForeignKey(
                name: "FK_Comptes_Clients_ClientId1",
                table: "Comptes");

            migrationBuilder.DropForeignKey(
                name: "FK_Ecritures_Comptes_CompteId1",
                table: "Ecritures");

            migrationBuilder.DropForeignKey(
                name: "FK_Operations_Utilisateur_UtilisateurId1",
                table: "Operations");

            migrationBuilder.DropForeignKey(
                name: "FK_Parametres_Agences_AgenceId1",
                table: "Parametres");

            migrationBuilder.DropForeignKey(
                name: "FK_Prets_Comptes_CompteDebitId1",
                table: "Prets");

            migrationBuilder.DropForeignKey(
                name: "FK_Prets_Utilisateur_ValideParId1",
                table: "Prets");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionsUtilisateur_Utilisateur_UtilisateurId1",
                table: "SessionsUtilisateur");

            migrationBuilder.DropForeignKey(
                name: "FK_Utilisateur_Agences_AgenceId",
                table: "Utilisateur");

            migrationBuilder.DropForeignKey(
                name: "FK_VerificationsKYC_Clients_ClientId1",
                table: "VerificationsKYC");

            migrationBuilder.DropForeignKey(
                name: "FK_VerificationsKYC_Utilisateur_VerifieParId1",
                table: "VerificationsKYC");

            migrationBuilder.DropForeignKey(
                name: "FK_Virements_Utilisateur_UtilisateurId1",
                table: "Virements");

            migrationBuilder.DropIndex(
                name: "IX_Virements_UtilisateurId1",
                table: "Virements");

            migrationBuilder.DropIndex(
                name: "IX_VerificationsKYC_ClientId1",
                table: "VerificationsKYC");

            migrationBuilder.DropIndex(
                name: "IX_VerificationsKYC_VerifieParId1",
                table: "VerificationsKYC");

            migrationBuilder.DropIndex(
                name: "IX_SessionsUtilisateur_UtilisateurId1",
                table: "SessionsUtilisateur");

            migrationBuilder.DropIndex(
                name: "IX_Prets_CompteDebitId1",
                table: "Prets");

            migrationBuilder.DropIndex(
                name: "IX_Prets_ValideParId1",
                table: "Prets");

            migrationBuilder.DropIndex(
                name: "IX_Parametres_AgenceId1",
                table: "Parametres");

            migrationBuilder.DropIndex(
                name: "IX_Operations_UtilisateurId1",
                table: "Operations");

            migrationBuilder.DropIndex(
                name: "IX_Ecritures_CompteId1",
                table: "Ecritures");

            migrationBuilder.DropIndex(
                name: "IX_Comptes_ClientId1",
                table: "Comptes");

            migrationBuilder.DropIndex(
                name: "IX_Cartes_CompteId1",
                table: "Cartes");

            migrationBuilder.DropIndex(
                name: "IX_AuditLog_AgenceId1",
                table: "AuditLog");

            migrationBuilder.DropIndex(
                name: "IX_AuditLog_UtilisateurId1",
                table: "AuditLog");

            migrationBuilder.DropIndex(
                name: "IX_AlertesAML_OperationId1",
                table: "AlertesAML");

            migrationBuilder.DropIndex(
                name: "IX_AlertesAML_TraiteParId1",
                table: "AlertesAML");

            migrationBuilder.DropColumn(
                name: "UtilisateurId1",
                table: "Virements");

            migrationBuilder.DropColumn(
                name: "ClientId1",
                table: "VerificationsKYC");

            migrationBuilder.DropColumn(
                name: "VerifieParId1",
                table: "VerificationsKYC");

            migrationBuilder.DropColumn(
                name: "UtilisateurId1",
                table: "SessionsUtilisateur");

            migrationBuilder.DropColumn(
                name: "CompteDebitId1",
                table: "Prets");

            migrationBuilder.DropColumn(
                name: "ValideParId1",
                table: "Prets");

            migrationBuilder.DropColumn(
                name: "AgenceId1",
                table: "Parametres");

            migrationBuilder.DropColumn(
                name: "UtilisateurId1",
                table: "Operations");

            migrationBuilder.DropColumn(
                name: "CompteId1",
                table: "Ecritures");

            migrationBuilder.DropColumn(
                name: "ClientId1",
                table: "Comptes");

            migrationBuilder.DropColumn(
                name: "CompteId1",
                table: "Cartes");

            migrationBuilder.DropColumn(
                name: "AgenceId1",
                table: "AuditLog");

            migrationBuilder.DropColumn(
                name: "UtilisateurId1",
                table: "AuditLog");

            migrationBuilder.DropColumn(
                name: "OperationId1",
                table: "AlertesAML");

            migrationBuilder.DropColumn(
                name: "TraiteParId1",
                table: "AlertesAML");

            migrationBuilder.AlterColumn<int>(
                name: "TypeCompteId",
                table: "Comptes",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "ClientId",
                table: "Comptes",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<bool>(
                name: "EstCompteInterne",
                table: "Comptes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CompensationsAgences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Reference = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AgenceId = table.Column<int>(type: "int", nullable: false),
                    CompteCompensationId = table.Column<int>(type: "int", nullable: false),
                    TotalDebits = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCredits = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Solde = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DateCompensation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "EnCours")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompensationsAgences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompensationsAgences_Agences_AgenceId",
                        column: x => x.AgenceId,
                        principalTable: "Agences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompensationsAgences_Comptes_CompteCompensationId",
                        column: x => x.CompteCompensationId,
                        principalTable: "Comptes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeclarationsSoupcon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Reference = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AlerteId = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    TypeDeclaration = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Contenu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MotifSuspicion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Statut = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Brouillon"),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateTransmission = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateAccuseReception = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RedigeParId = table.Column<int>(type: "int", nullable: true),
                    ReponseCNRF = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeclarationsSoupcon", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeclarationsSoupcon_AlertesAML_AlerteId",
                        column: x => x.AlerteId,
                        principalTable: "AlertesAML",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeclarationsSoupcon_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeclarationsSoupcon_Utilisateurs_RedigeParId",
                        column: x => x.RedigeParId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SeuilsAML",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Montant = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Devise = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "BIF"),
                    TypeOperation = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Toutes"),
                    Actif = table.Column<bool>(type: "bit", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeuilsAML", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VirementsInterAgences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Reference = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    VirementId = table.Column<int>(type: "int", nullable: false),
                    AgenceSourceId = table.Column<int>(type: "int", nullable: false),
                    AgenceDestinationId = table.Column<int>(type: "int", nullable: false),
                    Montant = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Devise = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "BIF"),
                    Statut = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "EnAttente"),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateCompensation = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateReglement = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotifRejet = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ValideParId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VirementsInterAgences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VirementsInterAgences_Agences_AgenceDestinationId",
                        column: x => x.AgenceDestinationId,
                        principalTable: "Agences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VirementsInterAgences_Agences_AgenceSourceId",
                        column: x => x.AgenceSourceId,
                        principalTable: "Agences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VirementsInterAgences_Utilisateurs_ValideParId",
                        column: x => x.ValideParId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VirementsInterAgences_Virements_VirementId",
                        column: x => x.VirementId,
                        principalTable: "Virements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Operations_Type' AND parent_object_id = OBJECT_ID(N'[dbo].[Operations]')) ALTER TABLE [dbo].[Operations] DROP CONSTRAINT [CK_Operations_Type];");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Operations_Type",
                table: "Operations",
                sql: "[TypeOperation] IN ('Depot', 'Retrait', 'Virement', 'VirementInterAgence', 'ReglementCompensation', 'DeblocagePret', 'RemboursementPret', 'Frais', 'Interet')");

            migrationBuilder.CreateIndex(
                name: "IX_CompensationsAgences_AgenceId",
                table: "CompensationsAgences",
                column: "AgenceId");

            migrationBuilder.CreateIndex(
                name: "IX_CompensationsAgences_CompteCompensationId",
                table: "CompensationsAgences",
                column: "CompteCompensationId");

            migrationBuilder.CreateIndex(
                name: "IX_CompensationsAgences_DateCompensation",
                table: "CompensationsAgences",
                column: "DateCompensation");

            migrationBuilder.CreateIndex(
                name: "IX_DeclarationsSoupcon_AlerteId",
                table: "DeclarationsSoupcon",
                column: "AlerteId");

            migrationBuilder.CreateIndex(
                name: "IX_DeclarationsSoupcon_ClientId",
                table: "DeclarationsSoupcon",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_DeclarationsSoupcon_RedigeParId",
                table: "DeclarationsSoupcon",
                column: "RedigeParId");

            migrationBuilder.CreateIndex(
                name: "IX_DeclarationsSoupcon_Reference",
                table: "DeclarationsSoupcon",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeuilsAML_Code",
                table: "SeuilsAML",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_VirementsInterAgences_AgenceDestinationId",
                table: "VirementsInterAgences",
                column: "AgenceDestinationId");

            migrationBuilder.CreateIndex(
                name: "IX_VirementsInterAgences_AgenceSourceId",
                table: "VirementsInterAgences",
                column: "AgenceSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_VirementsInterAgences_Reference",
                table: "VirementsInterAgences",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VirementsInterAgences_Statut",
                table: "VirementsInterAgences",
                column: "Statut");

            migrationBuilder.CreateIndex(
                name: "IX_VirementsInterAgences_ValideParId",
                table: "VirementsInterAgences",
                column: "ValideParId");

            migrationBuilder.CreateIndex(
                name: "IX_VirementsInterAgences_VirementId",
                table: "VirementsInterAgences",
                column: "VirementId");

            migrationBuilder.AddForeignKey(
                name: "FK_Utilisateur_Agences_AgenceId",
                table: "Utilisateur",
                column: "AgenceId",
                principalTable: "Agences",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Utilisateur_Agences_AgenceId",
                table: "Utilisateur");

            migrationBuilder.DropTable(
                name: "CompensationsAgences");

            migrationBuilder.DropTable(
                name: "DeclarationsSoupcon");

            migrationBuilder.DropTable(
                name: "SeuilsAML");

            migrationBuilder.DropTable(
                name: "VirementsInterAgences");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Operations_Type",
                table: "Operations");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Operations_Type",
                table: "Operations",
                sql: "[TypeOperation] IN ('Depot', 'Retrait', 'Virement', 'DeblocagePret', 'RemboursementPret', 'Frais', 'Interet')");

            migrationBuilder.DropColumn(
                name: "EstCompteInterne",
                table: "Comptes");

            migrationBuilder.AddColumn<int>(
                name: "UtilisateurId1",
                table: "Virements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ClientId1",
                table: "VerificationsKYC",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VerifieParId1",
                table: "VerificationsKYC",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UtilisateurId1",
                table: "SessionsUtilisateur",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CompteDebitId1",
                table: "Prets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ValideParId1",
                table: "Prets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AgenceId1",
                table: "Parametres",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UtilisateurId1",
                table: "Operations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CompteId1",
                table: "Ecritures",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TypeCompteId",
                table: "Comptes",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ClientId",
                table: "Comptes",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClientId1",
                table: "Comptes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompteId1",
                table: "Cartes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AgenceId1",
                table: "AuditLog",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UtilisateurId1",
                table: "AuditLog",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OperationId1",
                table: "AlertesAML",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TraiteParId1",
                table: "AlertesAML",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Virements_UtilisateurId1",
                table: "Virements",
                column: "UtilisateurId1");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationsKYC_ClientId1",
                table: "VerificationsKYC",
                column: "ClientId1");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationsKYC_VerifieParId1",
                table: "VerificationsKYC",
                column: "VerifieParId1");

            migrationBuilder.CreateIndex(
                name: "IX_SessionsUtilisateur_UtilisateurId1",
                table: "SessionsUtilisateur",
                column: "UtilisateurId1");

            migrationBuilder.CreateIndex(
                name: "IX_Prets_CompteDebitId1",
                table: "Prets",
                column: "CompteDebitId1");

            migrationBuilder.CreateIndex(
                name: "IX_Prets_ValideParId1",
                table: "Prets",
                column: "ValideParId1");

            migrationBuilder.CreateIndex(
                name: "IX_Parametres_AgenceId1",
                table: "Parametres",
                column: "AgenceId1");

            migrationBuilder.CreateIndex(
                name: "IX_Operations_UtilisateurId1",
                table: "Operations",
                column: "UtilisateurId1");

            migrationBuilder.CreateIndex(
                name: "IX_Ecritures_CompteId1",
                table: "Ecritures",
                column: "CompteId1");

            migrationBuilder.CreateIndex(
                name: "IX_Comptes_ClientId1",
                table: "Comptes",
                column: "ClientId1");

            migrationBuilder.CreateIndex(
                name: "IX_Cartes_CompteId1",
                table: "Cartes",
                column: "CompteId1");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_AgenceId1",
                table: "AuditLog",
                column: "AgenceId1");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_UtilisateurId1",
                table: "AuditLog",
                column: "UtilisateurId1");

            migrationBuilder.CreateIndex(
                name: "IX_AlertesAML_OperationId1",
                table: "AlertesAML",
                column: "OperationId1");

            migrationBuilder.CreateIndex(
                name: "IX_AlertesAML_TraiteParId1",
                table: "AlertesAML",
                column: "TraiteParId1");

            migrationBuilder.AddForeignKey(
                name: "FK_AlertesAML_Operations_OperationId1",
                table: "AlertesAML",
                column: "OperationId1",
                principalTable: "Operations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AlertesAML_Utilisateur_TraiteParId1",
                table: "AlertesAML",
                column: "TraiteParId1",
                principalTable: "Utilisateur",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLog_Agences_AgenceId1",
                table: "AuditLog",
                column: "AgenceId1",
                principalTable: "Agences",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLog_Utilisateur_UtilisateurId1",
                table: "AuditLog",
                column: "UtilisateurId1",
                principalTable: "Utilisateur",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Cartes_Comptes_CompteId1",
                table: "Cartes",
                column: "CompteId1",
                principalTable: "Comptes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Comptes_Clients_ClientId1",
                table: "Comptes",
                column: "ClientId1",
                principalTable: "Clients",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Ecritures_Comptes_CompteId1",
                table: "Ecritures",
                column: "CompteId1",
                principalTable: "Comptes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Operations_Utilisateur_UtilisateurId1",
                table: "Operations",
                column: "UtilisateurId1",
                principalTable: "Utilisateur",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Parametres_Agences_AgenceId1",
                table: "Parametres",
                column: "AgenceId1",
                principalTable: "Agences",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Prets_Comptes_CompteDebitId1",
                table: "Prets",
                column: "CompteDebitId1",
                principalTable: "Comptes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Prets_Utilisateur_ValideParId1",
                table: "Prets",
                column: "ValideParId1",
                principalTable: "Utilisateur",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SessionsUtilisateur_Utilisateur_UtilisateurId1",
                table: "SessionsUtilisateur",
                column: "UtilisateurId1",
                principalTable: "Utilisateur",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Utilisateur_Agences_AgenceId",
                table: "Utilisateur",
                column: "AgenceId",
                principalTable: "Agences",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VerificationsKYC_Clients_ClientId1",
                table: "VerificationsKYC",
                column: "ClientId1",
                principalTable: "Clients",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VerificationsKYC_Utilisateur_VerifieParId1",
                table: "VerificationsKYC",
                column: "VerifieParId1",
                principalTable: "Utilisateur",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Virements_Utilisateur_UtilisateurId1",
                table: "Virements",
                column: "UtilisateurId1",
                principalTable: "Utilisateur",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
