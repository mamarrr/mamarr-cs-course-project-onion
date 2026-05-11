namespace App.DTO.v1.Onboarding;

public class JoinRequestResultDto
{
    public bool Success { get; set; }
    public Guid? RequestId { get; set; }
    public string? Message { get; set; }
}
