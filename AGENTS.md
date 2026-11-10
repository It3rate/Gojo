# Gojo Agent Instructions

## Canonical Prompt

Before inspecting code, planning, reviewing, designing, or editing for a Gojo
task, read [`Gojo/docs/GojoSystemPrompt.md`](Gojo/docs/GojoSystemPrompt.md)
completely. It is the canonical repository instruction set and applies to the
entire repository.

Do not replace that read with excerpts, summaries, historical prompts, or this
bootstrap file. If a task changes Gojo's foundational rules, update the
canonical prompt and its design notes together.

## Supporting Sources

- Use current code and focused tests for claims about implemented behavior.
- Use `Gojo/axioms`, `Gojo/ConceptSurface`, and active project documentation for
  intended direction, subject to the authority rules in the canonical prompt.
- Read `Gojo/docs/GojoSystemPromptDesignNotes.md` when revising the prompt or
  investigating why a rule was included. The design notes are explanatory, not
  a competing instruction set.
- Discover current projects and test coverage from `Gojo.slnx` and project
  files; do not rely on copied project lists or historical test counts.

## Encoding Reuse And Promotion

For new encoding work, start with [`Gojo.Encoding/Core`](Gojo.Encoding/Core/README.md).
It is the shared home for reviewed encoding mechanisms; the numeric kernel
remains in `Gojo/Core`. Reuse the core mechanism whose documented contract fits
the task. Packed storage and native relation surfaces are distinct representations;
do not assume they are interchangeable or silently convert between them.

New or uncertain mechanisms may live in the relevant experiment. When they
prove useful, promote a domain-independent implementation into encoding core
with its structural grammar, limits, provenance and focused conformance tests.
Keep domain fixtures, renderers, evaluators and speculative policies in experiments.
Existing experiments may retain their own implementations as historical evidence;
they need not be migrated as part of every promotion. New work should prefer
the promoted core implementation. Do not automatically synchronize old copies.

## Git Integration

The repository default branch is `master`. Resolve the current remote default
before integrating work. Do not create a branch named `main` or `master` merely
because an integration request uses a historical name; merge into the existing
default branch.

Per the user's current workflow, work directly on `master` and commit completed,
verified checkpoints as experiments progress. Create a separate feature branch
only when the user requests one. The user has authorized ongoing GitHub pushes
for completed, verified updates to this repository (23 October 2026). Push such
checkpoints to the existing GitHub remote as part of the workflow. Publication
to other services still follows the authorization for that task.

## Verification Entry Point

For solution-wide verification, run:

```powershell
dotnet test Gojo.slnx
```
