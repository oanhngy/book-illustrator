using BookIllustrator.Gemini;

var builder = WebApplication.CreateBuilder(args);
var useFakeGemini = bool.TryParse(Environment.GetEnvironmentVariable("USE_FAKE_GEMINI"), out var fake) && fake;

builder.Services.AddHttpClient<GeminiClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});

if (useFakeGemini)
{
    builder.Services.AddSingleton<IGeminiClient, FakeGeminiClient>();
}
else
{
    builder.Services.AddScoped<IGeminiClient>(sp => sp.GetRequiredService<GeminiClient>());
}
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
