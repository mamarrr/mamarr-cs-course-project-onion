namespace App.DTO.v1.Portal.Leases;

public class LeasePropertySearchResultDto
{
    public IReadOnlyList<LeasePropertySearchItemDto> Properties { get; set; } = [];
}
