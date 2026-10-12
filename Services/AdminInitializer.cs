using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AkarangaBank.Web.Services;

public static class AdminInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, bool seedDemoUsers)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var context = services.GetRequiredService<AkarangaBankContext>();

        string[] roles = { "admin", "gestionnaire", "auditeur", "guichetier", "client" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new ApplicationRole { Name = role });
                EnsureSucceeded(result, $"creating role '{role}'");
            }
        }

        await SeedTypeComptesAsync(context);

        if (!seedDemoUsers)
            return;

        var siege = await context.Agences.FirstOrDefaultAsync(a => a.EstSiege);
        if (siege is null)
        {
            siege = await context.Agences.OrderBy(a => a.Id).FirstOrDefaultAsync();
            if (siege is null)
            {
                siege = new Agence
                {
                    Code = "AKR-BJM",
                    Nom = "Akaranga Bank Bujumbura (Siège)",
                    Ville = "Bujumbura",
                    EstSiege = true
                };
                context.Agences.Add(siege);
            }
            else
            {
                siege.EstSiege = true;
            }

            await context.SaveChangesAsync();
        }

        var demoUsers = new[]
        {
            (Email: "admin@akaranga.bi", Name: "Administrateur", Role: "admin"),
            (Email: "gestionnaire@akaranga.bi", Name: "Gestionnaire", Role: "gestionnaire"),
            (Email: "guichetier@akaranga.bi", Name: "Guichetier", Role: "guichetier")
        };

        foreach (var demoUser in demoUsers)
        {
            var user = await userManager.FindByEmailAsync(demoUser.Email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = demoUser.Email,
                    Email = demoUser.Email,
                    EmailConfirmed = true,
                    NomComplet = demoUser.Name,
                    Role = demoUser.Role,
                    AgenceId = siege.Id,
                    EstActif = true
                };

                var createResult = await userManager.CreateAsync(user, "Administrateur@2025!");
                EnsureSucceeded(createResult, $"creating demo user '{demoUser.Email}'");
            }
            else if (await userManager.CheckPasswordAsync(user, "Password123!"))
            {
                // Migrate accounts seeded by the previous development default once,
                // without resetting a developer's subsequently changed password.
                var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await userManager.ResetPasswordAsync(user, resetToken, "Administrateur@2025!");
                EnsureSucceeded(resetResult, $"updating the legacy demo password for '{demoUser.Email}'");
            }

            if (!await userManager.IsInRoleAsync(user, demoUser.Role))
            {
                var roleResult = await userManager.AddToRoleAsync(user, demoUser.Role);
                EnsureSucceeded(roleResult, $"assigning role '{demoUser.Role}' to '{demoUser.Email}'");
            }

            if (string.Equals(demoUser.Role, "admin", StringComparison.OrdinalIgnoreCase))
            {
                if (user.LockoutEnabled)
                {
                    EnsureSucceeded(await userManager.SetLockoutEndDateAsync(user, null),
                        $"clearing lockout for '{demoUser.Email}'");
                }
                EnsureSucceeded(await userManager.ResetAccessFailedCountAsync(user),
                    $"resetting failed access count for '{demoUser.Email}'");
                if (user.LockoutEnabled)
                {
                    EnsureSucceeded(await userManager.SetLockoutEnabledAsync(user, false),
                        $"disabling lockout for '{demoUser.Email}'");
                }
            }
        }
    }

    private static async Task SeedTypeComptesAsync(AkarangaBankContext context)
    {
        var typesParDefaut = new[]
        {
            new TypeCompte { Code = "COURANT", Libelle = "Compte Courant", TauxInteret = 0m, SoldeMinimum = 0m, AutoriseDecouvert = false },
            new TypeCompte { Code = "EPARGNE", Libelle = "Compte Épargne", TauxInteret = 3.50m, SoldeMinimum = 10_000m, AutoriseDecouvert = false },
            new TypeCompte { Code = "CREDIT", Libelle = "Compte Crédit", TauxInteret = 12m, SoldeMinimum = 0m, AutoriseDecouvert = true }
        };

        var codesExistants = await context.TypesCompte
            .Select(type => type.Code)
            .ToListAsync();
        var codesExistantsSet = codesExistants.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var typesManquants = typesParDefaut
            .Where(type => !codesExistantsSet.Contains(type.Code))
            .ToList();

        if (typesManquants.Count == 0)
            return;

        context.TypesCompte.AddRange(typesManquants);
        await context.SaveChangesAsync();
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Failed while {operation}: {errors}");
        }
    }
}
