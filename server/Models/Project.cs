namespace BookIllustrator.Models;

public class Project
{
    public Guid Id { get; set; }
    public string UserEmail { get; set; } = "";
    public string Title { get; set; } = "";
    public string BookText { get; set; } = "";
    public DateTime CreatedAt { get; set; }

    public int CompletedSteps { get; set; }
    public int? RunningStep { get; set; }
    public DateTime? RunningSince { get; set; }
    public string? LastError { get; set; }
    public int? FailedStep { get; set; }

    public string? LastInteractionId { get; set; }

    public string? StyleJson { get; set; }
    public string? CharactersJson { get; set; }
    public string? ChaptersJson { get; set; }
}
