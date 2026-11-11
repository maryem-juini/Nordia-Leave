namespace LeaveApi.Models.Domain;

public class LeaveRequest
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public DateOnly Start { get; set; }
    public DateOnly End { get; set; }
    public LeaveStatus Status { get; set; }
    public string? Reason { get; set; }
    public string? ManagerComment { get; set; }
    public int? DecidedBy { get; set; }
}
