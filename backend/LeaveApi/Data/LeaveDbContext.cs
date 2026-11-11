using System.Data;
using LeaveApi.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LeaveApi.Data;

public sealed class LeaveDbContext(DbContextOptions<LeaveDbContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<LeaveRequest> Leaves => Set<LeaveRequest>();

    public int NextLeaveId()
    {
        var connection = Database.GetDbConnection();
        var closeConnection = connection.State != ConnectionState.Open;
        if (closeConnection)
            connection.Open();

        try
        {
            using var command = connection.CreateCommand();
            command.Transaction = Database.CurrentTransaction?.GetDbTransaction();
            command.CommandText = "SELECT LEAVES_SEQ.NEXTVAL FROM DUAL";
            return Convert.ToInt32(command.ExecuteScalar());
        }
        finally
        {
            if (closeConnection)
                connection.Close();
        }
    }

    public void LockEmployee(int employeeId) => ExecuteRowLock(
        "SELECT ID FROM EMPLOYEES WHERE ID = :id FOR UPDATE",
        employeeId);

    public void LockLeave(int leaveId) => ExecuteRowLock(
        "SELECT ID FROM LEAVES WHERE ID = :id FOR UPDATE",
        leaveId);

    private void ExecuteRowLock(string sql, int id)
    {
        using var command = Database.GetDbConnection().CreateCommand();
        command.Transaction = Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = sql;
        var parameter = command.CreateParameter();
        parameter.ParameterName = "id";
        parameter.Value = id;
        command.Parameters.Add(parameter);
        using var reader = command.ExecuteReader();
        while (reader.Read()) { }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("EMPLOYEES");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("ID").ValueGeneratedNever();
            entity.Property(e => e.FirstName).HasColumnName("FIRST_NAME").HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasColumnName("LAST_NAME").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Team).HasColumnName("TEAM").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Role).HasColumnName("ROLE").HasMaxLength(20).IsRequired();
            entity.Property(e => e.ManagerId).HasColumnName("MANAGER_ID");
            entity.Ignore(e => e.FullName);
            entity.Ignore(e => e.IsManager);
        });

        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.ToTable("LEAVES");
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Id).HasColumnName("ID").ValueGeneratedNever();
            entity.Property(l => l.EmployeeId).HasColumnName("EMPLOYEE_ID").IsRequired();
            entity.Property(l => l.Start)
                .HasColumnName("START_DATE")
                .HasConversion(new ValueConverter<DateOnly, DateTime>(
                    date => date.ToDateTime(TimeOnly.MinValue),
                    dateTime => DateOnly.FromDateTime(dateTime)))
                .HasColumnType("DATE")
                .IsRequired();
            entity.Property(l => l.End)
                .HasColumnName("END_DATE")
                .HasConversion(new ValueConverter<DateOnly, DateTime>(
                    date => date.ToDateTime(TimeOnly.MinValue),
                    dateTime => DateOnly.FromDateTime(dateTime)))
                .HasColumnType("DATE")
                .IsRequired();
            entity.Property(l => l.Status)
                .HasColumnName("STATUS")
                .HasConversion(
                    status => status.ToString().ToLowerInvariant(),
                    status => Enum.Parse<LeaveStatus>(status, true))
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(l => l.Reason).HasColumnName("REASON").HasMaxLength(500);
            entity.Property(l => l.ManagerComment).HasColumnName("MANAGER_COMMENT").HasMaxLength(500);
            entity.Property(l => l.DecidedBy).HasColumnName("DECIDED_BY");
        });
    }
}