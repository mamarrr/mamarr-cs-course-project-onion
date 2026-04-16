namespace WebApp.ApiControllers.Shared;

public static class ApiRouteSections
{
    public const string ManagementDashboard = "management-dashboard";
    public const string CustomerDashboard = "customer-dashboard";
    public const string ResidentDashboard = "resident-dashboard";

    public static string FromContextType(string contextType)
    {
        return contextType switch
        {
            "management" => ManagementDashboard,
            "customer" => CustomerDashboard,
            "resident" => ResidentDashboard,
            _ => string.Empty
        };
    }
}
