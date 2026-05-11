namespace App.DTO.v1.Portal.Users;

public class PendingAccessRequestDto
{
    public Guid RequestId { get; set; }
    public Guid AppUserId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string RequesterEmail { get; set; } = string.Empty;
    public string RequestedRoleCode { get; set; } = string.Empty;
    public string RequestedRoleLabel { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime RequestedAt { get; set; }
}
