using Microsoft.AspNetCore.Identity; 
using Microsoft.EntityFrameworkCore; 
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models; 
using AkarangaBank.Web.ViewModels; 
 
namespace AkarangaBank.Web.Services; 
 
public class UtilisateurService : IUtilisateurService 
{ 
    private readonly UserManager<ApplicationUser> _userManager; 
    private readonly AkarangaBankContext _context;
 
    public UtilisateurService(UserManager<ApplicationUser> userManager, AkarangaBankContext context) 
    { 
        _userManager = userManager; 
        _context = context;
    } 
 
    public async Task<IEnumerable<ApplicationUser>> GetAllAsync() 
    { 
        // On utilise _userManager.Users (table Utilisateurs) 
        return await _userManager.Users 
            .Include(u => u.Agence) 
            .OrderBy(u => u.NomComplet) 

 
            .ToListAsync(); 
    } 
 
    public async Task<ApplicationUser?> GetByIdAsync(int id) 
    { 
        return await _userManager.Users 
            .Include(u => u.Agence) 
            .FirstOrDefaultAsync(u => u.Id == id); 
    } 
 
    public async Task<(bool Success, string? Error)> 
CreateAsync(UtilisateurCreateViewModel vm) 
    { 
        if (vm.AgenceId <= 0 || !await _context.Agences.AnyAsync(a => a.Id == vm.AgenceId))
            return (false, "L'agence sélectionnée est introuvable. Veuillez en choisir une autre ou créer une agence.");

        if (vm.Role == "client" && vm.ClientId.HasValue &&
            !await _context.Clients.AnyAsync(c => c.Id == vm.ClientId.Value && c.AgenceId == vm.AgenceId))
            return (false, "Le client associé doit appartenir à l'agence sélectionnée.");

        var user = new ApplicationUser 
        { 
            UserName = vm.Email, 
            Email = vm.Email, 
            EmailConfirmed = true, 
            NomComplet = vm.NomComplet, 
            Role = vm.Role, 
            AgenceId = vm.AgenceId, 
            ClientId = vm.Role == "client" ? vm.ClientId : null,
            EstActif = true 
        }; 
 
        var result = await _userManager.CreateAsync(user, vm.Password); 
        if (!result.Succeeded) 
            return (false, string.Join(", ", result.Errors.Select(e => 
e.Description))); 

        if (string.Equals(vm.Role, "admin", StringComparison.OrdinalIgnoreCase))
            await _userManager.SetLockoutEnabledAsync(user, false);
 
        await _userManager.AddToRoleAsync(user, vm.Role); 
        return (true, null); 
    } 
 
    public async Task<(bool Success, string? Error)> ToggleActifAsync(int 
id) 
    { 
        var user = await _userManager.FindByIdAsync(id.ToString()); 
        if (user is null) return (false, "Utilisateur introuvable."); 
 
        user.EstActif = !user.EstActif; 
        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
            return (false, string.Join(", ", update.Errors.Select(e => e.Description)));

        // Invalidate existing Identity cookies when an administrator disables an account.
        var stamp = await _userManager.UpdateSecurityStampAsync(user);
        if (!stamp.Succeeded)
            return (false, string.Join(", ", stamp.Errors.Select(e => e.Description)));

        return (true, null); 
    } 
 
    public async Task<(bool Success, string? Error)> ResetPasswordAsync(int 
id, string newPassword) 
    { 

 
        var user = await _userManager.FindByIdAsync(id.ToString()); 
        if (user is null) return (false, "Utilisateur introuvable."); 
 
        var token = await 
_userManager.GeneratePasswordResetTokenAsync(user); 
        var result = await _userManager.ResetPasswordAsync(user, token, 
newPassword); 
 
        return result.Succeeded 
            ? (true, null) 
            : (false, string.Join(", ", result.Errors.Select(e => 
e.Description))); 
    } 
 
    public async Task<(bool Success, string? Error)> AssignRoleAsync(int 
id, string role) 
    { 
        var user = await _userManager.FindByIdAsync(id.ToString()); 
        if (user is null) return (false, "Utilisateur introuvable."); 
 
        var currentRoles = await _userManager.GetRolesAsync(user); 
        await _userManager.RemoveFromRolesAsync(user, currentRoles); 
        await _userManager.AddToRoleAsync(user, role); 
        user.Role = role; 
        await _userManager.UpdateAsync(user); 
 
        return (true, null); 
    } 

    public async Task<(bool Success, string? Error)> AssocierClientAsync(int userId, int? clientId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return (false, "Utilisateur introuvable.");
        if (user.Role != "client") return (false, "Seuls les utilisateurs ayant le rôle client peuvent être associés à un dossier client.");
        if (clientId.HasValue && !await _context.Clients.AnyAsync(c => c.Id == clientId.Value && c.AgenceId == user.AgenceId))
            return (false, "Le client doit appartenir à l'agence de l'utilisateur.");

        user.ClientId = clientId;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded ? (true, null) : (false, string.Join(", ", result.Errors.Select(e => e.Description)));
    }
}
