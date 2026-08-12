# Testing

## Strategy

The risk in this project is not in the CRUD and not in the Gemini calls themselves — it is in
the **state transitions around a long-running step**. A step takes 10–30s, during which the user
can refresh, open a second tab, double-click, or lose the server. That window is where data loss,
duplicate spend, and permanent stuck states come from, so that is where the tests point.

Gemini is never called from a test. `FakeGeminiClient` serves recorded responses from
`fixtures/`, with an artificial delay so the concurrency behaviour under test is the same
behaviour that happens in production. <!-- adjust path/name to match your code -->

## Backend

Backend tests run against a real SQLite file in a temp directory, not a mocked data layer.
This is deliberate: what makes step-claiming safe is the database's atomicity, so a test that
mocks the database away would prove nothing about the property it claims to test.

| Test | What it protects |
| --- | --- |
| <!-- e.g. cannot run step 3 when only step 1 is complete --> | ordering — a step cannot run before its predecessors succeeded |
| <!-- e.g. two concurrent run requests, Gemini called once --> | no duplicate Gemini calls on double-click or second tab |
| <!-- e.g. failed step clears RunningStep, records error, leaves CompletedSteps --> | a failure leaves the project usable and retryable |
| <!-- e.g. stale RunningSince surfaces a recovery affordance --> | nothing is stuck forever |

## Frontend

Two component tests, chosen over breadth on purpose — the brief asks for a couple that matter,
not coverage.

| Test | What it protects |
| --- | --- |
| <!-- e.g. stepper renders done / current / pending correctly --> | the user can always see where the pipeline actually is |
| <!-- e.g. error state renders the failing step and a retry control --> | a failure is visible and recoverable from the UI |

## What I deliberately do not test

<!--
  Be specific and give the reason for each. Candidates:
    - E2E — the brief says it is not expected
    - the happy-path integration run across all 5 steps — wanted it, ran out of time,
      listed in DECISIONS.md under "one more day"
    - Gemini response parsing beyond the recorded fixtures — real API shape changes would
      not be caught; accepted
    - styling, layout, responsive behaviour — verified by hand, not worth automating here
    - CRUD endpoints with no branching logic
  Say plainly which of these is a considered omission and which is a time constraint.
  Both are honest; conflating them is not.
-->

## Running the tests

```bash
./test.sh
```

## Test report

<!--
  Paste the ACTUAL output of a real run below — or commit the generated file and link it.
  Not a summary, not a rewrite. Include the failures if there were any, and say what you
  did about them.
-->

```
<paste real output here>
```

**Run on:** <!-- date --> · **Result:** <!-- e.g. 4 passed backend, 2 passed frontend -->

## Manual verification

Automated tests do not cover what a real run feels like, so these were checked by hand
against the live Gemini API:

<!--
  Tick these off for real. They are the exact behaviours §07 grades under
  "Resume & concurrency correctness". Note what you observed, including anything that
  did not work the first time.
-->

- [ ] Refresh mid-step — project reopens showing the running step, not from scratch
- [ ] Second tab during a running step — shows the in-flight state, does not start a second call
- [ ] Double-click the action button — one Gemini call
- [ ] Kill the server mid-step, restart — project is recoverable without touching the database
- [ ] Force a failure — step is retryable on its own, completed steps untouched
- [ ] Full 5-step run against the real API, end to end
