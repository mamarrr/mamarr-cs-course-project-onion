using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

namespace WebApp.UI.Culture;

public sealed class CultureOptionsBuilder : ICultureOptionsBuilder
{
    private readonly IOptions<RequestLocalizationOptions> _localizationOptions;

    public CultureOptionsBuilder(IOptions<RequestLocalizationOptions> localizationOptions)
    {
        _localizationOptions = localizationOptions;
    }

    public IReadOnlyList<CultureOptionViewModel> Build(string currentUiCultureName)
    {
        return (_localizationOptions.Value.SupportedUICultures ?? [])
            .Select(culture => new CultureOptionViewModel
            {
                Value = culture.Name,
                Text = culture.NativeName,
                IsCurrent = culture.Name == currentUiCultureName
            })
            .ToList();
    }
}
