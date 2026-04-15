using System.Threading;
using Microsoft.AspNetCore.Mvc;
using WebApp.Services.ManagementLayout;
using WebApp.ViewModels.Shared.Layout;

namespace WebApp.Areas.Management.Controllers;

public abstract class ManagementPageShellController : Controller
{
    private readonly IManagementLayoutViewModelProvider _managementLayoutViewModelProvider;

    protected ManagementPageShellController(IManagementLayoutViewModelProvider managementLayoutViewModelProvider)
    {
        _managementLayoutViewModelProvider = managementLayoutViewModelProvider;
    }

    protected async Task<ManagementPageShellViewModel> BuildManagementPageShellAsync(
        string title,
        string currentSectionLabel,
        string companySlug,
        CancellationToken cancellationToken,
        string? currentController = null)
    {
        var layoutContext = await _managementLayoutViewModelProvider.BuildAsync(
            User,
            new ManagementLayoutRequestViewModel
            {
                CurrentController = currentController ?? ControllerContext.ActionDescriptor.ControllerName,
                CompanySlug = companySlug,
                CurrentPathAndQuery = $"{Request.Path}{Request.QueryString}",
                CurrentUiCultureName = System.Threading.Thread.CurrentThread.CurrentUICulture.Name
            },
            cancellationToken);

        return new ManagementPageShellViewModel
        {
            Title = title,
            CurrentSectionLabel = currentSectionLabel,
            Management = layoutContext
        };
    }
}
