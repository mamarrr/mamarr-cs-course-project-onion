namespace App.BLL.DTO.ManagementCompanies.Models;

/// <summary>
/// Single pending access request item.
/// </summary>
public class PendingAccessRequestItem
{
    public Guid RequestId { get; set; }
    public Guid AppUserId { get; set; }
    public string RequesterName { get; set; } = default!;
    public string RequesterEmail { get; set; } = default!;
    public string RequestedRoleCode { get; set; } = default!;
    public string RequestedRoleLabel { get; set; } = default!;
    public string? Message { get; set; }
    public DateTime RequestedAt { get; set; }
}
