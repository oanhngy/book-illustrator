namespace BookIllustrator.Gemini;

public interface IGeminiClient
{
    Task<GeminiJsonResult> GenerateJsonAsync(string prompt, string? previousInteractionId, string jsonSchema, CancellationToken ct=default);
    Task<GeminiImageResult> GenerateImageAsync(string prompt, string? previousInteractionId, CancellationToken ct=default);
}

public sealed record GeminiJsonResult(string InteractionId, string Json);
public sealed record GeminiImageResult(string InteractionId, byte[] ImageBytes, string MimeType);