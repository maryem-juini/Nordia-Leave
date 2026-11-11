using LeaveApi.Models.Domain;

namespace LeaveApi.Models.Responses;

public record LeaveView(
    int Id, int EmployeeId, string EmployeeName, string Team,
    DateOnly Start, DateOnly End, LeaveStatus Status,
    string? Reason, string? ManagerComment, int WorkingDays,
    bool Flagged, string? FlagDetail);
