namespace App.DTO.v1.Portal.Leases;

public class LeaseResidentSearchItemDto
{
    public Guid ResidentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string IdCode { get; set; } = string.Empty;
}
