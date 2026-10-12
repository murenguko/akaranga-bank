using System.Globalization;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Data.Interceptors;
using AkarangaBank.Web.Hubs;
using AkarangaBank.Web.Models;
using AkarangaBank.Web.Services;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("AkarangaBank");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Database connection string is missing. Set ConnectionStrings__AkarangaBank to a PostgreSQL connection string.");
}

// --- Localisation (Chapitre 13) ---
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var frenchCulture = new CultureInfo("fr-BI");
    options.DefaultRequestCulture = new RequestCulture("fr-BI");
    options.SupportedCultures = new[] { frenchCulture };
    options.SupportedUICultures = new[] { frenchCulture };
    options.RequestCultureProviders.Clear();
});

// --- SignalR (Chapitre 14) ---
builder.Services.AddSignalR();

// --- Hangfire (Chapitre 14) ---
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseMemoryStorage());
builder.Services.AddHangfireServer();

// --- Base de données & Audit ---
builder.Services.AddHttpContextAccessor(); 
builder.Services.AddScoped<AuditInterceptor>(); 

builder.Services.AddDbContext<AkarangaBankContext>((sp, options) => 
{ 
    var interceptor = sp.GetRequiredService<AuditInterceptor>(); 
    options.UseNpgsql(connectionString) 
        .AddInterceptors(interceptor); 
});

builder.Services.AddHealthChecks().AddDbContextCheck<AkarangaBankContext>("database"); 

// --- Identity & Sécurité Bancaire ---
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true; 
    options.Password.RequireNonAlphanumeric = true; 

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 3;
    options.Lockout.AllowedForNewUsers = true;

    options.SignIn.RequireConfirmedEmail = false; 
    options.SignIn.RequireConfirmedAccount = false; 

    options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider; 
    options.Tokens.AuthenticatorIssuer = "AkarangaBank"; 
})
.AddRoles<ApplicationRole>()
.AddEntityFrameworkStores<AkarangaBankContext>();

builder.Services.ConfigureApplicationCookie(options => 
{ 
    options.LoginPath = "/Identity/Account/Login"; 
    options.LogoutPath = "/Identity/Account/Logout"; 
    options.AccessDeniedPath = "/Identity/Account/AccessDenied"; 
 
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60); 
    options.SlidingExpiration = true; 
 
    options.Cookie.HttpOnly = true; 
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; 
    options.Cookie.SameSite = SameSiteMode.Lax; 
    options.EventsType = typeof(SessionCookieEvents);
}); 
builder.Services.AddScoped<SessionCookieEvents>();

// --- Services Métier --- 
builder.Services.AddScoped<IAgenceService, AgenceService>();
builder.Services.AddScoped<IComptabiliteService, ComptabiliteService>();
builder.Services.AddScoped<IConformiteService, ConformiteService>();
builder.Services.AddScoped<ISurveillanceService, SurveillanceService>();
builder.Services.AddScoped<IDeclarationService, DeclarationService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ICompensationService, CompensationService>();
builder.Services.AddScoped<IVirementInterAgenceService, VirementInterAgenceService>();
builder.Services.AddScoped<ICNRFService, CNRFService>();
builder.Services.AddScoped<ISeuilAMLService, SeuilAMLService>();
builder.Services.AddScoped<IDetectionAvanceeService, DetectionAvanceeService>(); 
builder.Services.AddScoped<IUtilisateurService, UtilisateurService>(); 
builder.Services.AddSingleton<IQrCodeService, QrCodeService>();

// Chapitre 12 : Prêts et Cartes
builder.Services.AddScoped<IPretService, PretService>();
builder.Services.AddScoped<ICarteService, CarteService>();

// Chapitre 13 : Internationalisation
builder.Services.AddScoped<ILocalizationService, LocalizationService>();

// Chapitre 14 : Gestion documentaire & Notifications
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<INotificationTemplateService, NotificationTemplateService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHostedService<NotificationBackgroundJob>();

// Chapitre 15 : Génération PDF, Excel, CSV et Reporting
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<IExcelService, ExcelService>();
builder.Services.AddScoped<ICsvService, CsvService>();
builder.Services.AddScoped<IReportService, ReportService>();

// --- Politiques d'autorisation --- 
builder.Services.AddSingleton<IAuthorizationHandler, RequireMfaHandler>(); 
builder.Services.AddAuthorization(options => 
{ 
    options.AddPolicy("RequireMfa", policy => 
        policy.Requirements.Add(new RequireMfa())); 
 
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireRole("admin")); 

    options.AddPolicy("StaffReadOnly", policy =>
        policy.RequireRole("admin", "gestionnaire", "guichetier"));

    options.AddPolicy("BankingStaff", policy =>
        policy.RequireRole("admin", "gestionnaire", "guichetier"));

    options.AddPolicy("ClientAccountReaders", policy =>
        policy.RequireRole("admin", "gestionnaire", "guichetier", "auditeur", "client"));

    options.AddPolicy("AmlReaders", policy =>
        policy.RequireRole("admin", "gestionnaire", "auditeur"));

    options.AddPolicy("ClientOrBankingStaff", policy =>
        policy.RequireRole("admin", "gestionnaire", "guichetier", "client"));

    options.AddPolicy("PersonalOrStaffTransfer", policy =>
        policy.RequireRole("admin", "gestionnaire", "guichetier", "client"));

    options.AddPolicy("FinancialReports", policy =>
        policy.RequireRole("admin"));

    options.AddPolicy("AuditeurOnly", policy =>
        policy.RequireRole("admin", "auditeur"));
 
    options.AddPolicy("GestionnaireOrAdmin", policy => 
        policy.RequireRole("gestionnaire", "admin"));

    options.AddPolicy("GuichetierOnly", policy =>
        policy.RequireRole("guichetier", "gestionnaire", "admin")); 
});

// --- Protection globale --- 
builder.Services.AddControllersWithViews(options => 
{ 
    var policy = new AuthorizationPolicyBuilder() 
        .RequireAuthenticatedUser() 
        .Build(); 
    options.Filters.Add(new AuthorizeFilter(policy)); 
})
.AddViewLocalization(Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat.Suffix)
.AddDataAnnotationsLocalization();

builder.Services.AddRazorPages(options => 
{ 
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/Login"); 
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/Register");
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/Logout"); 
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/AccessDenied"); 
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/LoginWith2fa"); 
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/LoginWithRecoveryCode"); 
});

var app = builder.Build();

// Pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Configuration de la localisation
var locOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(locOptions);

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Hangfire Dashboard (TM-14.9)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    DashboardTitle = "Akaranga Bank - Tâches d'arrière-plan",
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

// SignalR Hub (EF-14.13)
app.MapHub<NotificationHub>("/notificationHub");

app.MapStaticAssets();

app.MapHealthChecks("/health"); 
app.MapHealthChecks("/health/ready"); 

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages();

app.Run();

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true && httpContext.User.IsInRole("admin");
    }
}

public partial class Program { }

