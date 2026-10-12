using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace AkarangaBank.Web.Services;

public class SmsService : ISmsService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmsService> _logger;

    public SmsService(IConfiguration configuration, ILogger<SmsService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> EnvoyerSmsAsync(string numeroTelephone, string message)
    {
        if (string.IsNullOrWhiteSpace(numeroTelephone))
            return false;

        string? accountSid = _configuration["Twilio:AccountSid"];
        string? authToken = _configuration["Twilio:AuthToken"];
        string? fromNumber = _configuration["Twilio:FromPhoneNumber"] ?? "+15005550006";

        if (string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken))
        {
            _logger.LogInformation("[SIMULATION SMS] Vers: {Numero} | Message: {Message}", numeroTelephone, message);
            return true;
        }

        try
        {
            TwilioClient.Init(accountSid, authToken);
            var msg = await MessageResource.CreateAsync(
                body: message,
                from: new PhoneNumber(fromNumber),
                to: new PhoneNumber(numeroTelephone)
            );

            _logger.LogInformation("SMS Twilio envoyé avec succès (SID: {Sid})", msg.Sid);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'envoi du SMS Twilio à {Numero}", numeroTelephone);
            return false;
        }
    }
}
