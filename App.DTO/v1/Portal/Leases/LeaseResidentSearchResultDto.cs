namespace App.DTO.v1.Portal.Leases;

public class LeaseResidentSearchResultDto
{
    public IReadOnlyList<LeaseResidentSearchItemDto> Residents { get; set; } = [];
}
