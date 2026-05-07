namespace App.BLL.DTO.Leases.Models;

public class LeasePropertySearchResultModel
{
    public IReadOnlyList<LeasePropertySearchItemModel> Properties { get; init; } = Array.Empty<LeasePropertySearchItemModel>();
}
