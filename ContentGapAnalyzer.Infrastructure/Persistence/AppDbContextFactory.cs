using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using ContentGapAnalyzer.Infrastructure.Persistence;

namespace ContentGapAnalyzer.Infrastructure;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "../ContentGapAnalyzer.API");

        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var connectionString = config.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        
        // تم التعديل هنا: إضافة CommandTimeout بمدة 120 ثانية لمنع انتهاء مهلة الاتصال
        optionsBuilder.UseSqlServer(connectionString, sqlOptions => 
        {
            sqlOptions.CommandTimeout(120);
        });

        return new AppDbContext(optionsBuilder.Options);
    }
}