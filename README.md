# Book Illustrator

Turns a book's text into character portraits and a chapter illustration using the Gemini API.
Five steps, run one at a time: **Style → Characters → Portraits → Chapters → Illustrations**.

---

## Prerequisites

- .NET 9 SDK
- Node.js 20+
- A Gemini API key — https://aistudio.google.com/apikey

No Docker required. Storage is a local SQLite file, and generated images are written to
the local filesystem, so there is nothing to orchestrate. <!-- adjust if you changed storage -->

## Setup

```bash
cp .env.example .env
# open .env and paste your Gemini API key
```

## Run

```bash
./start.sh
```

Backend on http://localhost:5000, frontend on http://localhost:5173.

## Test

```bash
./test.sh
```

Runs backend (xUnit) and frontend (Vitest) suites. See `TESTING.md` for strategy and a real run report.

---

## Environment variables

| Variable | Required | Description |
| --- | --- | --- |
| `GEMINI_API_KEY` | yes | Gemini API key. Never commit this. |
| `GEMINI_TEXT_MODEL` | no | Defaults to `<model-id>` |
| `GEMINI_IMAGE_MODEL` | no | Defaults to `<model-id>` |
| `USE_FAKE_GEMINI` | no | `true` serves canned fixtures instead of calling Gemini. Used during development. |

---

## Architecture

```
client/                 React + Vite
  src/
    api.ts              fetch wrappers
    components/         Stepper, ProjectCard, CharacterCard, ...
    pages/              Identity, ProjectList, ProjectDetail

server/
  Program.cs            Minimal API endpoints
  PipelineService.cs    the five steps + state transitions
  GeminiClient.cs       REST calls to the Gemini API
  FakeGeminiClient.cs   fixture-backed stand-in for local dev and tests
  AppDbContext.cs       EF Core / SQLite
  Models/Project.cs

server.tests/           xUnit
fixtures/               recorded Gemini responses
```

**Request flow.** The client POSTs to run a step and gets `202 Accepted` immediately; the step
runs in the background because Gemini calls take 10–30s. The client polls project state every
2s and renders whichever step is currently running.

**Pipeline state.** A project carries `CompletedSteps` (how far it has got) and `RunningStep`
(what is executing right now, or null). Keeping these separate is what lets a refresh
mid-step read the true state. See `DECISIONS.md`.

**Duplicate protection.** Claiming a step is a single conditional `UPDATE` that only succeeds
when the project is idle and the previous step is done, so a double-click or a second tab
cannot fire the same Gemini call twice. See `DECISIONS.md`.

**Storage.** Project state in SQLite. Book text and generated images on the local filesystem,
served through the API. No external storage. <!-- adjust if you changed storage -->

---

## Pipeline caps

Per the assessment, **max 2 characters** and **max 1 chapter**. Both are enforced server-side
in `PipelineService`, not in the UI.

## AI artifacts

Context files, prompts, and planning notes used while building are committed under
`<path>`. See `DECISIONS.md` for where AI proposals were accepted and where they were overridden.

## Not implemented

Deliberate omissions and what would come next are listed at the end of `DECISIONS.md`.
