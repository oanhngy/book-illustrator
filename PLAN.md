# Plan

Blocks are ordered by risk, not by layer. Each ends with a commit and a `DECISIONS.md` entry
written the same hour — never backfilled.

Ship each block to its acceptance criteria and stop. Polish is the last block, if there is one.

---

## Core design

Everything hard in this project reduces to one table and one statement.

```csharp
public class Project
{
    public Guid Id { get; set; }
    public string UserEmail { get; set; }
    public string Title { get; set; }
    public string BookText { get; set; }          // stored locally so the UI can show it

    public int CompletedSteps { get; set; }        // 0..5 — how far it has got
    public int? RunningStep { get; set; }          // null = idle
    public DateTime? RunningSince { get; set; }    // detects a stranded step
    public string? LastError { get; set; }
    public int? FailedStep { get; set; }

    public string? LastInteractionId { get; set; } // Gemini chaining handle

    public string? StyleJson { get; set; }
    public string? CharactersJson { get; set; }
    public string? ChaptersJson { get; set; }
}
```

Two fields, not one status enum, because "finished 2 steps" and "currently running step 3" are
independent facts. A refresh mid-step has to read both correctly.

**Claiming a step** — the single mechanism behind in-order execution *and* duplicate protection:

```sql
UPDATE Projects
SET    RunningStep = @step, RunningSince = @now, LastError = NULL
WHERE  Id = @id
  AND  RunningStep IS NULL
  AND  CompletedSteps = @step - 1
```

Zero rows affected means either the step is already running or its predecessor has not
finished. Both are correct refusals. Check-and-write happen in one indivisible statement, so
there is no window for a second tab to slip through.

The endpoint returns `202 Accepted` and runs the step on a background task (Gemini takes
10–30s). The background task needs its own DI scope — reusing the request's `DbContext` will
throw once the request completes.

On success: persist the result, set `LastInteractionId`, increment `CompletedSteps`, clear
`RunningStep`. On failure: record `LastError` and `FailedStep`, clear `RunningStep`, leave
`CompletedSteps` untouched so the step is retryable on its own.

If `RunningSince` is older than 5 minutes, the API surfaces `canForceRetry: true` and the UI
offers a control that clears `RunningStep`. That is the answer to "nothing stuck forever".

---

## Block B — Gemini layer

**Goal: be able to build the whole app without spending quota.**

- [x] 1. Record real fixtures first — one structured-JSON response, one image response — into
  `fixtures/`. Read the actual response shape from them; do not assume it.
- [x] 2. `IGeminiClient` with `GenerateJsonAsync(prompt, previousInteractionId, schema)` and
  `GenerateImageAsync(prompt, previousInteractionId)`.
- [x] 3. `FakeGeminiClient` reads fixtures and delays 15s. Registered when `USE_FAKE_GEMINI=true`.
- [x] 4. Real `GeminiClient` over `HttpClient` — can land at the end of the block.

**Done when:** an endpoint calling `IGeminiClient` returns a parsed result after ~15s with the fake registered.

**Decision to record:** chaining vs file upload - choose what

---

## Block C — Backend

- [x] 1. Identity: `POST /api/auth` takes email + name, upserts, returns something the client can hold.
  No passwords — the brief allows this.
- [x] 2. `POST /api/projects`, `GET /api/projects`, `GET /api/projects/{id}`.
- [x] 3. `POST /api/projects/{id}/steps/{step}/run` — claim, then background task.
- [ ] 4. `PipelineService` with five step methods.
- [ ] 5. Caps enforced here: truncate to 2 characters, 1 chapter.
- [ ] 6. Portraits generated one at a time, each persisted immediately so the UI can show them
  arriving individually.
- [ ] 7. Images written under `data/images/{projectId}/`, served through our own endpoint.

**Done when:** all five steps complete against the fake; a mid-step refresh shows the running step; two concurrent run requests produce one Gemini call.

**Decisions to record:** what is persisted to survive a restart, and what happens when that handle expires; the two-field progress model; the conditional-UPDATE guard.

---

## Block D — Backend tests

Four tests against a real SQLite file in a temp directory. Mock Gemini, never the database.

- [ ] 1. Step 3 is refused when only step 1 is complete
- [ ] 2. Two concurrent run requests → Gemini called exactly once
- [ ] 3. A failing step clears `RunningStep`, records the error, leaves `CompletedSteps` unchanged
- [ ] 4. A stale `RunningSince` surfaces `canForceRetry`

Test 2 is the most valuable test in the submission.

---

## Block E — Frontend

Order: identity → project list → create project → project detail → stepper → polling →
loading/error/empty states → force-retry → polish.

- [ ] 1. Poll `GET /api/projects/{id}` every 2s while `RunningStep != null`.
- [ ] 2. Loading must name the step in progress, not a generic spinner.
- [ ] 3. Errors must be per-step and retryable from the UI.
- [ ] 4. Empty states for no projects and for a project with no results yet.
- [ ] 5. Layout must not jump as results arrive.

`app-demo.html` in the brief is a visual reference to match or beat, not a layout to copy.

**Decision to record:** polling over SSE at this scope.

---

## Block F — Frontend tests

Two component tests only:
- [ ] 1. Stepper renders done / current / pending correctly
- [ ] 2. Project detail renders the error state and a retry control

---

## Block G — Real run

- [ ] 1. Switch to the real client.
- [ ] 2. Run all five steps on a short chapter.
- [ ] 3. Verify by hand: refresh mid-step, second tab, double-click, kill and restart the server,
  force a failure.
- [ ] 4. Paste real test output into `TESTING.md`.
- [ ] 5. Fix only what is broken enough to matter.

---

## Block H — Packaging

- [ ] 1. `start.sh` and `test.sh` verified from a clean clone.
- [ ] 2. `README.md`, `TESTING.md`, and a final pass on `DECISIONS.md` — at least three genuine
  AI overrides, plus the "one more day" section.
- [ ] 3. Commit `CLAUDE.md`, this plan, and any prompts or spec files used.

---

## Cut list, in order of least pain

1. Responsive/mobile — desktop only, just do not let it break
2. One frontend test instead of two
3. Everything in the brief's bonus section — already out
4. Auto-generating the art style from the text; accept a typed style instead

**Never cut:** resume, duplicate protection, stranded-step recovery, `DECISIONS.md`, or commits
spread across the working session.

---

## Known gaps for "if I had one more day"

Recorded as choices, not apologies:

- **TDD** — never used it; a deadline is the wrong place to learn a new process. Tests were
  written after, aimed at the riskiest logic.
- **Integration test across all five steps** — the fake client makes it cheap; ran out of time.
- **Multi-instance safety** — the conditional UPDATE holds for one process, not several.
- **Expired chaining handle** — an old project whose Gemini context has expired cannot resume.
