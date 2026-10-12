using System.ComponentModel.DataAnnotations;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AkarangaBank.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly AkarangaBankContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public RegisterModel(
        AkarangaBankContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IList<SelectListItem> Agences { get; private set; } = [];

    public string? ReturnUrl { get; private set; }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        await LoadAgencesAsync();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        // Validate the foreign key against the database before Identity attempts its INSERT.
        var agenceExists = await _context.Agences.AnyAsync(agence => agence.Id == Input.AgenceId);
        if (!agenceExists)
        {
            ModelState.AddModelError("Input.AgenceId", "Veuillez sélectionner une agence valide.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAgencesAsync();
            return Page();
        }

        var user = new ApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            NomComplet = Input.NomComplet.Trim(),
            Role = "client",
            AgenceId = Input.AgenceId,
            EstActif = true
        };

        var result = await _userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await LoadAgencesAsync();
            return Page();
        }

        var roleResult = await _userManager.AddToRoleAsync(user, "client");
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            foreach (var error in roleResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await LoadAgencesAsync();
            return Page();
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Content("~/"));
    }

    private async Task LoadAgencesAsync()
    {
        Agences = await _context.Agences
            .AsNoTracking()
            .OrderByDescending(agence => agence.EstSiege)
            .ThenBy(agence => agence.Nom)
            .Select(agence => new SelectListItem
            {
                Value = agence.Id.ToString(),
                Text = $"{agence.Nom} — {agence.Ville}",
                Selected = agence.Id == Input.AgenceId
            })
            .ToListAsync();
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Le nom complet est obligatoire.")]
        [StringLength(100)]
        [Display(Name = "Nom complet")]
        public string NomComplet { get; set; } = string.Empty;

        [Required(ErrorMessage = "L’adresse e-mail est obligatoire.")]
        [EmailAddress(ErrorMessage = "Veuillez saisir une adresse e-mail valide.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
        [StringLength(100, ErrorMessage = "Le {0} doit comporter au moins {2} caractères.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirmer le mot de passe")]
        [Compare(nameof(Password), ErrorMessage = "Les mots de passe ne correspondent pas.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Veuillez sélectionner une agence.")]
        [Display(Name = "Agence")]
        public int AgenceId { get; set; }
    }
}
