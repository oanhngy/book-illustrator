using Microsoft.EntityFrameworkCore;
using BookIllustrator.Gemini;
using BookIllustrator.Models;

namespace BookIllustrator;

public class PipelineService
{
    private readonly AppDbContext _db;
    private readonly IGeminiClient _gemini;
    private readonly ILogger<PipelineService> _logger;

    public PipelineService(AppDbContext db, IGeminiClient gemini, ILogger<PipelineService> logger)
    {
        _db = db;
        _gemini = gemini;
        _logger = logger;
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
            // Unexpected failure outside the step body itself (e.g. DB unreachable).
            // RunningStep is left set here — it self-heals via the 5-minute force-retry
            // window in the /run endpoint, but nothing surfaces *why* without this log.
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

    // Task 4 điền nội dung thật vào 5 hàm dưới đây.
    private Task RunStyleAsync(Project project) => throw new NotImplementedException();
    private Task RunCharactersAsync(Project project) => throw new NotImplementedException();
    private Task RunPortraitsAsync(Project project) => throw new NotImplementedException();
    private Task RunChaptersAsync(Project project) => throw new NotImplementedException();
    private Task RunIllustrationsAsync(Project project) => throw new NotImplementedException();
}
