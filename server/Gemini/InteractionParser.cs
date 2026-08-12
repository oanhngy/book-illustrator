//task 3,4 gọi ở đây
namespace BookIllustrator.Gemini;

internal static class InteractionParser
{
    public static string ExtractText(InteractionResponse response)
    {
        var text=response.Steps
            .Where(s=>s.Type=="model_output")
            .SelectMany(s=>s.Content ?? [])
            .LastOrDefault(c=>c.Type=="text")
            ?.Text;

        return text ?? throw new InvalidOperationException($"Gemini repsonse {response.Id} has no text content");
    }

    //logic nếu output >2 lấy later one
    public static(byte[] Bytes, string MimeType) ExtractLastImage(InteractionResponse response)
    {
        var image=response.Steps
            .Where(s=>s.Type=="model_output")
            .SelectMany(s=>s.Content ?? [])
            .LastOrDefault(c=>c.Type=="image" && c.Data is not null);

        if(image is null)
            throw new InvalidOperationException($"Gemini response {response.Id} has no image content");

        return (Convert.FromBase64String(image.Data!), image.MimeType ?? "image/jpeg");
    }
}