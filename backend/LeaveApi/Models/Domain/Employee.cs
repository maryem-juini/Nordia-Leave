using System.Text.Json.Serialization;

namespace LeaveApi.Models.Domain;

public record Employee(int Id, string FirstName, string LastName, string Team, string Role, int? ManagerId)
{
    [JsonIgnore] public string FullName => $"{FirstName} {LastName}";
    [JsonIgnore] public bool IsManager => string.Equals(Role, "manager", StringComparison.OrdinalIgnoreCase);
}
