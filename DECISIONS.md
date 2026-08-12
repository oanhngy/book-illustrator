
# Decisions

## Stack and storage
Who proposed: brief suggests JSON file is best fit. I considered both
Decision: SQLite + EFCore
Reasons:
- Database can makes sure nothing slips in between the check and the write, no coding need
- SQLite writes are all-or-nothing. If the app dies mid-write, data stays at the previous state --> No loss. JSON only safe if I get every single write path right
- 24 hrs, I stayed with tools I already know so I could spend time on parts that actually matter most 
Trade-offs: 
- File .db need tools to open. With JSON, "cat" show the whole project state: easier to debug or review
- When cloning the project, migration could be error, lead to can't open the project
==> JSON file seems better fit for the project, I know I chose the heavier one than brief suggests. But I traded that simplicity for not having to write concurrency control under time pressure

## Right-sizing the structure
Who proposed: I proposed 3-layered architecture with repository, Claude pushed back on 3 ground: 
- DbSet is already a repository
- Conditional UPDATE can only do by SQL, wrapping it make the abstraction meaningless
- Do not mock repository on something need to be test 
Decision: No Repository, no separate domain project. Endpoint in Program.cs, logic in PipelineService, service talks to DbContext directly
Reasons:
- Keep PipelineService: For not pushing logic into endpoints so I can test rules like "step 3 must be refuses when only step 1 is done" by calling the service in 3 lines. If that logic live in endpoint, the same test will need a running web server
- 2 entites + 5 operations: 3-layered is over-engineering
- Less code between app and database means fewer places for bug while I'm working fast
Trade-offs: 
- "UPDATE" is written in plain string --> compiler can't check
- Rules like step order and 2-character max live as "if" checks inside service, not in "Project" class itself --> nothing stops a new endpoint from changing a project directly and skipping them
- Tests run by real SQLite file --> slower, need to clean up after each testing
- This structure works good with 5 steps, if project grow a few times bigger, splitting will be needed

## Modelling pipeline progress


## Preventing duplicate execution


## Recovering a stranded step


## Passing the book to Gemini once


## Model choice



---

# Where I overrode the AI

## 1.

## 2.

## 3.

---

# If I had one more day

<!--
  One short answer. This is where deliberate omissions go — framed as choices with reasons,
  not apologies. Things you already know belong here:
    - TDD (never used it; 24h is the wrong time to learn a new process)
    - the happy-path integration test across all 5 steps
    - multi-instance safety
    - what happens when the chain/file handle expires
    - anything from §08 you skipped on purpose
  Say what you would build FIRST and why that one.
-->
