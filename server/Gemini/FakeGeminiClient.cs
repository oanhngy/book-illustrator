using System.Text.Json;

namespace BookIllustrator.Gemini;

public sealed class FakeGeminiClient : IGeminiClient
{
    private static readonly TimeSpan Delay=TimeSpan.FromSeconds(15);
    private static readonly JsonSerializerOptions JsonOpts=new(JsonSerializerDefaults.Web);
    private readonly string _fixturesPath;

    public FakeGeminiClient(IHostEnvironment env)
    {
        _fixturesPath=Path.Combine(env.ContentRootPath, "..", "fixtures");
    }

    public async Task<GeminiJsonResult> GenerateJsonAsync(string prompt, string? previousInteractionId, string jsonSchema, CancellationToken ct=default)
    {
        await Task.Delay(Delay, ct);
        var fixtureName = jsonSchema switch
        {
            var s when s.Contains("\"style\"") => "style.json",
            var s when s.Contains("\"chapters\"") => "chapters.json",
            _ => "characters.json",
        };
        var response=ReadFixture(fixtureName);
        return new GeminiJsonResult(response.Id, InteractionParser.ExtractText(response));
    }

    public async Task<GeminiImageResult> GenerateImageAsync(string prompt, string? previousInteractionId, CancellationToken ct=default)
    {
        await Task.Delay(Delay, ct);
        var response=ReadFixture("portrait.json");
        var (bytes, mime)=InteractionParser.ExtractLastImage(response);
        return new GeminiImageResult(response.Id, bytes, mime);
    }

    private InteractionResponse ReadFixture(string fileName)
    {
        var path=Path.Combine(_fixturesPath, fileName);
        var json=File.ReadAllText(path);
        return JsonSerializer.Deserialize<InteractionResponse>(json, JsonOpts) ?? throw new InvalidOperationException($"Fixture {fileName} at {path} could not be parsed");
    }

}