using System.Text.Json.Serialization;

namespace BookIllustrator.Gemini;

internal sealed class InteractionResponse
{
    public string Id {get; set;}="";
    public string Status {get; set;}="";
    public List<InteractionStep> Steps {get; set;}=[];
}

internal sealed class InteractionStep
{
    public string Type {get; set;}="";
    public List<InteractionContent>? Content {get; set;}
}

internal sealed class InteractionContent
{
    public string Type {get; set;}="";
    public string? Text {get; set;}
    public string? Data {get; set;}

    [JsonPropertyName("mime_type")]
    public string? MimeType {get; set;}
}