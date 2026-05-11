namespace App.DTO.v1.Shared;

public class LookupOptionDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string Label { get; set; } = string.Empty;
}
