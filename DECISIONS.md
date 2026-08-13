
# Decisions
## Stack and storage
**Who proposed:** brief suggests JSON file is best fit. I considered both
**Decision:** SQLite + EFCore
**Reasons:**
- Database can makes sure nothing slips in between the check and the write, no coding need
- SQLite writes are all-or-nothing. If the app dies mid-write, data stays at the previous state --> No loss. JSON only safe if I get every single write path right
- 24 hrs, I stayed with tools I already know so I could spend time on parts that actually matter most 
**Trade-offs:** 
- File .db need tools to open. With JSON, "cat" show the whole project state: easier to debug or review
- When cloning the project, migration could be error, lead to can't open the project
==> JSON file seems better fit for the project, I know I chose the heavier one than brief suggests. But I traded that simplicity for not having to write concurrency control under time pressure

## Right-sizing the structure
**Who proposed:** I proposed 3-layered architecture with repository, Claude pushed back on 3 ground: 
- DbSet is already a repository
- Conditional UPDATE can only do by SQL, wrapping it make the abstraction meaningless
- Do not mock repository on something need to be test 
**Decision:** No Repository, no separate domain project. Endpoint in Program.cs, logic in PipelineService, service talks to DbContext directly
**Reasons:**
- Keep PipelineService: For not pushing logic into endpoints so I can test rules like "step 3 must be refuses when only step 1 is done" by calling the service in 3 lines. If that logic live in endpoint, the same test will need a running web server
- 2 entites + 5 operations: 3-layered is over-engineering
- Less code between app and database means fewer places for bug while I'm working fast
**Trade-offs**: 
- First I sketched this as a raw SQL string, which compiler cant check. Then switched to ExecuteUpdateAsync so column names go through a lambda, same single atomic UPDATE underneath
- Rules like step order and 2-character max live as "if" checks inside service, not in "Project" class itself --> nothing stops a new endpoint from changing a project directly and skipping them
- Tests run by real SQLite file --> slower, need to clean up after each testing
- This structure works good with 5 steps, if project grow a few times bigger, splitting will be needed

## Modelling pipeline progress
**Who propose**: I ask Claude to lay out the options for where grogress gets written and how to store images that arrive one at a time
**Decision**:
- One RunStepAsync in PipelineService dispatches to the 5 step methods and
  writes the end of a step — CompletedSteps on success, LastError on failure,
  clearing RunningStep either way. The start of a step is written separately by
  the claim UPDATE in the endpoint
- Images get their own table
**Reasons**:
- Splitting start and end this way is deliberate: the start has to be atomic to block
  a second tab, the end does not. Everything after the claim goes through 1 function, so the state machine is in one place instead of five
- Step 3 makes portraits one at a time. A row per image lets me save each one as it lands--> a refresh mid-step shows what's already done
- A JSON column=read-modify-write per image, the race I avoided elsewhere
**Trade-offs**:
- RunStepAsync is a hot spot --> a bug there will break 5 steps
- 2 places write RunningStep, the endpoint sets it, the service clears it. If they ever disagree, a project can look busy when nothing is running. This is the same seam the stranded-step recovery below has to cover
- 2 entities instead of 1, reading full project state need a join
- Step and Index=plain ints so nothing stops a nonsense value

## Preventing duplicate execution
**Who propose**: Me, I wrote it in PLAN.md before starting to code
**Decision**: A single conditional UPDATE claims a step, written with EF Core's ExecuteUpdateAsync rather than a raw SQL string, claimed==0 -->refuse
**Reasons**:
- 1 statement does 3 job: order (CompletedSteps==step-1), no duplicate(RunningStep==null), reclaiming a stranded step (RunningSince < staleThreshold)
- Check and write at the same statement, no window between them. Anything that read first and then write like plain C#, optimistic concurrency with a row version - reopen that window or move the problem into exception handling
- No new state need 
**Trade-offs**:
- Protection ends at the process boundary --> 2 instances against the same SQLite file would both be able to claim
- claimed==0 collapses several different refusals into 1 outcome --> client cant tell which error they facing
- The stale-threshold clause deliberately weakens the guard: inside that window the step is protected, past it a genuinely-still-running step can be claimed again, see below

## Recovering a stranded step
**Who propose**: Claude offered 2 shapes - a separate force-entry endpoint or folding it into '/run'
**Decision**: Folded into '/run', the claim UPDATE also matches a RunningStep who RunningSince is older than 5 minutes, same single statement
**Reasons**:
- Retrying a stuck step and running a fresh step are the same action to user, so they're 1 request + 1 code path
- Keep the claim atomic, a separate endpoint clearing RunningStep would reopen the gap the conditional UPDATE exists to close
- Nothing manual needed to unstick a project
**Trade-offs**:
- The 5-minute threshold is a guess. It is not there to cover a slow Gemini call — HttpClient times out at 60s and the failure is caught and cleaned up long before 5 minutes. It covers the narrower case where the process is alive but stuck somewhere else: a blocked SaveChangesAsync, or a bug that never returns. If that ever happens and the step is still genuinely running when the flag ages out, a second click claims it and I pay for two Gemini calls
- No way to tell a dead process from a slow one
- User gets no gisnal it happened, the step just becomes runnable again

## Passing the book to Gemini once
**Who propose**: Brief offer 2 options: conversation chaining vs file upload with a reference
**Decision**: Chaining
**Reasons**:
- Chaining carries the whole conversation, not just file. Step 4 need to reference the character produced in step 2 --> Chaining=free. With file upload I only get the book back, also need to re-inject the character JSON into every later prompt itseft
- Verified. When recording fixtures, I generated a portrait, took the interaction_id, asked for a scene image using that id, the character came back as the same persion
- It's a raw HttpClient either way, chaining is one extra field in the body, while File API would need implementing a separate upload flow
**Trade-offs**:
- Cant find a documented lifetime for an interaction, while Files API states 48 hours --> if chains expire similarly, a project that sits unfinised overnight would fail on its next step and stay stuck (for retry sending the same dead id). But nothing lost, every ealier step that are completed are stored locally, the chain could be rebuild by re-sending a fresh call, I just didnt write that path
- The context lives on Google's server, I only have the id --> I cant inspect what the model is carrying, of the chain breaks there's no way to rebuild it
- Carrying the conversation seem like the reason that model generate 4 images when I only ask for 1, it continued the conversation on its own initiative
---

# Where I overrode the AI

## 1. Unflagged extra image generations
Claude Code ran an image call while building and reported as passing. I re-ran it myself and found the model had generated 4 images for 2 characters --> double the paid and fixtures have a shape I dont intented to build
**What I did**:
- tighten the prompt to ask for 1 image
- re-recorded the fixtures
- (defensive coding) make GenerateImageAsync take the last image if output is several, last rather than first because the model labels the early ones as drafts and the later one as final
**Trade-offs**:
- I verified the tight prompt once, cant guarantee the model will always return 1 single output, this only reduces the problem, not remove it
- A prompt constraint is a request, not a limit. A real fix would cap spend per step, which I didnt build

## 2. Honestly I dont have much time for this project, so I didn't argue much with Claude Code, most of the things I already plan before writing code in CLAUDE.md and PLAN.md
---

# If I had one more day
- TDD (never used it; 24h is the wrong time to learn a new process)
- the happy-path integration test across all 5 steps
- multi-instance safety
- what happens when the chain/file handle expires
