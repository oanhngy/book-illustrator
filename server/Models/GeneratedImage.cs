namespace BookIllustrator.Models;

public class GeneratedImage
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public int Step { get; set; }
    public int Index { get; set; }
    public string ImagePath { get; set; } = "";
    public string MimeType { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
