using LeaveApi;
using LeaveApi.Models.Domain;
using LeaveApi.Services;

namespace LeaveApi.Tests;

internal static class TestData
{
    public static LeaveService NewService() => new(
        new[]
        {
            new Employee(1, "Amel", "Manager", "Platform", "manager", null),
            new Employee(2, "Bilel", "Employee", "Platform", "employee", 1),
            new Employee(3, "Sami", "Employee", "Platform", "employee", 1),
            new Employee(4, "Nour", "Employee", "Platform", "employee", 1),
            new Employee(9, "Imen", "Manager", "Support", "manager", null),
            new Employee(10, "Jawher", "Employee", "Support", "employee", 9),
            new Employee(11, "Lotfi", "Employee", "Support", "employee", 9),
            new Employee(12, "Hana", "Employee", "Support", "employee", 9),
        },
        new[]
        {
            new LeaveRequest
            {
                Id = 1, EmployeeId = 2, Start = D("2027-01-11"), End = D("2027-01-15"),
                Status = LeaveStatus.Approved, Reason = "Existing request",
            },
            new LeaveRequest
            {
                Id = 2, EmployeeId = 10, Start = D("2027-01-12"), End = D("2027-01-13"),
                Status = LeaveStatus.Approved,
            },
            new LeaveRequest
            {
                Id = 3, EmployeeId = 11, Start = D("2027-01-13"), End = D("2027-01-13"),
                Status = LeaveStatus.Approved,
            },
        });

    public static DateOnly D(string iso) => DateOnly.Parse(iso);
}
