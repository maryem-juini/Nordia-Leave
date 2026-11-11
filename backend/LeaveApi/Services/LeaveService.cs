using LeaveApi.Models.Domain;
using LeaveApi.Models.Responses;
using LeaveApi.Data;
using Microsoft.EntityFrameworkCore;

namespace LeaveApi.Services;

/// <summary>
/// Business rules remain testable without a live database; the database context is optional in tests.
/// Each API operation works from a snapshot loaded from Oracle and commits mutations transactionally.
/// </summary>
public class LeaveService
{
    public const int YearlyAllowance = 25;
    public const double TeamAbsenceThreshold = 0.40;
    public const int MaxTextLength = 500;

    private readonly List<Employee> _employees;
    private readonly List<LeaveRequest> _leaves;
    private readonly LeaveDbContext? _db;
    private readonly object _gate = new();
    private int _nextId;

    public LeaveService(IEnumerable<Employee> employees, IEnumerable<LeaveRequest> leaves)
    {
        _employees = employees.ToList();
        _leaves = leaves.ToList();
        _nextId = _leaves.Count == 0 ? 1 : _leaves.Max(l => l.Id) + 1;
    }

    public LeaveService(LeaveDbContext db)
    {
        _db = db;
        _employees = db.Employees.AsNoTracking().ToList();
        _leaves = db.Leaves.ToList();
        _nextId = _leaves.Count == 0 ? 1 : _leaves.Max(l => l.Id) + 1;
    }

    public IReadOnlyList<Employee> Employees => _employees;

    // ---------- Working days ----------

    public static bool IsWorkingDay(DateOnly d) =>
        d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday;

    /// <summary>Counts Monday-Friday days between start and end (inclusive), optionally clipped to one calendar year.</summary>
    public static int CountWorkingDays(DateOnly start, DateOnly end, int? year = null)
    {
        if (start > end) return 0;

        var from = start;
        var to = end;
        if (year is int y)
        {
            var yearStart = new DateOnly(y, 1, 1);
            var yearEnd = new DateOnly(y, 12, 31);
            if (from < yearStart) from = yearStart;
            if (to > yearEnd) to = yearEnd;
        }

        if (from > to) return 0;

        var count = 0;
        for (var d = from; d <= to; d = d.AddDays(1))
            if (IsWorkingDay(d)) count++;
        return count;
    }

    // ---------- Queries ----------

    public Balance GetBalance(int employeeId, int year)
    {
        if (year < 2000 || year > 2100) throw new DomainException(400, "Year is out of range.");
        lock (_gate)
        {
            RequireEmployee(employeeId);
            return ComputeBalance(employeeId, year);
        }
    }

    public IReadOnlyList<LeaveView> GetHistory(int employeeId)
    {
        lock (_gate)
        {
            RequireEmployee(employeeId);
            return _leaves
                .Where(l => l.EmployeeId == employeeId)
                .OrderByDescending(l => l.Start)
                .Select(ToView)
                .ToList();
        }
    }

    public IReadOnlyList<LeaveView> GetPendingForManager(int managerId)
    {
        lock (_gate)
        {
            var manager = RequireManager(managerId);
            var reportIds = _employees.Where(e => e.ManagerId == manager.Id).Select(e => e.Id).ToHashSet();
            return _leaves
                .Where(l => l.Status == LeaveStatus.Pending && reportIds.Contains(l.EmployeeId))
                .OrderBy(l => l.Start)
                .Select(ToView)
                .ToList();
        }
    }

    // ---------- Commands ----------

    public LeaveView Create(int employeeId, DateOnly start, DateOnly end, string? reason)
    {
        lock (_gate)
        {
            using var transaction = _db?.Database.BeginTransaction();
            if (_db is not null)
            {
                _db.LockEmployee(employeeId);
                RefreshLeaves();
            }

            var employee = RequireEmployee(employeeId);

            // Rule: managers only review their team's requests; they cannot request leave themselves.
            if (employee.IsManager)
                throw new DomainException(403, "Managers cannot request leave. They only review their team's requests.");

            if (start == default || end == default)
                throw new DomainException(400, "Start and end dates are required.");
            if (start <= DateOnly.FromDateTime(DateTime.Today))
                throw new DomainException(400, "Leave must start in the future.");
            if (end < start)
                throw new DomainException(400, "The end date must not be before the start date.");
            if (end.DayNumber - start.DayNumber > 366)
                throw new DomainException(400, "A request cannot span more than one year.");

            reason = reason?.Trim();
            if (reason is { Length: > MaxTextLength })
                throw new DomainException(400, $"The reason must be at most {MaxTextLength} characters.");

            if (CountWorkingDays(start, end) == 0)
                throw new DomainException(400, "The request contains no working day (Monday to Friday).");

            // Rule: two requests from the same employee cannot overlap (rejected ones do not count).
            var clash = _leaves.FirstOrDefault(l =>
                l.EmployeeId == employeeId && l.Status != LeaveStatus.Rejected && l.Start <= end && start <= l.End);
            if (clash is not null)
                throw new DomainException(409,
                    $"This request overlaps your leave from {clash.Start:yyyy-MM-dd} to {clash.End:yyyy-MM-dd}.");

            // Rule: a request that exceeds the balance is rejected at creation.
            // A request can straddle two years, so each year is checked against its own balance.
            for (var year = start.Year; year <= end.Year; year++)
            {
                var needed = CountWorkingDays(start, end, year);
                if (needed == 0) continue;
                var remaining = ComputeBalance(employeeId, year).Remaining;
                if (needed > remaining)
                    throw new DomainException(422,
                        $"Insufficient balance for {year}: {needed} working day(s) requested, {Math.Max(remaining, 0)} remaining.");
            }

            var leave = new LeaveRequest
            {
                Id = _db is null ? _nextId++ : _db.NextLeaveId(),
                EmployeeId = employeeId,
                Start = start,
                End = end,
                Reason = string.IsNullOrEmpty(reason) ? null : reason,
                Status = LeaveStatus.Pending,
            };
            _leaves.Add(leave);
            if (_db is not null)
            {
                _db.Leaves.Add(leave);
                _db.SaveChanges();
            }
            transaction?.Commit();
            return ToView(leave);
        }
    }

