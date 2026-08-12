<!--
  HOW TO USE THIS FILE
  ====================
  These are headings and prompts only. Write each entry YOURSELF, in your own voice,
  at the moment the decision happens — not at the end.

  Each entry: a heading, then 3-6 sentences covering
    - who proposed it (you, or the AI)
    - who pushed back and on what grounds
    - where you landed
    - what it cost you — the con you accepted, stated plainly

  Delete every HTML comment before you commit. Delete any heading you did not actually
  face, and add ones you did. 4-6 real entries beats 8 thin ones.

  At least 3 entries must be places where AI output was wrong, unsafe, or overcomplicated
  and you did something else. Those are marked below.
-->

# Decisions

## Stack and storage

<!--
  Required by the brief (§06). Cover: what you picked, what else was genuinely on the table,
  why this one, and the limits you accepted.
  If SQLite: what does it not give you? (single writer; one instance only)
  If JSON files: how did you make concurrent writes safe, and what is still unsafe?
  Either way: the brief explicitly blesses JSON files, so say why you went the way you went.
-->

## Modelling pipeline progress

<!--
  Required by the brief (§06). How does a project know where it is?
  Why more than one field? What breaks if you collapse them into a single status enum?
  What does a refresh mid-step have to read correctly?
  What is the cost of the shape you chose?
-->

## Stopping duplicate execution on refresh

<!--
  Required by the brief (§06). What actually guarantees a second tab or a double-click
  does not fire the same Gemini call twice?
  Name the mechanism. Say what it relies on. Say where it stops working.
-->

## Recovering a stranded step

<!--
  What happens when the server dies mid-call? How does the user get unstuck without
  someone editing the database by hand? What threshold did you pick and why that number?
-->

## Passing the book to Gemini once

<!--
  Chaining, file upload, or something else? What do you persist to make it work across
  a restart? What happens when that handle expires or is lost?
-->

## Model choice

<!--
  Which text model, which image model, and why those. The brief asks for this specifically (§5.3).
  Anything you learned about the image model's limits that shaped the choice?
-->

---

# Where I overrode the AI

<!--
  The brief calls this "the single strongest signal in the whole submission" (§2.3).
  At least 3. Each one: what the AI produced, what was wrong with it, what you did instead.
  Be specific enough that it is obviously a real event — name the file, the endpoint,
  the pattern. Vague entries read as invented.

  The push-back goes both ways, so if the AI caught a mistake of yours, that belongs
  somewhere in this file too — it scores.
-->

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
