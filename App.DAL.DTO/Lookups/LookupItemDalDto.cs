namespace App.DAL.DTO.Lookups;

public class LookupItemDalDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}