    public LeaveView Decide(int managerId, int leaveId, bool approve, string? comment)
    {
        lock (_gate)
        {
            using var transaction = _db?.Database.BeginTransaction();
            if (_db is not null)
            {
                _db.LockLeave(leaveId);
                RefreshLeaves();
            }

            var manager = RequireManager(managerId);
            var leave = _leaves.FirstOrDefault(l => l.Id == leaveId)
                        ?? throw new DomainException(404, "Leave request not found.");
            var owner = _employees.First(e => e.Id == leave.EmployeeId);

            if (owner.ManagerId != manager.Id)
                throw new DomainException(403, "You can only decide on requests from your own team.");
            if (leave.Status != LeaveStatus.Pending)
                throw new DomainException(409, "This request has already been decided.");

            comment = comment?.Trim();
            if (comment is { Length: > MaxTextLength })
                throw new DomainException(400, $"The comment must be at most {MaxTextLength} characters.");
            if (!approve && string.IsNullOrEmpty(comment))
                throw new DomainException(400, "A comment is required when rejecting a request.");

            leave.Status = approve ? LeaveStatus.Approved : LeaveStatus.Rejected;
            leave.ManagerComment = string.IsNullOrEmpty(comment) ? null : comment;
            leave.DecidedBy = manager.Id;
            _db?.SaveChanges();
            transaction?.Commit();
            return ToView(leave);
        }
    }

    private void RefreshLeaves()
    {
        _db!.ChangeTracker.Clear();
        _leaves.Clear();
        _leaves.AddRange(_db.Leaves.ToList());
    }

    // ---------- Helpers (always called inside the lock) ----------

    private Employee RequireEmployee(int id) =>
        _employees.FirstOrDefault(e => e.Id == id) ?? throw new DomainException(401, "Unknown user.");

    private Employee RequireManager(int id)
    {
        var e = RequireEmployee(id);
        if (!e.IsManager) throw new DomainException(403, "Only managers can do this.");
        return e;
    }

    private Balance ComputeBalance(int employeeId, int year)
    {
        int approved = 0, pending = 0;
        foreach (var l in _leaves.Where(l => l.EmployeeId == employeeId))
        {
            var days = CountWorkingDays(l.Start, l.End, year);
            if (l.Status == LeaveStatus.Approved) approved += days;
            else if (l.Status == LeaveStatus.Pending) pending += days;
        }
        return new Balance(year, YearlyAllowance, approved, pending, YearlyAllowance - approved - pending);
    }

    private LeaveView ToView(LeaveRequest l)
    {
        var employee = _employees.First(e => e.Id == l.EmployeeId);
        var flagged = false;
        string? detail = null;
        if (l.Status == LeaveStatus.Pending)
            (flagged, detail) = EvaluateTeamLoad(l, employee);

        return new LeaveView(l.Id, employee.Id, employee.FullName, employee.Team, l.Start, l.End, l.Status,
            l.Reason, l.ManagerComment, CountWorkingDays(l.Start, l.End), flagged, detail);
    }

    /// <summary>
    /// Rule: if more than 40 % of the team is ALREADY away on a given day, flag the request (never auto-reject).
    /// "Already away" = other team members with a pending or approved leave covering that day.
    /// The team includes its manager.
    /// </summary>
    private (bool Flagged, string? Detail) EvaluateTeamLoad(LeaveRequest request, Employee employee)
    {
        var teamIds = _employees.Where(e => e.Team == employee.Team).Select(e => e.Id).ToHashSet();
        var teamSize = teamIds.Count;
        if (teamSize == 0) return (false, null);

        double worstShare = 0;
        var worstDay = request.Start;
        var worstCount = 0;

        for (var d = request.Start; d <= request.End; d = d.AddDays(1))
        {
            if (!IsWorkingDay(d)) continue;
            var away = _leaves
                .Where(o => o.Id != request.Id && o.EmployeeId != employee.Id && teamIds.Contains(o.EmployeeId)
                            && o.Status != LeaveStatus.Rejected && o.Start <= d && d <= o.End)
                .Select(o => o.EmployeeId)
                .Distinct()
                .Count();
            var share = (double)away / teamSize;
            if (share > worstShare)
            {
                worstShare = share;
                worstDay = d;
                worstCount = away;
            }
        }

        if (worstShare > TeamAbsenceThreshold)
            return (true, $"{worstCount} of {teamSize} team members are already away on {worstDay:yyyy-MM-dd} ({Math.Round(worstShare * 100)} %).");
        return (false, null);
    }
}