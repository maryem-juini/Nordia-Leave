using LeaveApi.Services;
using Microsoft.AspNetCore.Routing;

namespace LeaveApi.Endpoints;

public static class EmployeeEndpoints
{
    public static void MapEmployeeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api");
        api.MapGet("/employees", (LeaveService service) => service.Employees);
    }
}
