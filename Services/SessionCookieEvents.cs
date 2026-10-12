using System.Security.Claims;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AkarangaBank.Web.Services;

public sealed class SessionCookieEvents : CookieAuthenticationEvents
{
    private const string SessionClaim = "akaranga:session_id";
    private readonly AkarangaBankContext _db;

    public SessionCookieEvents(AkarangaBankContext db) => _db = db;

    public override async Task SigningIn(CookieSigningInContext context)
    {
        var principal = context.Principal;
        if (principal is null || !int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return;

        var identity = principal.Identities.FirstOrDefault(i => i.IsAuthenticated);
        if (identity is null) return;

        var token = principal.FindFirstValue(SessionClaim);
        if (string.IsNullOrWhiteSpace(token))
        {
            token = Guid.NewGuid().ToString("N");
            identity.AddClaim(new Claim(SessionClaim, token));
        }

        var now = DateTime.UtcNow;
        var expires = context.Properties.ExpiresUtc?.UtcDateTime ?? now.Add(context.Options.ExpireTimeSpan);
        var session = await _db.SessionsUtilisateur.FirstOrDefaultAsync(s => s.Token == token);
        if (session is null)
        {
            var request = context.HttpContext.Request;
            var userAgent = request.Headers.UserAgent.ToString();
            session = new SessionUtilisateur
            {
                Token = token,
                UtilisateurId = userId,
                DateConnexion = now,
                AdresseIP = request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = userAgent[..Math.Min(userAgent.Length, 255)],
                DeuxFA_Valide = principal.FindFirst("amr")?.Value == "mfa"
            };
            _db.SessionsUtilisateur.Add(session);
        }

        session.EstActive = true;
        session.DateExpiration = expires;
        await _db.SaveChangesAsync();
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var token = context.Principal?.FindFirstValue(SessionClaim);
        if (string.IsNullOrWhiteSpace(token))
        {
            context.RejectPrincipal();
            return;
        }

        var now = DateTime.UtcNow;
        var session = await _db.SessionsUtilisateur
            .Include(s => s.Utilisateur)
            .FirstOrDefaultAsync(s => s.Token == token);
        if (session is null || !session.EstActive || session.DateExpiration <= now || !session.Utilisateur.EstActif)
        {
            if (session is not null && session.EstActive)
            {
                session.EstActive = false;
                await _db.SaveChangesAsync();
            }
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return;
        }

        var changed = false;
        var mfaWasUsed = context.Principal?.FindFirst("amr")?.Value == "mfa";
        if (session.DeuxFA_Valide != mfaWasUsed)
        {
            session.DeuxFA_Valide = mfaWasUsed;
            changed = true;
        }
        if (session.Utilisateur.DeuxFA_Active != session.Utilisateur.TwoFactorEnabled)
        {
            session.Utilisateur.DeuxFA_Active = session.Utilisateur.TwoFactorEnabled;
            changed = true;
        }
        if (context.ShouldRenew)
        {
            session.DateExpiration = now.Add(context.Options.ExpireTimeSpan);
            changed = true;
        }
        if (changed) await _db.SaveChangesAsync();
    }

    public override async Task SigningOut(CookieSigningOutContext context)
    {
        var token = context.HttpContext.User.FindFirstValue(SessionClaim);
        if (string.IsNullOrWhiteSpace(token)) return;

        var session = await _db.SessionsUtilisateur.FirstOrDefaultAsync(s => s.Token == token);
        if (session is null) return;

        session.EstActive = false;
        await _db.SaveChangesAsync();
    }
}
