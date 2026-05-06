namespace App.BLL.DTO.Tickets;

public static class TicketWorkflowConstants
{
    public const string Created = "CREATED";
    public const string Assigned = "ASSIGNED";
    public const string Scheduled = "SCHEDULED";
    public const string InProgress = "IN_PROGRESS";
    public const string Completed = "COMPLETED";
    public const string Closed = "CLOSED";

    public static readonly IReadOnlyList<string> StatusOrder =
    [
        Created,
        Assigned,
        Scheduled,
        InProgress,
        Completed,
        Closed
    ];
}
