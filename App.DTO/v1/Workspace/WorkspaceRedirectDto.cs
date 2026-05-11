namespace App.DTO.v1.Workspace;

public class WorkspaceRedirectDto
{
    public string Destination { get; set; } = string.Empty;
    public string? CompanySlug { get; set; }
    public string? CustomerSlug { get; set; }
    public string? ResidentIdCode { get; set; }
    public string Path { get; set; } = string.Empty;
}
