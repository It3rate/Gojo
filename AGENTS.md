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

## Verification Entry Point

For solution-wide verification, run:

```powershell
dotnet test Gojo.slnx
```
