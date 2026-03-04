using FastEndpoints;
using FastEndpoints.Swagger;
using ReadingTheReader.core.Application;
using ReadingTheReader.Realtime.Persistence;
using ReadingTheReader.TobiiEyetracker;
using ReadingTheReader.WebApi;
using ReadingTheReader.WebApi.Websockets;

var builder = WebApplication.CreateBuilder(args);
const string LocalhostCorsPolicy = "LocalhostCorsPolicy";

// Modules installation
builder.Services.InstallTobiiEyeTrackerModule();
builder.Services.InstallApplicationModule();
builder.Services.InstallRealtimePersistenceModule(builder.Configuration);

builder.Services.AddWebSocketServices();
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy(LocalhostCorsPolicy, policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
                Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                 uri.Host.Equals("127.0.0.1")))
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddFastEndpoints().SwaggerDocument();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors(LocalhostCorsPolicy);
app.UseFastEndpoints(c =>
{
    c.Endpoints.RoutePrefix = "api";
}).UseSwaggerGen();

app.UseAuthentication();
app.UseAuthorization();
app.ConfigureWebSockets();

app.Run();
