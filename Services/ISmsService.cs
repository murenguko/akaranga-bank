namespace AkarangaBank.Web.Services;

public interface ISmsService
{
    Task<bool> EnvoyerSmsAsync(string numeroTelephone, string message);
}
