# Book Illustrator

Turns a book's text into character portraits and chapter illustrations using the Gemini API.
Five steps, run one at a time: **Style → Characters → Portraits → Chapters → Illustrations**.

## Screenshots
<img width="599" height="215" alt="Screenshot 2026-08-13 at 15 32 53" src="https://github.com/user-attachments/assets/34ea6c09-6dfd-4545-85a3-1ed039cb6ea1" />

<img width="541" height="665" alt="Screenshot 2026-08-13 at 15 33 17" src="https://github.com/user-attachments/assets/7f969327-1d26-411e-8e8e-de8443521484" />

## Prerequisites

- .NET 10 SDK
- Node.js 20+
- A Gemini API key — https://aistudio.google.com/apikey

No Docker. Storage is a local SQLite file; generated images and book text go to the local
filesystem, served through the API — nothing to orchestrate.

## Setup

```bash
cp .env.example .env
# open .env and paste your Gemini API key
```

## Run

```bash
./start.sh
```

Backend on http://localhost:5050, frontend on http://localhost:5173.

## Test

```bash
./test.sh
```

Runs the backend xUnit suite (real SQLite in a temp file, Gemini mocked). See `TESTING.md`
for strategy, what's deliberately not tested, and a real run report.

## Environment variables

| Variable | Required | Description |
| --- | --- | --- |
| `GEMINI_API_KEY` | yes | Gemini API key. Never commit this. |
| `GEMINI_TEXT_MODEL` | no | Defaults to `gemini-3.5-flash-lite` |
| `GEMINI_IMAGE_MODEL` | no | Defaults to `gemini-3.1-flash-lite-image` |
| `USE_FAKE_GEMINI` | no | `true` serves recorded fixtures instead of calling Gemini — used for local dev/tests |
| `STORAGE_PATH` | no | Where the SQLite file and generated images live. Defaults to `./data` |

## Architecture

```
client/src/
  api.ts                 fetch wrappers over the backend endpoints
  components/Stepper.tsx done / current / pending, driven by polling
  pages/                 Identity, ProjectList, CreateProject, ProjectDetail

server/
  Program.cs             Minimal API endpoints (transaction script)
  PipelineService.cs     the five steps + progress/error state transitions
  AppDbContext.cs        EF Core / SQLite
  Gemini/                IGeminiClient, real client, fake fixture-backed client
  Models/                Project, User, GeneratedImage

server.Tests/            xUnit + Moq, WebApplicationFactory against a real temp SQLite file
fixtures/                recorded Gemini responses used by the fake client
```

The client POSTs to run a step and gets `202 Accepted` immediately; the step runs in a
background task (Gemini takes 10–30s) and the client polls every 2s. A single conditional
`UPDATE` claims a step atomically — it's what makes duplicate protection and resume work.
Details and trade-offs are in `DECISIONS.md`.

**Caps, enforced server-side in `PipelineService`, not the UI:** max 2 characters, max 1 chapter.

## Further reading

- `DECISIONS.md` — trade-offs, where AI proposals were accepted or overridden, "one more day"
- `PLAN.md` — the block-by-block build order this was actually built in
- `TESTING.md` — test strategy and a real run report
