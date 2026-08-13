using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BookIllustrator;
using BookIllustrator.Gemini;
using BookIllustrator.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace server.Tests;

public class PipelineTests
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Step3IsRefused_WhenOnlyStep1IsComplete()
    {
        using var factory = new TestWebApplicationFactory();
        var (projectId, email) = await SeedProjectAsync(factory, completedSteps: 1);

        var client = ClientFor(factory, email);
        var response = await client.PostAsync($"/api/projects/{projectId}/steps/3/run", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ConcurrentRunRequests_CallGeminiExactlyOnce()
    {
        using var factory = new TestWebApplicationFactory();
        factory.GeminiMock
            .Setup(g => g.GenerateJsonAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await Task.Delay(200);
                return new GeminiJsonResult("fake-interaction-id", """{"style":"test style"}""");
            });

        var (projectId, email) = await SeedProjectAsync(factory, completedSteps: 0);
        var client1 = ClientFor(factory, email);
        var client2 = ClientFor(factory, email);

        var call1 = client1.PostAsync($"/api/projects/{projectId}/steps/1/run", null);
        var call2 = client2.PostAsync($"/api/projects/{projectId}/steps/1/run", null);
        var responses = await Task.WhenAll(call1, call2);

        var statusCodes = responses.Select(r => r.StatusCode).OrderBy(c => c).ToList();
        Assert.Equal([HttpStatusCode.Accepted, HttpStatusCode.Conflict], statusCodes);

        var detail = await WaitForStepToFinishAsync(factory, projectId, email);

        factory.GeminiMock.Verify(g => g.GenerateJsonAsync(
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal(1, detail.CompletedSteps);
        Assert.Null(detail.LastError);
        Assert.Equal("""{"style":"test style"}""", detail.StyleJson);
    }

    [Fact]
    public async Task FailingStep_ClearsRunningStepAndRecordsError_LeavesCompletedStepsUnchanged()
    {
        using var factory = new TestWebApplicationFactory();
        factory.GeminiMock
            .Setup(g => g.GenerateJsonAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Gemini exploded"));

        var (projectId, email) = await SeedProjectAsync(factory, completedSteps: 0);
        var client = ClientFor(factory, email);

        var response = await client.PostAsync($"/api/projects/{projectId}/steps/1/run", null);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var detail = await WaitForStepToFinishAsync(factory, projectId, email);

        Assert.Equal(0, detail.CompletedSteps);
        Assert.Null(detail.RunningStep);
        Assert.Equal("Gemini exploded", detail.LastError);
        Assert.Equal(1, detail.FailedStep);
    }

    [Fact]
    public async Task StaleRunningSince_SurfacesCanForceRetry()
    {
        using var factory = new TestWebApplicationFactory();
        var (projectId, email) = await SeedProjectAsync(factory, completedSteps: 0);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var project = await db.Projects.FirstAsync(p => p.Id == projectId);
            project.RunningStep = 1;
            project.RunningSince = DateTime.UtcNow.AddMinutes(-10);
            await db.SaveChangesAsync();
        }

        var client = ClientFor(factory, email);
        var detail = await client.GetFromJsonAsync<TestProjectDetail>($"/api/projects/{projectId}", JsonOpts);

        Assert.True(detail!.CanForceRetry);
    }

    private static HttpClient ClientFor(TestWebApplicationFactory factory, string email)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Email", email);
        return client;
    }

    private static async Task<(Guid ProjectId, string Email)> SeedProjectAsync(TestWebApplicationFactory factory, int completedSteps)
    {
        var email = $"test-{Guid.NewGuid()}@example.com";
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Users.Add(new User { Id = Guid.NewGuid(), Email = email, Name = "Test User" });
        var project = new Project
        {
            Id = Guid.NewGuid(),
            UserEmail = email,
            Title = "Test Project",
            BookText = "Once upon a time.",
            CreatedAt = DateTime.UtcNow,
            CompletedSteps = completedSteps,
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        return (project.Id, email);
    }

    private static async Task<TestProjectDetail> WaitForStepToFinishAsync(TestWebApplicationFactory factory, Guid projectId, string email)
    {
        var client = ClientFor(factory, email);
        for (var i = 0; i < 50; i++)
        {
            var detail = await client.GetFromJsonAsync<TestProjectDetail>($"/api/projects/{projectId}", JsonOpts);
            if (detail!.RunningStep is null)
                return detail;
            await Task.Delay(100);
        }
        throw new TimeoutException("Step did not finish within 5s");
    }

    // Mirrors the server's ProjectDetail record shape — kept local rather than referencing the
    // internal record in Program.cs, so the test only depends on the public JSON contract.
    private sealed record TestProjectDetail(
        Guid Id, string Title, DateTime CreatedAt, string BookText,
        int CompletedSteps, int? RunningStep, DateTime? RunningSince,
        string? LastError, int? FailedStep, bool CanForceRetry,
        string? StyleJson, string? CharactersJson, string? ChaptersJson);
}
