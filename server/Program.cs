using BookIllustrator.Gemini;

var builder = WebApplication.CreateBuilder(args);
var useFakeGemini = bool.TryParse(Environment.GetEnvironmentVariable("USE_FAKE_GEMINI"), out var fake) && fake;

if (useFakeGemini)
{
    builder.Services.AddSingleton<IGeminiClient, FakeGeminiClient>();
}
else
{
    throw new NotImplementedException("GeminiClient not done yet — Block B task 4");
}
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
