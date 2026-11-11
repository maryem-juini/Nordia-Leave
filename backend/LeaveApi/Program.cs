using System.Text.Json;
using System.Text.Json.Serialization;
using LeaveApi.Data;
using LeaveApi.Endpoints;
using LeaveApi.Models.Domain;
using LeaveApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

var oracleConnectionString = builder.Configuration.GetConnectionString("Oracle");
if (string.IsNullOrWhiteSpace(oracleConnectionString))
    throw new InvalidOperationException("Set the Oracle connection string named 'Oracle' in appsettings.Development.json or ConnectionStrings__Oracle.");

builder.Services.AddDbContext<LeaveDbContext>(options => options.UseOracle(oracleConnectionString));
builder.Services.AddScoped(provider =>
    new LeaveService(provider.GetRequiredService<LeaveDbContext>()));

var app = builder.Build();

// Turn business-rule errors into a clean JSON body: { "error": "..." }
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (DomainException ex)
    {
        context.Response.StatusCode = ex.StatusCode;
        await context.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
});

app.MapEmployeeEndpoints();
app.MapLeaveEndpoints();

app.Run();

// Makes the entry point visible to WebApplicationFactory in the test project.
public partial class Program { }
