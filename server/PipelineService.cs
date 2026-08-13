using Microsoft.EntityFrameworkCore;
using BookIllustrator.Gemini;
using BookIllustrator.Models;
using System.Text.Json;

namespace BookIllustrator;

public class PipelineService
{
    private const int MaxCharacters=2;
    private const int MaxChapters=1;
    private static readonly JsonSerializerOptions JsonOpts=new(JsonSerializerDefaults.Web);
    private const string StyleSchema="""{"type":"object","properties":{"style":{"type":"string"}},"required":["style"]}""";

    private const string CharactersSchema =
        """{"type":"object","properties":{"characters":{"type":"array","items":{"type":"object","properties":{"name":{"type":"string"},"imagePrompt":{"type":"string"}},"required":["name","imagePrompt"]}}},"required":["characters"]}""";

    private const string ChaptersSchema =
        """{"type":"object","properties":{"chapters":{"type":"array","items":{"type":"object","properties":{"title":{"type":"string"},"summary":{"type":"string"},"imagePrompt":{"type":"string"}},"required":["title","summary","imagePrompt"]}}},"required":["chapters"]}""";

    private readonly AppDbContext _db;
    private readonly IGeminiClient _gemini;
    private readonly ILogger<PipelineService> _logger;
    private readonly string _storagePath;

    public PipelineService(AppDbContext db, IGeminiClient gemini, ILogger<PipelineService> logger, string storagePath)
    {
        _db = db;
        _gemini = gemini;
        _logger = logger;
        _storagePath=storagePath;
    }

    public async Task RunStepAsync(Guid projectId, int step)
    {
        try
        {
            var project = await _db.Projects.FirstAsync(p => p.Id == projectId);

            try
            {
                await RunStepBodyAsync(project, step);

                project.CompletedSteps = step;
                project.RunningStep = null;
                project.RunningSince = null;
                project.LastError = null;
                project.FailedStep = null;
            }
            catch (Exception ex)
            {
                project.LastError = ex.Message;
                project.FailedStep = step;
                project.RunningStep = null;
                project.RunningSince = null;
            }

            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected failure running step {Step} for project {ProjectId}", step, projectId);
        }
    }

    private Task RunStepBodyAsync(Project project, int step) => step switch
    {
        1 => RunStyleAsync(project),
        2 => RunCharactersAsync(project),
        3 => RunPortraitsAsync(project),
        4 => RunChaptersAsync(project),
        5 => RunIllustrationsAsync(project),
        _ => throw new ArgumentOutOfRangeException(nameof(step)),
    };

    private async Task RunStyleAsync(Project project)
    {
        var prompt =
            "You are an art director for a children's book. Read the following book text and " + "define ONE consistent visual illustration style to use for every image in this book " + "(character portraits and chapter scenes). Describe medium, color palette, linework, " + "and mood in a few sentences.\n\nBook text:\n" + project.BookText;

        var result = await _gemini.GenerateJsonAsync(prompt, project.LastInteractionId, StyleSchema);

        project.StyleJson = result.Json;
        project.LastInteractionId = result.InteractionId;
    }

    private async Task RunCharactersAsync(Project project)
    {
        var prompt =
            $"Identify up to {MaxCharacters} main characters from the book. For each character, " +
            "give a short name and a detailed imagePrompt suitable for generating a character " +
            "portrait consistent with the established art style.";

        var result = await _gemini.GenerateJsonAsync(prompt, project.LastInteractionId, CharactersSchema);
        var characters = JsonSerializer.Deserialize<CharactersDto>(result.Json, JsonOpts)?.Characters ?? [];
        var capped = characters.Take(MaxCharacters).ToList();

        project.CharactersJson = JsonSerializer.Serialize(new CharactersDto(capped), JsonOpts);
        project.LastInteractionId = result.InteractionId;
    }

