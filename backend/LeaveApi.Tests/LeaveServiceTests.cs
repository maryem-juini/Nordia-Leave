using LeaveApi;
using LeaveApi.Models;
using LeaveApi.Models.Domain;
using LeaveApi.Services;
using static LeaveApi.Tests.TestData;

namespace LeaveApi.Tests;
public class LeaveServiceTests
{
    private static int StatusOf(Action act) => Assert.Throws<DomainException>(act).StatusCode;

    // ---- working days ----

    [Fact]
    public void WorkingDays_ExcludeWeekends()
    {
        Assert.Equal(5, LeaveService.CountWorkingDays(D("2027-01-04"), D("2027-01-10"))); // Mon..Sun
        Assert.Equal(2, LeaveService.CountWorkingDays(D("2027-01-08"), D("2027-01-11"))); // Fri..Mon
    }

    [Fact]
    public void WorkingDays_InvalidRange_ReturnsZero()
    {
        Assert.Equal(0, LeaveService.CountWorkingDays(D("2027-01-10"), D("2027-01-08")));
    }

    [Fact]
    public void Create_WeekendOnly_IsRejected()
    {
        var s = NewService();
        Assert.Equal(400, StatusOf(() => s.Create(3, D("2027-01-09"), D("2027-01-10"), null)));
    }

    [Fact]
    public void Create_EndBeforeStart_IsRejected()
    {
        var s = NewService();
        Assert.Equal(400, StatusOf(() => s.Create(3, D("2027-02-10"), D("2027-02-09"), null)));
    }

    [Fact]
    public void Create_PastOrCurrentStartDate_IsRejected()
    {
        var s = NewService();
        var today = DateOnly.FromDateTime(DateTime.Today);

        Assert.Equal("Leave must start in the future.",
            Assert.Throws<DomainException>(() => s.Create(3, today.AddDays(-1), today.AddDays(-1), null)).Message);
        Assert.Equal("Leave must start in the future.",
            Assert.Throws<DomainException>(() => s.Create(3, today, today, null)).Message);
    }

    // ---- balance ----

    [Fact]
    public void Balance_CountsApprovedLeave_FromSeed()
    {
        var s = NewService();
        var b = s.GetBalance(2, 2027);
        Assert.Equal(5, b.Approved);
        Assert.Equal(20, b.Remaining);
        Assert.Equal(25, s.GetBalance(2, 2026).Remaining);
    }

    [Fact]
    public void Create_ExceedingBalance_IsRejected()
    {
        var s = NewService();
        // Mon 5 Apr -> Fri 14 May = 30 working days
        Assert.Equal(422, StatusOf(() => s.Create(3, D("2027-04-05"), D("2027-05-14"), null)));
        Assert.Empty(s.GetHistory(3));
    }

    [Fact]
    public void Create_ExactlyTheBalance_IsAccepted_ThenOneMoreDayIsRejected()
    {
        var s = NewService();
        var ok = s.Create(3, D("2027-04-05"), D("2027-05-07"), null); // exactly 25 working days
        Assert.Equal(25, ok.WorkingDays);
        Assert.Equal(0, s.GetBalance(3, 2027).Remaining);
        Assert.Equal(422, StatusOf(() => s.Create(3, D("2027-06-01"), D("2027-06-01"), null)));
    }

    [Fact]
    public void Create_AcrossTwoYears_ChargesEachYearSeparately()
    {
        var s = NewService();
        s.Create(3, D("2026-12-28"), D("2027-01-08"), null); // 4 days in 2026, 6 in 2027
        Assert.Equal(4, s.GetBalance(3, 2026).Pending);
        Assert.Equal(6, s.GetBalance(3, 2027).Pending);
    }

    [Fact]
    public void RejectedRequest_GivesTheDaysBack()
    {
        var s = NewService();
        var leave = s.Create(3, D("2027-03-01"), D("2027-03-05"), "Trip");
        Assert.Equal(20, s.GetBalance(3, 2027).Remaining);
        s.Decide(1, leave.Id, approve: false, "Release week");
        Assert.Equal(25, s.GetBalance(3, 2027).Remaining);
    }

    // ---- overlap ----

    [Fact]
    public void Create_OverlappingOwnLeave_IsRejected()
    {
        var s = NewService();
        Assert.Equal(409, StatusOf(() => s.Create(2, D("2027-01-06"), D("2027-01-12"), null)));
    }

    // ---- team load flag ----

    [Fact]
    public void Create_WhenMoreThan40PercentOfTeamAway_IsFlagged_NotRejected()
    {
        var s = NewService();
        var created = s.Create(12, D("2027-01-13"), D("2027-01-13"), null); // 2 of 4 away = 50 %
        Assert.Equal(LeaveStatus.Pending, created.Status);

        var pending = s.GetPendingForManager(9);
        var item = Assert.Single(pending);
        Assert.True(item.Flagged);
        Assert.Contains("2 of 4", item.FlagDetail);
    }

    [Fact]
    public void Create_WhenOnly25PercentOfTeamAway_IsNotFlagged()
    {
        var s = NewService();
        s.Create(12, D("2027-01-12"), D("2027-01-12"), null); // only Jawher away = 25 %
        Assert.False(Assert.Single(s.GetPendingForManager(9)).Flagged);
    }

    // ---- manager decisions ----

    [Fact]
    public void Manager_SeesOnlyOwnTeamPendingRequests()
    {
        var s = NewService();
        s.Create(3, D("2027-03-01"), D("2027-03-02"), null);   // Platform
        s.Create(12, D("2027-03-01"), D("2027-03-02"), null);  // Support
        Assert.Equal(3, Assert.Single(s.GetPendingForManager(1)).EmployeeId);
        Assert.Equal(12, Assert.Single(s.GetPendingForManager(9)).EmployeeId);
    }

    [Fact]
    public void Approve_ChangesStatus_AndRejectNeedsAComment()
    {
        var s = NewService();
        var leave = s.Create(3, D("2027-03-01"), D("2027-03-02"), null);
        Assert.Equal(400, StatusOf(() => s.Decide(1, leave.Id, false, "  ")));
        var approved = s.Decide(1, leave.Id, true, "Enjoy");
        Assert.Equal(LeaveStatus.Approved, approved.Status);
        Assert.Equal("Enjoy", approved.ManagerComment);
        Assert.Equal(409, StatusOf(() => s.Decide(1, leave.Id, true, null)));
    }
}