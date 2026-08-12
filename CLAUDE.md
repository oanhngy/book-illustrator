# CLAUDE.md

Context for Claude Code working on this repo. Read this before doing anything.

---

## What this is

A take-home assessment for an Intern Fullstack role. A web app that turns a book's text into
character portraits and chapter illustrations via the Gemini API, as a five-step pipeline the
user advances one step at a time: **Style → Characters → Portraits → Chapters → Illustrations**.

**Hard deadline. Roughly 18 hours of working time remain.** Plan and prioritise accordingly.

### What is actually graded

The brief states plainly: *"We are not counting features."* The rubric weights **process** over
scope. In descending order of value:

1. `DECISIONS.md` quality — real trade-offs, honestly stated
2. Resume & concurrency correctness — no duplicate Gemini calls, nothing stuck forever
3. Evidence of AI use with human judgement on top — where I overrode you, and why
4. Right-sized solution — no abstractions for features not being shipped
5. Working pipeline end to end
6. UI states: loading / error / empty, handled deliberately
7. Tests that target real risk, not coverage

A polished UI with weak `DECISIONS.md` scores worse than a plain UI with a strong one. When
time is short, protect items 1–4.

---

## Decisions already made — do not relitigate

These were reasoned through before you joined. Raise them only if you find *new* evidence
they are actively breaking something.

| Area | Choice |
| --- | --- |
| Backend | ASP.NET Core Minimal API (.NET 9) |
| Frontend | React + Vite + TypeScript + Tailwind |
| Storage | SQLite + EF Core |
| Structure | `Program.cs` (endpoints) + `PipelineService` + `AppDbContext` — transaction script, two layers |
| **No repository layer** | `PipelineService` talks to `DbContext` directly |
| Gemini | Interactions API over raw `HttpClient`. No SDK |
| Context reuse | Chaining via `previous_interaction_id` |
| Long-running steps | Background task + client polls every 2s. Not SSE |
| Dev workflow | `FakeGeminiClient` serving recorded fixtures with a 15s artificial delay |
| Text model | `gemini-3.5-flash-lite` |
| Image model | `gemini-3.1-flash-lite-image` |

### Why no repository — so you do not suggest one

`DbSet` is already a repository. More importantly, the core of this app is an atomic
conditional `UPDATE`; wrapping it yields a method only SQL can implement, which defeats the
abstraction's only purpose. And mocking that layer in tests would mock away the very thing
under test.

---

## Non-negotiable requirements

From the brief. Violating any of these loses points directly.

- **Send the book to Gemini once.** Never re-send full text on a later step.
- **No duplicate Gemini calls.** Refresh, double-click, or a second tab must not fire the same
  step twice. The guard lives server-side, not in browser state.
- **Steps run in order.** A step cannot start before its predecessors have succeeded.
- **Resumable.** Reopening a project always continues; never restarts from scratch.
- **Nothing stuck forever.** If the server dies mid-step, the user can recover without a DBA.
- **Never auto-retry a Gemini call in a loop.** Retry is user-initiated, per step. The brief
  forbids this explicitly.
- **Caps enforced server-side:** max 2 characters, max 1 chapter. Not in the UI.
- **Images and book text on the local filesystem**, served through our own API. No S3, no CDN.
- **API key from env only.** Never in code, never committed, never in a URL query string.

---

## How I want you to work with me

### Disagree with me when you actually disagree

I want push-back, not agreement. If a decision I state looks wrong, say so directly, give
your reasoning, and propose the alternative. If I overrule you, implement my version without
sulking or re-arguing.

**But do not manufacture disagreement.** Inventing objections to look rigorous wastes the
clock and produces fake entries in `DECISIONS.md` that read as fake. Silence is fine when you
agree.

### Do not write DECISIONS.md for me

This is the highest-value artifact in the submission and it must be in my voice. You may:
- summarise the options and trade-offs you see
- point out a cost I have not accounted for
- flag when a decision just happened and should be recorded

You may not draft the prose. If I ask you to, remind me of this line.

### Flag the trade-off I am not seeing

Every choice costs something. When I state a decision and only give upsides, name the cost.
That is the material `DECISIONS.md` needs and the thing I keep failing to produce unprompted.

### Respect the clock

- Prefer the boring, familiar solution. Novel is a risk I cannot afford today.
- Do not refactor working code for elegance.
- Do not add libraries without asking. `useState` + `fetch` is the state management.
- If something is taking longer than its budget, say so and propose a cut.
- When I ask for something out of scope for the remaining time, say that before building it.

### Scope discipline

Build what is asked. Do not add config options, extension points, interfaces with one
implementation, or handling for cases the brief does not require. If you think an abstraction
is warranted, argue for it first — do not just add it.

---

## Working agreement per task

1. State your plan in a few lines before writing code, unless the change is trivial.
2. Write the code.
3. Tell me what you did **not** handle and what would break it.
4. If the task produced a decision worth recording, say: *"This is a DECISIONS entry — the
   trade-off is X."* Then let me write it.

---

## Layout

```
server/           Program.cs, PipelineService.cs, AppDbContext.cs, Models/, Gemini/
server.Tests/     xUnit + Moq, real SQLite in a temp file
client/           React, Vite, Tailwind
fixtures/         recorded Gemini responses used by FakeGeminiClient
docs/             plan, prompts, AI artifacts
```

Ports: API `5050`, client `5173`. `./start.sh` runs both, `./test.sh` runs both suites.

See `PLAN.md` for the block schedule and acceptance criteria.
