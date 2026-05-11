namespace App.DTO.v1.Portal.WorkLogs;

public class WorkLogTotalsDto
{
    public int Count { get; set; }
    public decimal Hours { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal TotalCost { get; set; }
}
