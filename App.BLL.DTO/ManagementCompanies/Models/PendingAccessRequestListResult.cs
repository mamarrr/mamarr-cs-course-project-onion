namespace App.BLL.DTO.ManagementCompanies.Models;

/// <summary>
/// Result of listing pending access requests.
/// </summary>
public class PendingAccessRequestListResult
{
    public IReadOnlyList<PendingAccessRequestItem> Requests { get; set; } = Array.Empty<PendingAccessRequestItem>();
}
