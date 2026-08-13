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
builder.Services.AddScoped<PipelineService>(sp => new PipelineService(
    sp.GetRequiredService<AppDbContext>(),
    sp.GetRequiredService<IGeminiClient>(),
    sp.GetRequiredService<ILogger<PipelineService>>(),
    storagePath));
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

app.MapPost("/api/projects", async (CreateProjectRequest request, HttpContext http, AppDbContext db) =>
{
    var email = http.Request.Headers["X-User-Email"].ToString().Trim().ToLowerInvariant();
    if (email.Length == 0)
        return Results.BadRequest("X-User-Email header is required.");

    if (!await db.Users.AnyAsync(u => u.Email == email))
        return Results.Unauthorized();

    var title = request.Title?.Trim() ?? "";
    var bookText = request.BookText?.Trim() ?? "";
    if (title.Length == 0 || bookText.Length == 0)
        return Results.BadRequest("Title and bookText are required.");

    var project = new Project
    {
        Id = Guid.NewGuid(),
        UserEmail = email,
        Title = title,
        BookText = bookText,
        CreatedAt = DateTime.UtcNow,
        CompletedSteps = 0
    };
    db.Projects.Add(project);
    await db.SaveChangesAsync();

    return Results.Ok(ToSummary(project));
});

app.MapGet("/api/projects", async (HttpContext http, AppDbContext db) =>
{
    var email = http.Request.Headers["X-User-Email"].ToString().Trim().ToLowerInvariant();
    if (email.Length == 0)
        return Results.BadRequest("X-User-Email header is required.");

    var projects = await db.Projects
        .Where(p => p.UserEmail == email)
        .OrderByDescending(p => p.CreatedAt)
        .ToListAsync();

    return Results.Ok(projects.Select(ToSummary));
});

app.MapGet("/api/projects/{id:guid}", async (Guid id, HttpContext http, AppDbContext db) =>
{
    var email = http.Request.Headers["X-User-Email"].ToString().Trim().ToLowerInvariant();
    if (email.Length == 0)
        return Results.BadRequest("X-User-Email header is required.");

    var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id);
    if (project is null || project.UserEmail != email)
        return Results.NotFound();

    return Results.Ok(ToDetail(project));
});

app.MapPost("/api/projects/{id:guid}/steps/{step:int}/run", async (
    Guid id, int step, HttpContext http, AppDbContext db, IServiceScopeFactory scopeFactory) =>
{
    var email = http.Request.Headers["X-User-Email"].ToString().Trim().ToLowerInvariant();
    if (email.Length == 0)
        return Results.BadRequest("X-User-Email header is required.");

    if (step < 1 || step > 5)
        return Results.BadRequest("Step must be between 1 and 5.");

    var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id);
    if (project is null || project.UserEmail != email)
        return Results.NotFound();

    var staleThreshold = DateTime.UtcNow.AddMinutes(-5);

    var claimed = await db.Projects
        .Where(p => p.Id == id
            && p.UserEmail == email
            && p.CompletedSteps == step - 1
            && (p.RunningStep == null || p.RunningSince < staleThreshold))
        .ExecuteUpdateAsync(s => s
            .SetProperty(p => p.RunningStep, step)
            .SetProperty(p => p.RunningSince, DateTime.UtcNow)
            .SetProperty(p => p.LastError, (string?)null)
            .SetProperty(p => p.FailedStep, (int?)null));

    if (claimed == 0)
        return Results.Conflict("Step cannot be started right now.");

    _ = Task.Run(async () =>
    {
        using var scope = scopeFactory.CreateScope();
        var pipeline = scope.ServiceProvider.GetRequiredService<PipelineService>();
        await pipeline.RunStepAsync(id, step);
    });

    return Results.Accepted();
});

app.Run();

static ProjectSummary ToSummary(Project p) =>
    new(p.Id, p.Title, p.CreatedAt, p.CompletedSteps, p.RunningStep);

static ProjectDetail ToDetail(Project p)
{
    var canForceRetry = p.RunningStep is not null
        && p.RunningSince is not null
        && p.RunningSince < DateTime.UtcNow.AddMinutes(-5);

    return new(
        p.Id, p.Title, p.CreatedAt, p.BookText,
        p.CompletedSteps, p.RunningStep, p.RunningSince,
        p.LastError, p.FailedStep, canForceRetry,
        p.StyleJson, p.CharactersJson, p.ChaptersJson);
}

record AuthRequest(string Email, string Name);
record AuthResponse(Guid UserId, string Name, string Email);
record CreateProjectRequest(string Title, string BookText);
record ProjectSummary(Guid Id, string Title, DateTime CreatedAt, int CompletedSteps, int? RunningStep);
record ProjectDetail(
    Guid Id, string Title, DateTime CreatedAt, string BookText,
    int CompletedSteps, int? RunningStep, DateTime? RunningSince,
    string? LastError, int? FailedStep, bool CanForceRetry,
    string? StyleJson, string? CharactersJson, string? ChaptersJson);
