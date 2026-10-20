# Retaining a match across questions

Status: implemented experimental structural reader above the kernel. The
[interactive gallery](../../experiments/QualitativeFrieze/results/retained-match/gallery.html)
shows five cases with a selected match, explicit retained context, follow-up
question and recheck. All questions use native incidence, heading participation
and site bindings. Rendering happens after the reads.

## What is being tested

An existential question asks whether some occurrence satisfies its constraints.
Another question can receive Yes from a different occurrence. That is valid
when asking about any matching part, but does not answer a question about one
part already selected.

This fixture explicitly retains a certified match. A later question must keep
the selected roles' actual sites, edge footprint and complete pattern-site
correspondence. Other roles can still seek matching participants. The retained
scope is part of the question context, rather than an informal use of “that”.

| Case | Retained context | Follow-up outcome |
| --- | --- | --- |
| R01 | A path joined to a loop at a site between its endpoints | Unrestricted endpoint search: Yes, using another path. Retained path: No. |
| R02 | Only the path | Its endpoint can join a different loop: Yes. |
| R03 | The path and its original loop | The original pair does not have endpoint contact: No, despite an unrestricted Yes. |
| R04 | A path with uneven uncalibrated steps | A site between its endpoints joins a loop: Yes. |
| R05 | A path with equal uncalibrated steps | The same between question: Yes. |

Every initial condition remains valid when rechecked after the follow-up,
including after No. Reading a failed endpoint claim does not erase the matched
path or its valid between relation.

R02 and R03 use independently executed realizations of the same layout. The
different answers come from which role bindings are retained. Retaining a path
does not automatically retain its first partner. Retaining both prevents the
partner from being silently replaced.

“Between” is an ordered topological read: the selected site participates after
one path endpoint and before the other. No length, equal subdivision or
calibration is required. R04 and R05 retain the same valid coarse statement
while leaving their different formal reach participation outside the question.

## The native reference

The implementation is
[RetainedMatch.cs](../../experiments/QualitativeFrieze/RetainedMatch.cs), composed
with [OccurrenceComposition.cs](../../experiments/QualitativeFrieze/OccurrenceComposition.cs).

```text
retained match = (actual assessment -> actual supported witness) ->
                 (Closed(selected role slots) ->
                  (actual observation event -> Closed(pins)))

pin = actual role slot ->
      (source-free pattern ->
       ((Closed(actual sites) -> Closed(actual segment rows)) ->
        Closed(pattern site -> actual site)))

continued read = ordinary assessment -> (retained match -> stale evidence)
```

Capture explicitly selects one actual supported witness and the roles to
remember. It is not a universal rule to accept the first match. The fixture
records its explicit witness selection; an application with several useful
interpretations can preserve separate retained contexts.

Remembering only an edge set would be insufficient. A symmetric motif can have
several valid site-role correspondences over the same footprint. Retention
preserves the chosen correspondence so a later selected-leaf question cannot
quietly exchange two indistinguishable leaves. A later matching wrapper can
be new; its actual retained participants and correspondence must agree.

Pinned role slots and their pattern surfaces must participate in the new
question. Dropping a role or replacing its pattern without an explicit
correspondence is Blocked under this exact contract. Narrowing or transferring
the context should be an explicit operation, rather than silent forgetting.

All observations here are non-draining. There is no new material transfer,
numeric carrier state or automatic conversion of a failed claim into tension.

## Certification and transport

The native reader checks the selected source witness, all source role bindings
and their actual incidence, frame/site/edge correspondences, route uses and
the full joint question. It recomputes the local constraints; presence in a
stored Supported scope alone cannot certify a false claim. A shared assignment
checker supplies the law both for fresh assessment and for retained-memory
validation.

A continued capture also verifies that the new witness respects every retained
pin and the observation event from its input context. A fabricated continuation
cannot attach another successful occurrence to an unrelated memory and call it
the same reference. Successful continued reads can themselves be captured to
retain a larger context while preserving their provenance.

Native transport preserves aliases within the restored world. Transporting
memory alone does not bind it to a separately created lookalike observation.
The archive round-trip validates each restored retained context from its native
surface. It does not automatically recreate an executable observation/frame
wrapper or establish correspondence with another live world.

## Different unfinished and false reads

The gallery includes five reference checks:

- A new induction wrapper reading the same observation retains the valid No
  for R01. Match-wrapper identity is not physical occurrence identity.
- A new observation event returns Open with connected Stale evidence, even
  when its geometry looks the same. Tracking across events requires explicit
  correspondence or transfer.
- An unfinished capture remains Open and cannot authorize another occurrence
  as a substitute.
- Zero follow-up assignment capacity remains Open when the search is needed.
- A question that does not admit a retained role is Blocked.

No is a settled answer about the retained context. Open means required work or
evidence is unfinished. Stale identifies the mismatch between observation
events. Blocked identifies an incompatible contract. None silently falls back
to the unrestricted search result.

## What this says about pattern matching

The demonstrated sequence is: find a structural occurrence, explicitly select
it, retain its role bindings, and test more predicates in that context. This
adds identity continuity to local template matching. It is a useful component
of a pattern recognizer, rather than a learned recognizer by itself.

The templates remain supplied, and the fixture does not learn which match is
useful to retain. Matching can discover valid native walks that combine a
drawn path with a neighbouring loop edge. Retaining such a match preserves its
certified structural interpretation; it does not recover the author's intended
path grouping unless additional continuation or construction evidence is
exposed. An initial fixture revealed that ambiguity, and the focused examples
use diagonal contour contacts to make their chosen path interpretation clear.

The retained memory contains full proof history, including its source
assessment and observation. It is a bounded observational checkpoint, not a
compact learned concept or an adaptive forgetting mechanism. Per-edge-role
correspondence is retained as provenance; selecting a particular edge role in
a later constraint is not a feature of this site-selection vocabulary yet.

Candidate matching and correspondence preparation remain eager before pin
filtering. The examples demonstrate semantic continuity, not an execution-speed
improvement. An indexed local read starting from retained participants could
reduce work while preserving this contract; it has not been benchmarked here.

Useful next tests are explicit families of retained alternatives, larger joint
motifs built by extending a retained context, and cross-event correspondence
with changed or missing connections. These should distinguish the valid
structural read from the question used to select and refine it.

## Reproduction

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --retained-match
dotnet test Tests/QualitativeExecution.Tests --filter "FullyQualifiedName~RetainedMatch"
node experiments/QualitativeFrieze/tools/check-retained-match.cjs
dotnet test Gojo.slnx
```

The saved native archive and diagnostic JSON are linked from the gallery.
Drawings preserve the whole scene; blue strokes mark the actual retained
participation through the publisher's explicit segment correspondence. Yes,
No and Open remain plain text and do not determine diagram colour.

The saved run includes five cases and five contract controls. Its complete
native proof world contains 807,051 participants and no measured numeric
carriers or locations. The archive restores all five retained references with
their aliases intact. This size includes search budgets and full proof history;
it is not a compact representation of the five motifs alone.

Browser verification exercises all 20 case stages on desktop and mobile, checks
the five controls and 13 linked assets, and verifies that highlighting preserves
the whole source geometry. The focused structural suite passes 75 tests,
including 31 new tests for capture, continuation, event scope, joint-witness
validation and misleading correspondence aliases.

Solution-wide verification passes 4,584 tests across 12 test projects, including
all 2,154 qualitative execution tests. The adversarial native replacement helper
uses iterative traversal so deeply nested proof scopes can also be tested in
Debug without exhausting the call stack.