    private async Task RunPortraitsAsync(Project project)
    {
        await ClearGeneratedImagesAsync(project.Id, step: 3);

        var characters = JsonSerializer.Deserialize<CharactersDto>(project.CharactersJson ?? "{}", JsonOpts)?.Characters ?? [];

        // Chain portraits off each other locally (verified: image->image keeps the same
        // character), but never leave project.LastInteractionId pointing at an image call —
        // a later JSON-schema call (Chapters) chaining off an image-only interaction gets a
        // real 400 from Gemini. Text steps must always resume from the last text interaction.
        var chainId = project.LastInteractionId;
        for (var index = 0; index < characters.Count; index++)
        {
            var character = characters[index];
            var prompt = $"Portrait of {character.Name}: {character.ImagePrompt}. Match the established illustration style.";

            var result = await _gemini.GenerateImageAsync(prompt, chainId);

            await SaveGeneratedImageAsync(project.Id, step: 3, index, result);
            chainId = result.InteractionId;
            await _db.SaveChangesAsync();
        }
    }

    private async Task RunChaptersAsync(Project project)
    {
        var prompt =
            $"Break this book into at most {MaxChapters} chapter suitable for illustration. For each " +
            "chapter, give a title, a short summary, and an imagePrompt for a chapter illustration " +
            "consistent with the established art style and characters.";

        var result = await _gemini.GenerateJsonAsync(prompt, project.LastInteractionId, ChaptersSchema);
        var chapters = JsonSerializer.Deserialize<ChaptersDto>(result.Json, JsonOpts)?.Chapters ?? [];
        var capped = chapters.Take(MaxChapters).ToList();

        project.ChaptersJson = JsonSerializer.Serialize(new ChaptersDto(capped), JsonOpts);
        project.LastInteractionId = result.InteractionId;
    }

    private async Task RunIllustrationsAsync(Project project)
    {
        await ClearGeneratedImagesAsync(project.Id, step: 5);

        var chapters = JsonSerializer.Deserialize<ChaptersDto>(project.ChaptersJson ?? "{}", JsonOpts)?.Chapters ?? [];

        // Same reasoning as RunPortraitsAsync: chain images off each other locally, never
        // persist an image interaction as project.LastInteractionId.
        var chainId = project.LastInteractionId;
        for (var index = 0; index < chapters.Count; index++)
        {
            var chapter = chapters[index];
            var prompt = $"Illustration for chapter '{chapter.Title}': {chapter.ImagePrompt}. " +
                "Match the established illustration style and character designs.";

            var result = await _gemini.GenerateImageAsync(prompt, chainId);

            await SaveGeneratedImageAsync(project.Id, step: 5, index, result);
            chainId = result.InteractionId;
            await _db.SaveChangesAsync();
        }
    }

    private async Task ClearGeneratedImagesAsync(Guid projectId, int step)
    {
        await _db.GeneratedImages
            .Where(g => g.ProjectId == projectId && g.Step == step)
            .ExecuteDeleteAsync();
    }

    private async Task SaveGeneratedImageAsync(Guid projectId, int step, int index, GeminiImageResult result)
    {
        var extension = result.MimeType switch
        {
            "image/png" => "png",
            "image/jpeg" or "image/jpg" => "jpg",
            "image/webp" => "webp",
            _ => "png",
        };

        var relativePath = Path.Combine("images", projectId.ToString(), $"{step}-{index}.{extension}");
        var absolutePath = Path.Combine(_storagePath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
        await File.WriteAllBytesAsync(absolutePath, result.ImageBytes);

        _db.GeneratedImages.Add(new GeneratedImage
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Step = step,
            Index = index,
            ImagePath = relativePath,
            MimeType = result.MimeType,
            CreatedAt = DateTime.UtcNow,
        });
    }

    private sealed record CharacterDto(string Name, string ImagePrompt);
    private sealed record CharactersDto(List<CharacterDto> Characters);
    private sealed record ChapterDto(string Title, string Summary, string ImagePrompt);
    private sealed record ChaptersDto(List<ChapterDto> Chapters);

}
