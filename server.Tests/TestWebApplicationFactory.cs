using BookIllustrator;
using BookIllustrator.Gemini;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;

namespace server.Tests;

// Real SQLite in a temp file per instance, IGeminiClient replaced with a mock.
// "Mock Gemini, never the database" — CLAUDE.md.
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"book-illustrator-test-{Guid.NewGuid()}.db");
    private readonly string _storagePath = Path.Combine(Path.GetTempPath(), $"book-illustrator-test-{Guid.NewGuid()}");

    public Mock<IGeminiClient> GeminiMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Data Source={_dbPath}"));

            services.RemoveAll<IGeminiClient>();
            services.AddSingleton<IGeminiClient>(GeminiMock.Object);

            services.RemoveAll<PipelineService>();
            services.AddScoped<PipelineService>(sp => new PipelineService(
                sp.GetRequiredService<AppDbContext>(),
                sp.GetRequiredService<IGeminiClient>(),
                sp.GetRequiredService<ILogger<PipelineService>>(),
                _storagePath));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        foreach (var path in new[] { _dbPath, $"{_dbPath}-shm", $"{_dbPath}-wal" })
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        if (Directory.Exists(_storagePath))
            Directory.Delete(_storagePath, recursive: true);
    }
}
