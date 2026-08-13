# Testing
## Backend
Real SQLite in a temp file per test, not a mocked data layer — what makes step-claiming safe
is the database's atomicity, so mocking it away would prove nothing about the property under
test. `IGeminiClient` is replaced with a `Moq` mock via DI (`TestWebApplicationFactory`); all
four tests run through the real HTTP endpoints, not a copy of the claim logic — a passing test
means the production code path actually works.

| Test | What it protects |
| --- | --- |
| `Step3IsRefused_WhenOnlyStep1IsComplete` | ordering — a step cannot run before its predecessors succeeded |
| `ConcurrentRunRequests_CallGeminiExactlyOnce` | no duplicate Gemini calls on double-click or second tab — the most valuable test in the submission |
| `FailingStep_ClearsRunningStepAndRecordsError_LeavesCompletedStepsUnchanged` | a failure leaves the project usable and retryable |
| `StaleRunningSince_SurfacesCanForceRetry` | nothing is stuck forever |

## Frontend
Cut for time — see PLAN.md's cut list and "one more day". Block D's backend tests protect
the higher-risk logic (concurrency, resume); the budget did not stretch to both.

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
```
=== backend ===
Passed!  - Failed:     0, Passed:     4, Skipped:     0, Total:     4, Duration: 1 s - server.Tests.dll (net10.0)
```

Also ran 16x in a row by hand — zero flakes, including the concurrency test.

**Run on:** 2026-08-13 · **Result:** 4 passed backend, 0 frontend (cut)

## Manual verification
- [ ] Refresh mid-step — project reopens showing the running step, not from scratch
- [ ] Second tab during a running step — shows the in-flight state, does not start a second call
- [ ] Double-click the action button — one Gemini call
- [ ] Kill the server mid-step, restart — project is recoverable without touching the database
- [ ] Force a failure — step is retryable on its own, completed steps untouched
- [ ] Full 5-step run against the real API, end to end
