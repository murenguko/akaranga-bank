namespace AkarangaBank.Web.Services;

public interface IPushNotificationService
{
    Task<bool> EnvoyerPushAsync(string tokenOuUserId, string titre, string message);
}

public class PushNotificationService : IPushNotificationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PushNotificationService> _logger;

    public PushNotificationService(IConfiguration configuration, ILogger<PushNotificationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<bool> EnvoyerPushAsync(string tokenOuUserId, string titre, string message)
    {
        // Simulation / Support Firebase Cloud Messaging (FCM)
        _logger.LogInformation("[NOTIFICATION PUSH] Destinataire: {Cible} | Titre: {Titre} | Message: {Message}",
            tokenOuUserId, titre, message);
        return Task.FromResult(true);
    }
}
