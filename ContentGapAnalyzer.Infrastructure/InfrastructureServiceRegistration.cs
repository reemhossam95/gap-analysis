using ContentGapAnalyzer.Application.Interfaces;
using ContentGapAnalyzer.Domain.Interfaces;
using ContentGapAnalyzer.Infrastructure.Persistence;
using ContentGapAnalyzer.Infrastructure.Persistence.Repositories;
using ContentGapAnalyzer.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace ContentGapAnalyzer.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── EF Core ──────────────────────────────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null);
                    sqlOptions.CommandTimeout(60);
                });
        });

        // ── Repositories ──────────────────────────────────────────────────
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IVideoRepository, VideoRepository>();
        services.AddScoped<IGapReportRepository, GapReportRepository>();
        services.AddScoped<IAnalysisSessionRepository, AnalysisSessionRepository>();
        services.AddScoped<ICachedTrendResultRepository, CachedTrendResultRepository>();
        services.AddScoped<IOpportunityRepository, OpportunityRepository>();

        // ── HttpClients with retry policies ──────────────────────────────
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (outcome, delay, attempt, _) =>
                {
                    Console.WriteLine($"[Retry {attempt}] after {delay.TotalSeconds}s due to: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
                });

        var circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

        // إعداد YouTube مع السياسات
        services.AddHttpClient("YouTube", client =>
        {
            client.BaseAddress = new Uri("https://www.googleapis.com");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        })
        .AddPolicyHandler(retryPolicy)
        .AddPolicyHandler(circuitBreakerPolicy);

        // إعداد Gemini (بدون CircuitBreaker لضمان عدم قفل الاتصال، وبمهلة زمنية كافية)
        services.AddHttpClient("Gemini", client =>
        {
            client.BaseAddress = new Uri("https://generativelanguage.googleapis.com");
            client.Timeout = TimeSpan.FromSeconds(120);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        })
        .AddPolicyHandler(retryPolicy);

        // ── External Services ─────────────────────────────────────────────
        services.AddScoped<IYouTubeService, YouTubeService>();
        services.AddScoped<IGeminiAiService, GeminiAiService>();

        // ── Memory Cache ──────────────────────────────────────────────────
        services.AddMemoryCache();
        return services;
    }
}