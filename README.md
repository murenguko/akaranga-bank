# Akaranga Bank

Application web de gestion bancaire réalisée avec ASP.NET Core. Elle regroupe notamment la gestion des clients, des comptes, des opérations, des prêts, des cartes et des documents, ainsi que des fonctions de conformité, de reporting et de notifications.

## Prérequis

- SDK .NET 10.0 ou supérieur.
- SQL Server 2022 ou supérieur, installé localement ou accessible sur le réseau.
- Outil Entity Framework Core en ligne de commande (`dotnet-ef`) pour appliquer les migrations :

  ```powershell
  dotnet tool install --global dotnet-ef --version 10.0.12
  ```

## Installation

1. Clonez le dépôt, puis ouvrez un terminal dans le dossier contenant `AkarangaBank.Web.csproj`.
2. Restaurez les dépendances NuGet :

   ```powershell
   dotnet restore
   ```

3. Configurez la chaîne de connexion SQL Server sous la clé `ConnectionStrings:AkarangaBank`. Pour le développement, utilisez les secrets utilisateur afin de ne pas enregistrer de mot de passe dans les fichiers suivis par Git :

   ```powershell
   dotnet user-secrets set "ConnectionStrings:AkarangaBank" "Server=localhost,1433;Database=AkarangaBank;User Id=sa;Password=VotreMotDePasse;TrustServerCertificate=True;Encrypt=False"
   ```

   Adaptez le serveur, l’authentification et le mot de passe à votre installation SQL Server. Les paramètres SMTP, Twilio et Firebase sont facultatifs et peuvent être configurés de la même manière selon les fonctionnalités utilisées.

4. Créez la base et appliquez les migrations :

   ```powershell
   dotnet ef database update
   ```

   Le fichier `AkarangaBank-Script-DB.sql` est également fourni comme script de référence pour SQL Server. N’exécutez pas ce script sur une base contenant des données à conserver : il commence par supprimer la base `AkarangaBank` si elle existe.

## Lancement

Depuis le dossier du projet, lancez l’application :

```powershell
dotnet run
```

Ouvrez ensuite l’adresse indiquée dans la sortie de la commande, généralement `http://localhost:5000` ou `https://localhost:5001`. L’application nécessite une connexion valide à SQL Server.

## Structure du projet

| Dossier ou fichier | Rôle |
| --- | --- |
| `Areas/` | Zones ASP.NET Core, dont les pages d’identité |
| `Controllers/` | Contrôleurs MVC et points d’entrée HTTP |
| `Data/` | Contexte Entity Framework Core et accès aux données |
| `Migrations/` | Migrations de schéma de base de données |
| `Models/` | Modèles métier et entités |
| `ViewModels/` | Modèles destinés aux vues et formulaires |
| `Views/` | Vues Razor de l’interface web |
| `Services/` | Services applicatifs et logique métier |
| `Hubs/` | Hubs SignalR pour les notifications en temps réel |
| `Resources/` | Ressources de localisation |
| `Storage/` | Stockage de documents de l’application |
| `wwwroot/` | Fichiers statiques : CSS, JavaScript et bibliothèques |
| `Program.cs` | Configuration et démarrage de l’application |
| `AkarangaBank.Web.csproj` | Cible .NET et dépendances NuGet |

## Technologies utilisées

- C# et ASP.NET Core MVC avec Razor Pages.
- .NET 10 et ASP.NET Core Identity.
- Entity Framework Core et SQL Server.
- SignalR pour les notifications en temps réel.
- Hangfire pour les tâches d’arrière-plan.
- QuestPDF, ClosedXML et CsvHelper pour les documents et exports.
- MailKit, Twilio et Firebase pour les intégrations de notification.

## DÃ©ploiement avec Docker Compose

Le conteneur Web utilise .NET 10, Ã©coute sur le port `8080` et s'exÃ©cute sans privilÃ¨ges root. SQL Server reste sur le rÃ©seau Docker interne. Les donnÃ©es SQL et les documents tÃ©lÃ©versÃ©s sont conservÃ©s dans des volumes Docker.

1. Copiez `.env.example` vers `.env` et remplacez `MSSQL_SA_PASSWORD` par un mot de passe fort et unique. Ne publiez pas le fichier `.env`.
2. Construisez et dÃ©marrez les services :

   ```sh
   docker compose up --build -d
   ```

L'application est disponible sur le port `8080` de l'hÃ´te. En production, placez-la derriÃ¨re un proxy inverse qui termine TLS. ProtÃ©gez et sauvegardez les volumes `mssql_data` et `document_data`; un volume Docker seul n'est pas une sauvegarde.

Compose active l'application automatique des migrations Entity Framework au dÃ©marrage, avant l'initialisation des donnÃ©es. Utilisez une seule instance Web pour exÃ©cuter les migrations lors des dÃ©ploiements.

## Licence

Aucune licence n’est spécifiée dans le dépôt. Les conditions de réutilisation et de distribution restent à définir par les auteurs.

## Auteurs

- Équipe Akaranga Bank — Murenguko Jesus Derrick.
