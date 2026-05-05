namespace App.DAL.DTO.Properties;

public class PropertyWorkspaceDalDto
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    
}
