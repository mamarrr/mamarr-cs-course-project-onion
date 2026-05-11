namespace App.DTO.v1.Portal.ScheduledWork;

public class ScheduledWorkOptionDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Code { get; set; }
}
