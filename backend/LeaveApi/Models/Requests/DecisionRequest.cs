namespace LeaveApi.Models.Requests;

public record DecisionRequest(bool Approve, string? Comment);
