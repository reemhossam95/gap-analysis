using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ContentGapAnalyzer.API.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerWithVersioning(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new()
            {
                Title = "Content Gap Analyzer API",
                Version = "v1",
                Description = "AI-powered Content Gap Analysis Platform"
            });

            // JWT Security (بدون OpenApiModels مباشرة)
            options.AddSecurityDefinition("Bearer", new()
            {
                Name = "Authorization",
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header
            });

            // options.AddSecurityRequirement(new()
            // {
            //     {
            //         new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            //         {
            //             Reference = new Microsoft.OpenApi.Models.OpenApiReference
            //             {
            //                 Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
            //                 Id = "Bearer"
            //             }
            //         },
            //         Array.Empty<string>()
            //     }
            // });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath);
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerWithUi(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
            c.RoutePrefix = "swagger";
        });

        return app;
    }
}