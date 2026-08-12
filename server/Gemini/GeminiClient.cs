using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BookIllustrator.Gemini;

public sealed class GeminiClient : IGeminiClient
{
    private const string BaseUrl="https://generativelanguage.googleapis.com/v1beta/interactions";
    private static readonly JsonSerializerOptions JsonOpts=new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly string _textModel;
    private readonly string _imageModel;

    public GeminiClient(HttpClient http)
    {
        _http=http;
        _http.DefaultRequestHeaders.Add("x-goog-api-key", Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? throw new InvalidOperationException("GEMINI_API_KEY is not set"));
        _textModel=Environment.GetEnvironmentVariable("GEMINI_TEXT_MODEL") ?? throw new InvalidOperationException("GEMINI_TEXT_MODEL is not set");
        _imageModel=Environment.GetEnvironmentVariable("GEMINI_IMAGE_MODEL") ?? throw new InvalidOperationException("GEMINI_IMAGE_MODEL is not set");
    }

    public async Task<GeminiJsonResult> GenerateJsonAsync(string prompt, string? previousInteractionId, string jsonSchema, CancellationToken ct=default)
    {
        var body=new JsonObject
        {
            ["model"]=_textModel,
            ["input"]=prompt,
            ["response_format"]=new JsonArray
            {
                new JsonObject
                {
                    ["type"]="text",
                    ["mime_type"]="application/json",
                    ["schema"]=JsonNode.Parse(jsonSchema) ?? throw new ArgumentException("jsonSchema is not valid JSON", nameof(jsonSchema))
                }
            }
        };

        if(previousInteractionId is not null)
            body["previous_interaction_id"]=previousInteractionId;

        var response=await SendAsync(body,ct);
        return new GeminiJsonResult(response.Id, InteractionParser.ExtractText(response));
    }

    public async Task<GeminiImageResult> GenerateImageAsync(string prompt, string? previousInteractionId, CancellationToken ct=default)
    {
        var wrappedPrompt=$"{prompt} " + "Generate exactly one image and stop - do not generate a draft followed by a final version. " + "Do not include any text response, only the image.";

        var body=new JsonObject
        {
            ["model"]=_imageModel,
            ["input"]=wrappedPrompt
        };

        if(previousInteractionId is not null)
            body["previous_interaction_id"]=previousInteractionId;

        var response=await SendAsync(body,ct);
        var(bytes, mime)=InteractionParser.ExtractLastImage(response);
        return new GeminiImageResult(response.Id, bytes, mime);
    }

    private async Task<InteractionResponse> SendAsync(JsonObject body, CancellationToken ct)
    {
        using var httpResponse=await _http.PostAsJsonAsync(BaseUrl, body, JsonOpts, ct);
        httpResponse.EnsureSuccessStatusCode();
        var response=await httpResponse.Content.ReadFromJsonAsync<InteractionResponse>(JsonOpts,ct) ?? throw new InvalidOperationException("Gemini response body could not be parsed");

        if(response.Status!="completed")
            throw new InvalidOperationException($"Gemini interaction {response.Id} ended with status '{response.Status}'");

        return response;
    }
}