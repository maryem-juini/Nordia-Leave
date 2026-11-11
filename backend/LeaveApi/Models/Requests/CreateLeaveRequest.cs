namespace LeaveApi.Models.Requests;

public record CreateLeaveRequest(DateOnly Start, DateOnly End, string? Reason);
