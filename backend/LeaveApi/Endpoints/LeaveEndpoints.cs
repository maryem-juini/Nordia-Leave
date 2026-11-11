using LeaveApi.Models.Domain;
using LeaveApi.Models.Requests;
using LeaveApi.Services;
using Microsoft.AspNetCore.Routing;

namespace LeaveApi.Endpoints;

public static class LeaveEndpoints
{
    public static void MapLeaveEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api");

        api.MapGet("/me/balance", (HttpContext context, LeaveService service, int? year) =>
            service.GetBalance(CurrentUser(context), year ?? DateTime.Today.Year));

        api.MapGet("/me/leaves", (HttpContext context, LeaveService service) =>
            service.GetHistory(CurrentUser(context)));

        api.MapPost("/leaves", (HttpContext context, LeaveService service, CreateLeaveRequest body) =>
        {
            var created = service.Create(CurrentUser(context), body.Start, body.End, body.Reason);
            return Results.Created($"/api/leaves/{created.Id}", created);
        });

        api.MapGet("/leaves/pending", (HttpContext context, LeaveService service) =>
            service.GetPendingForManager(CurrentUser(context)));

        api.MapPost("/leaves/{id:int}/decision",
            (int id, HttpContext context, LeaveService service, DecisionRequest body) =>
                service.Decide(CurrentUser(context), id, body.Approve, body.Comment));
    }

    // The UI's role switch sends an employee id; the service enforces permissions for that user.
    private static int CurrentUser(HttpContext context) =>
        int.TryParse(context.Request.Headers["X-Employee-Id"].ToString(), out var id)
            ? id
            : throw new DomainException(401, "Missing or invalid X-Employee-Id header.");
}
