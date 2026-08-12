using BookIllustrator;
using BookIllustrator.Gemini;
using BookIllustrator.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var useFakeGemini = bool.TryParse(Environment.GetEnvironmentVariable("USE_FAKE_GEMINI"), out var fake) && fake;

builder.Services.AddHttpClient<GeminiClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});

var storagePath = Environment.GetEnvironmentVariable("STORAGE_PATH") ?? "./data";
Directory.CreateDirectory(storagePath);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={Path.Combine(storagePath, "app.db")}"));

if (useFakeGemini)
{
    builder.Services.AddSingleton<IGeminiClient, FakeGeminiClient>();
}
else
{
    builder.Services.AddScoped<IGeminiClient>(sp => sp.GetRequiredService<GeminiClient>());
}
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

app.MapGet("/", () => "Hello World!");

app.MapPost("/api/auth", async (AuthRequest request, AppDbContext db) =>
{
    var email = request.Email?.Trim().ToLowerInvariant() ?? "";
    var name = request.Name?.Trim() ?? "";

    if (email.Length == 0 || !email.Contains('@') || name.Length == 0)
        return Results.BadRequest("Email and name are required.");

    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
    if (user is null)
    {
        user = new User { Id = Guid.NewGuid(), Email = email, Name = name };
        db.Users.Add(user);
    }
    else
    {
        user.Name = name;
    }
    await db.SaveChangesAsync();

    return Results.Ok(new AuthResponse(user.Id, user.Name, user.Email));
});

app.Run();

record AuthRequest(string Email, string Name);
record AuthResponse(Guid UserId, string Name, string Email);
