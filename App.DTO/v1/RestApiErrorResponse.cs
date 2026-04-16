namespace App.DTO.v1;

public class RestApiErrorResponse
{
    public System.Net.HttpStatusCode Status { get; set; }
    public string Error { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public Dictionary<string, string[]> Errors { get; set; } = new();
    public string? TraceId { get; set; }
}
