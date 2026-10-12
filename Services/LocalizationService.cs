using Microsoft.Extensions.Localization;
using AkarangaBank.Web.Resources;

namespace AkarangaBank.Web.Services;

public interface ILocalizationService
{
    string Get(string key);
    string this[string key] { get; }
}

public class LocalizationService : ILocalizationService
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public LocalizationService(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;
    }

    public string Get(string key)
    {
        var localized = _localizer[key];
        return localized.ResourceNotFound ? key : localized.Value;
    }

    public string this[string key] => Get(key);
}
