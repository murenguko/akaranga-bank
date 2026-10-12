using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
namespace AkarangaBank.Web.Services;
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor; private readonly AkarangaBankContext _context;
    public CurrentUserService(IHttpContextAccessor accessor, AkarangaBankContext context) { _accessor = accessor; _context = context; }
    private int UserId => int.TryParse(_accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : throw new InvalidOperationException("Utilisateur authentifié introuvable.");
    public Task<int> GetUserIdAsync() => Task.FromResult(UserId);
    public async Task<int> GetAgenceIdAsync()
    {
        var id = await _context.Users.AsNoTracking().Where(u => u.Id == UserId).Select(u => u.AgenceId).FirstOrDefaultAsync();
        return id > 0 ? id : throw new InvalidOperationException("Agence de l'utilisateur introuvable.");
    }
}
