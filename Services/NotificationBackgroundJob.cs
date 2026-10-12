using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AkarangaBank.Web.Services;

public class NotificationBackgroundJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationBackgroundJob> _logger;

    public NotificationBackgroundJob(
        IServiceProvider serviceProvider,
        ILogger<NotificationBackgroundJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Service de tâches d'arrière-plan de notifications démarré.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var notifService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                var docService = scope.ServiceProvider.GetRequiredService<IDocumentService>();

                // 1. Réessayer les notifications échouées
                int retryCount = await notifService.ReessayerNotificationsEchoueesAsync();
                if (retryCount > 0)
                {
                    _logger.LogInformation("{Count} notifications en échec ont été renvoyées avec succès.", retryCount);
                }

                // 2. Envoyer les rappels d'échéance à J-3
                int rappelsCount = await notifService.EnvoyerRappelsEcheanceAsync();
                if (rappelsCount > 0)
                {
                    _logger.LogInformation("{Count} rappels d'échéance de prêt envoyés.", rappelsCount);
                }

                // 3. Purger les documents supprimés depuis plus de 30 jours
                int purgeCount = await docService.PurgerDocumentsSupprimesAsync(30);
                if (purgeCount > 0)
                {
                    _logger.LogInformation("{Count} documents archivés/supprimés ont été purgés définitivement.", purgeCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur dans l'exécution de la boucle d'arrière-plan.");
            }

            // Attendre 1 heure entre chaque passage en tâche de fond
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    // Méthodes invocables directement par Hangfire si planifié
    public async Task ExecuterTachesHangfireAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var notifService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var docService = scope.ServiceProvider.GetRequiredService<IDocumentService>();

        await notifService.ReessayerNotificationsEchoueesAsync();
        await notifService.EnvoyerRappelsEcheanceAsync();
        await docService.PurgerDocumentsSupprimesAsync(30);
    }
}
