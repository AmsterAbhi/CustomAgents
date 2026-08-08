using JobSearchAggregator.Application;
using JobSearchAggregator.Infrastructure;
using JobSearchAggregator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

const string AngularDevCorsPolicy = "AngularDevClient";

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true)
        .Build())
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting JobSearchAggregator.Api");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration));

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
        {
            Title = "Job Search Aggregator API",
            Version = "v1",
            Description = "Local-only API for discovering, matching, and tracking software/AI job openings."
        });
    });

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AppDbContext>("postgresql");

    var angularDevOrigin = builder.Configuration["Cors:AngularDevOrigin"] ?? "http://localhost:4200";
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(AngularDevCorsPolicy, policy =>
            policy.WithOrigins(angularDevOrigin)
                .AllowAnyHeader()
                .AllowAnyMethod());
    });

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Job Search Aggregator API v1");
        });
    }

    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();

    app.UseCors(AngularDevCorsPolicy);

    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex) when (ex.GetType().Name is not "HostAbortedException")
{
    // HostAbortedException is thrown intentionally by `dotnet ef` design-time
    // tooling when it builds the host just to discover the DbContext; it is
    // not a real startup failure and must not be logged as fatal.
    Log.Fatal(ex, "JobSearchAggregator.Api terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
