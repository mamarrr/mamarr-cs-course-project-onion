using System.Globalization;

namespace WebApp.Tests.Helpers;

public sealed class CultureScope : IDisposable
{
    private readonly CultureInfo _previousCulture;
    private readonly CultureInfo _previousUICulture;

    public CultureScope(string culture) : this(culture, culture)
    {
    }

    public CultureScope(string culture, string uiCulture)
    {
        _previousCulture = Thread.CurrentThread.CurrentCulture;
        _previousUICulture = Thread.CurrentThread.CurrentUICulture;
        Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(culture);
        Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(uiCulture);
    }

    public void Dispose()
    {
        Thread.CurrentThread.CurrentCulture = _previousCulture;
        Thread.CurrentThread.CurrentUICulture = _previousUICulture;
    }
}
