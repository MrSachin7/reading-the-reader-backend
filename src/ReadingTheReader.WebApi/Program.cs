using FastEndpoints;
using FastEndpoints.Swagger;
using ReadingTheReader.core.Application;
using ReadingTheReader.Realtime.Persistence;
using ReadingTheReader.TobiiEyetracker;
using ReadingTheReader.WebApi;
using ReadingTheReader.WebApi.Websockets;

var builder = WebApplication.CreateBuilder(args);

// Modules installation
builder.Services.InstallTobiiEyeTrackerModule();
builder.Services.InstallApplicationModule();
builder.Services.InstallRealtimePersistenceModule(builder.Configuration);

builder.Services.AddWebSocketServices();
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddFastEndpoints().SwaggerDocument();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseFastEndpoints(c =>
{
    c.Endpoints.RoutePrefix = "api";
}).UseSwaggerGen();

app.UseAuthentication();
app.UseAuthorization();
app.ConfigureWebSockets();

app.Run();
