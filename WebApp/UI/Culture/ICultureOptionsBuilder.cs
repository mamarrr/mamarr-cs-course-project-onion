namespace WebApp.UI.Culture;

public interface ICultureOptionsBuilder
{
    IReadOnlyList<CultureOptionViewModel> Build(string currentUiCultureName);
}
