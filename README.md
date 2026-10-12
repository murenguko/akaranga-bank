# Akaranga Bank

Application web de gestion bancaire réalisée avec ASP.NET Core. Elle regroupe notamment la gestion des clients, des comptes, des opérations, des prêts, des cartes et des documents, ainsi que des fonctions de conformité, de reporting et de notifications.

## Prérequis

- SDK .NET 10.0 ou supérieur.
- PostgreSQL 16 ou supérieur, installé localement ou accessible sur le réseau.
## Installation

1. Clonez le dépôt, puis ouvrez un terminal dans le dossier contenant `AkarangaBank.Web.csproj`.
2. Restaurez les dépendances NuGet :

   ```powershell
   dotnet restore
   ```

3. Configurez la chaîne de connexion PostgreSQL sous la clé `ConnectionStrings:AkarangaBank`. Pour le développement, utilisez les secrets utilisateur afin de ne pas enregistrer de mot de passe dans les fichiers suivis par Git :

   ```powershell
   dotnet user-secrets set "ConnectionStrings:AkarangaBank" "Host=localhost;Port=5432;Database=akarangabank;Username=akaranga_app;Password=VotreMotDePasse"
   ```

   Adaptez l’hôte, la base, l’utilisateur et le mot de passe à votre installation PostgreSQL. Les paramètres SMTP, Twilio et Firebase sont facultatifs et peuvent être configurés de la même manière selon les fonctionnalités utilisées.

4. Préparez la base PostgreSQL avec les tables nécessaires aux fonctionnalités utilisées. Le script `AkarangaBank-Script-DB-PostgreSQL.sql` contient les données métier de référence, mais ne crée pas la table Identity `Roles`.

## Lancement

Depuis le dossier du projet, lancez l’application :

```powershell
dotnet run
```

Ouvrez ensuite l’adresse indiquée dans la sortie de la commande, généralement `http://localhost:5000` ou `https://localhost:5001`. L’application nécessite une connexion valide à PostgreSQL.

## Déploiement sur Railway

Ajoutez au service web la variable suivante, en liant les valeurs à votre service PostgreSQL :

- `ConnectionStrings__AkarangaBank` : `Host=${{Postgres.PGHOST}};Port=${{Postgres.PGPORT}};Database=${{Postgres.PGDATABASE}};Username=${{Postgres.PGUSER}};Password=${{Postgres.PGPASSWORD}}` (adaptez `Postgres` au nom du service).

L’application ne crée pas de tables et ne peuple pas la base au démarrage. Les fonctionnalités Identity exigent les tables correspondantes, notamment `Roles`.

### Raccorder la base déjà peuplée par le script PostgreSQL

Le script initial crée des noms non cités, donc PostgreSQL les stocke en minuscules (`utilisateurs`, `agences`, etc.). L'application utilise les noms cités et les colonnes d'ASP.NET Identity. Exécutez une seule fois `Railway-PostgreSQL-Bridge.sql` dans **la base à laquelle le service Web se connecte**. Le script crée les tables de l'application, copie les données avec les mêmes identifiants et laisse les tables d'origine intactes. Il s'arrête si les tables d'origine manquent ou si les tables de l'application existent déjà. Faites une sauvegarde avant l'exécution.

Dans PowerShell, avec une URL de connexion publique à cette base enregistrée dans une variable d'environnement locale :

```powershell
psql "$env:RAILWAY_POSTGRES_URL" -v ON_ERROR_STOP=1 -f .\Railway-PostgreSQL-Bridge.sql
```

Le script affiche les nombres de lignes copiées après le commit. Vérifiez que chaque paire est identique. Après cette copie, l'application écrit dans les nouvelles tables aux noms cités ; les anciennes tables ne sont plus synchronisées. Les mots de passe factices du script initial ne sont pas des hachages ASP.NET Identity utilisables. Les utilisateurs importés restent inactifs jusqu'à la définition d'un nouveau mot de passe. Leurs adresses reprennent `username` quand celui-ci est déjà une adresse ; autrement, le script ajoute `@akaranga.bi`. Vérifiez ces adresses :

```sql
SELECT "Id", "Email", "Role", "EstActif" FROM "Utilisateurs" ORDER BY "Id";
```

Pour activer un compte, générez localement une commande SQL avec un mot de passe choisi par vous. Le mot de passe est saisi sans affichage et n'apparaît pas dans la commande PowerShell :

```powershell
dotnet run --project tools/IdentityPasswordSql -- admin_bjm@akaranga.bi
```

Exécutez ensuite le `UPDATE` affiché dans la même base Railway. Répétez pour les comptes nécessaires. Si l'adresse doit changer, corrigez `"UserName"`, `"NormalizedUserName"`, `"Email"` et `"NormalizedEmail"` ensemble avant d'utiliser l'outil.

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
- Entity Framework Core et PostgreSQL.
- SignalR pour les notifications en temps réel.
- Hangfire pour les tâches d’arrière-plan.
- QuestPDF, ClosedXML et CsvHelper pour les documents et exports.
- MailKit, Twilio et Firebase pour les intégrations de notification.

## DÃ©ploiement avec Docker Compose

Le conteneur Web utilise .NET 10, Ã©coute sur le port `8080` et s'exÃ©cute sans privilÃ¨ges root. PostgreSQL reste sur le rÃ©seau Docker interne. Les donnÃ©es PostgreSQL et les documents tÃ©lÃ©versÃ©s sont conservÃ©s dans des volumes Docker.

1. Copiez `.env.example` vers `.env` et remplacez `POSTGRES_PASSWORD` par un mot de passe fort et unique. Ne publiez pas le fichier `.env`.
2. Construisez et dÃ©marrez les services :

   ```sh
   docker compose up --build -d
   ```

L'application est disponible sur le port `8080` de l'hÃ´te. En production, placez-la derriÃ¨re un proxy inverse qui termine TLS. ProtÃ©gez et sauvegardez les volumes `postgres_data` et `document_data`; un volume Docker seul n'est pas une sauvegarde.

Compose ne lance ni migration ni initialisation des données au démarrage. Préparez la base avant de démarrer le service Web.

## Licence

Aucune licence n’est spécifiée dans le dépôt. Les conditions de réutilisation et de distribution restent à définir par les auteurs.

## Auteurs

- Équipe Akaranga Bank — Murenguko Jesus Derrick.
