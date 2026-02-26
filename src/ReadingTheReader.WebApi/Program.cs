using FastEndpoints;
using FastEndpoints.Swagger;
using ReadingTheReader.core.Application.ApplicationContracts.Realtime;
using ReadingTheReader.RealtimeMessenger;
using ReadingTheReader.TobiiEyetracker;
using ReadingTheReader.WebApi;
using ReadingTheReader.WebApi.Websockets;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

builder.Services.InstallTobiiEyeTrackerModule();
builder.Services.AddWebSocketServices();

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddFastEndpoints().SwaggerDocument();
builder.Services.AddEndpointsApiExplorer();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseFastEndpoints(c => {
    c.Endpoints.RoutePrefix = "api";
}).UseSwaggerGen();

app.UseAuthentication();
app.UseAuthorization();
app.ConfigureWebSockets();

app.Run();