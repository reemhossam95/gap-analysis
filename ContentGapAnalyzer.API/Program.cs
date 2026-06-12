using System.Threading.RateLimiting;
using Asp.Versioning;
using ContentGapAnalyzer.Infrastructure.Settings;
using ContentGapAnalyzer.API.Extensions;
using ContentGapAnalyzer.API.Middleware;
using ContentGapAnalyzer.Application;
using ContentGapAnalyzer.Infrastructure;
using ContentGapAnalyzer.Infrastructure.Persistence;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/content-gap-analyzer-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate:
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting Content Gap Analyzer API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy =
                System.Text.Json.JsonNamingPolicy.CamelCase;

            options.JsonSerializerOptions.DefaultIgnoreCondition =
                System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new HeaderApiVersionReader("X-API-Version"),
            new QueryStringApiVersionReader("api-version"));
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    builder.Services.AddSwaggerWithVersioning();
    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddRateLimiter(options =>
    {
        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

            await context.HttpContext.Response.WriteAsJsonAsync(
                new { success = false, message = "Too many requests. Please slow down." },
                cancellationToken);
        };

        options.AddFixedWindowLimiter("fixed", limiterOptions =>
        {
            limiterOptions.PermitLimit = 60;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit = 5;
        });

        options.AddFixedWindowLimiter("analysis", limiterOptions =>
        {
            limiterOptions.PermitLimit = 10;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit = 2;
        });
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins("http://localhost:3000", "http://127.0.0.1:5500")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AppDbContext>("database");

    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    builder.Services.Configure<GeminiOptions>(
        builder.Configuration.GetSection("Gemini"));

    var app = builder.Build();

    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<PerformanceLoggingMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();

    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwaggerWithUi();
    }
    else
    {
        app.UseSwaggerWithUi();
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    
    app.UseCors("AllowFrontend");
    
    app.UseRateLimiter();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Content Gap Analyzer API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}