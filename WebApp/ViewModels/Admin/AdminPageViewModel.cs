namespace WebApp.ViewModels.Admin;

public interface IAdminPageViewModel
{
    string PageTitle { get; }
    string ActiveSection { get; }
    string? SuccessMessage { get; set; }
    string? ErrorMessage { get; set; }
}

public abstract class AdminPageViewModel : IAdminPageViewModel
{
    public string PageTitle { get; set; } = string.Empty;
    public string ActiveSection { get; set; } = string.Empty;
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
}
