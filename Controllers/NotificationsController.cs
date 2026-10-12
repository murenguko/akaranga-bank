using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AkarangaBank.Web.Services;
using AkarangaBank.Web.ViewModels;

namespace AkarangaBank.Web.Controllers;

[Authorize(Policy = "StaffReadOnly")]
public class NotificationsController : Controller
{
    private readonly INotificationService _notificationService;
    private readonly INotificationTemplateService _templateService;
    private readonly ICurrentUserService _currentUser;

    public NotificationsController(
        INotificationService notificationService,
        INotificationTemplateService templateService,
        ICurrentUserService currentUser)
    {
        _notificationService = notificationService;
        _templateService = templateService;
        _currentUser = currentUser;
    }

    // GET: /Notifications
    public async Task<IActionResult> Index(string? statut, string? typeNotification, int page = 1)
    {
        const int taille = 15;
        var vm = await _notificationService.GetPagedAsync(statut, typeNotification, page, taille);
        return View(vm);
    }

    // GET: /Notifications/Templates
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Templates()
    {
        var templates = await _templateService.GetAllAsync();
        return View(templates);
    }

    // POST: /Notifications/InitialiserTemplates
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> InitialiserTemplates()
    {
        await _templateService.InitialiserTemplatesParDefautAsync();
        TempData["Success"] = "Templates de notifications initialisés.";
        return RedirectToAction(nameof(Templates));
    }

    // GET: /Notifications/Tester
    [Authorize(Policy = "AdminOnly")]
    public IActionResult Tester()
    {
        return View(new NotificationSendTestViewModel());
    }

    // POST: /Notifications/Tester
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Tester(NotificationSendTestViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        int userId = await _currentUser.GetUserIdAsync();
        var notif = await _notificationService.EnvoyerNotificationAsync(
            vm.TypeNotification,
            vm.Destinataire,
            vm.Sujet,
            vm.Message,
            templateCode: null,
            clientId: vm.ClientId,
            utilisateurId: userId
        );

        if (notif.Statut == "Envoye")
        {
            TempData["Success"] = $"Notification {vm.TypeNotification} transmise avec succès à {vm.Destinataire}.";
        }
        else
        {
            TempData["Error"] = $"Échec de l'envoi : {notif.DerniereErreur ?? "Erreur de communication."}";
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: /Notifications/Reessayer/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "GestionnaireOrAdmin")]
    public async Task<IActionResult> ReessayerTous()
    {
        int count = await _notificationService.ReessayerNotificationsEchoueesAsync();
        TempData["Success"] = $"{count} notification(s) renvoyée(s) avec succès.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Notifications/MarquerLue/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarquerLue(int id)
    {
        await _notificationService.MarquerCommeLueAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
