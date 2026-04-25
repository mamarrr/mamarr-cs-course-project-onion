namespace WebApp.UI.Chrome;

public interface IAppChromeBuilder
{
    Task<AppChromeViewModel> BuildAsync(
        AppChromeRequest request,
        CancellationToken cancellationToken = default);
}
