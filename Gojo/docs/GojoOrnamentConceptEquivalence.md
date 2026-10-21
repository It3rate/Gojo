# Ornament concepts: preserved arrangement with different appearances

This experiment separates **agreement under an existing question vocabulary**
from **membership in an explicitly declared structural concept**. Both can be
useful; neither means that two pictures must look identical. The concept
question should preserve the participation that matters and expose which
differences it permits.

The [interactive gallery](../../experiments/QualitativeFrieze/results/ornament-concepts/gallery.html)
compares any of 38 executed designs against any other. The
[single-page overview](../../experiments/QualitativeFrieze/results/ornament-concepts/overview.html)
shows all designs. Captions describe construction; native relations determine
admission. Coordinates and SVGs are used only for authoring and presentation.

## What counts as the same concept?

The reference R01 has four closed lozenge lobes at a common hub, each touching
one tip on a closed outer course. Its full published incidence contains 13
sites and 20 segments. At a coarser route resolution it has five branch
landmarks and twelve routes:

- One hub participates in eight route ends.
- Four outer landmarks each participate in four route ends.
- Two distinct routes connect the hub to each outer landmark.
- Four further routes connect those outer landmarks in one cycle.

These obligations are one connected question. Four unrelated claims that a
lobe exists would not establish the common hub, their different participation,
or the relation to the outer course.

The reader demands a bijection of the complete branch-and-route graphs,
including parallel and returning routes and component multiplicity. Every
published segment must belong to exactly one route. It may remove sites with
two incident segments from the selected resolution, replacing their chains
with nonempty routes. It keeps their actual segment footprints in the witness.
An isolated closed cycle retains an arbitrary reference and its nonzero
returning traversal; choosing that reference does not assert a unique start.

This permits extra turns, subdivisions, changed proportions and other route
shapes. It does not permit an extra branch, missing return, detached outer
course or second disconnected occurrence. Those are changed whole
arrangements, even when familiar local motifs remain present.

Whole-copy count is a selected scope promise, not a universal ornament rule.
Another contract could deliberately permit any nonempty number of valid units
while still rejecting unmatched types. This experiment fixes the whole scope.

The more restrictive **every-site question** keeps all published sites and
segments. It still reads undirected incidence: it does not require the same
heading, storage direction or geometric embedding. The existing incidence and
ordered-relation judges remain separate finer reads.

## Actual findings

The local run evaluated all **1,444 ordered pairs** of 38 designs and completed
in **13.0 seconds**, excluding final archive serialization. No main pair read
remained Open or Blocked at the supplied capacities. The 38 designs are authored
variants and counterexamples, not independent samples or learned classes.

For reference R01:

| Selected question | Admitted designs | Interpretation |
|---|---:|---|
| Same answers to the preceding 60 queries | 12 | Agreement under the advertised vocabulary |
| Same whole branch-and-route arrangement | 24 | Turns and degree-two subdivisions may differ |
| Same whole every-site topology | 14 | All site and segment participation retained |
| Same incidence with native headings | 11 | Directional evidence also agrees |
| Same ordered published relations | 10 | Continuation/crossing row order also agrees |

These questions are not a simple strength ranking. The 60-query vocabulary
includes heading and mechanical distinctions that the arrangement question
deliberately leaves open, while missing some whole-arrangement obligations.

Three designs agree with all 60 reference answers but fail its whole arrangement:
R23 has two independent copies, R28 detaches the outer course, and R31 adds a
diagonal tie. They are counterexamples to using this answer vector as a complete
definition. The answers themselves remain true under their original questions.

Conversely, fifteen accepted branch-and-route examples disagree with at least
one earlier answer. Thirteen also fail the heading-sensitive incidence judge.
Their differences are permitted by this newly selected concept; they are not
reconstruction errors under that contract.

R37 **Four-lobe fan in an enclosing field** and R38 **Four-chamber branching
window** make this visible. They preserve the same five branch landmarks and
twelve routes while abandoning the radial rosette silhouette. Each has a
17-row correspondence: five landmark bindings and twelve route bindings. Both
fail the earlier answer-vector and every-site comparisons. This establishes
one shared route concept, not a claim that people necessarily call both
designs rosettes.

For all 1,444 pairs, the branch-and-route question admits 590, the every-site
question 226, the 60-query profile 184, heading-sensitive incidence 148 and
ordered published relations 128. These counts describe the chosen corpus and
read bases; they are not recognition accuracy estimates.

The preceding 416-candidate catalogue contained sixteen variants sharing the
rosette's answer vector, all from the same construction family. Their apparent
uniqueness was therefore conditional on that repertoire. The new counterexamples
show why catalogue agreement and conceptual completeness must stay separate.

## Native encoding and refinement

The question is ordinary connected structure:

```text
question = fresh public template ->
  (Closed(retained template sites) ->
    (Closed(atomic template segments) -> Closed(path template segments)))

route = (actual first site -> actual last site) ->
  Closed(actual segment -> (traversed from site -> traversed to site))

correspondence = Closed(template anchor -> target anchor) ->
  Closed(template route -> (target route -> ordered traversal))
```

Atomic mode retains all sites. Path mode retains branch sites and one reference
per otherwise unanchored cycle. A mixed atomic/path contract remains Open under
this initial reader instead of silently receiving a stronger interpretation.

`Crystallize` stores a standalone coarse template rather than carrying the full
fine graph into every question. For R01 this stores five sites and twelve
segments, with **267 reachable native participants** including the standard
frame and contract scopes. The finer template with identical route permissions
uses 369 participants, so crystallization removes source detail without changing
those read decisions. Its every-site contract has 377 participants; that
larger number belongs to a different preservation promise. These are native
graph counts, not encoded bytes, bits or a global minimum. Returning routes
use a chosen three-segment representation rather than a claim of minimal size.

The correspondence to the original finer source and its discarded complement
remain outside the communicated question root. Alpha-copying and standalone
transport keep author occurrence identities and execution history out of that
root. Headings, continuation and crossing publications are carried outside the
topology promise. The crystallized template intentionally has open heading and
encounter scopes; this does not make its selected topology Open.

Independent native transport succeeded for all 38 executable recipes and all
38 source-free questions. Regeneration preserved all 38 strict published graphs
and all **2,280** query answers. The archive retains native providers and
questions, source-free legacy claim messages with their focus surfaces, all R01
comparison receipts, selected import proofs and crystallization
correspondences. The full 1,444-pair table is a diagnostic projection, not a
native archive of every pair proof.

Focused controls cover unequal graphs with identical degree inventories,
parallel routes versus returning loops, cycle versus open chain, extra
branches/copies/isolated sites, alias identity, whole-edge coverage, incomplete
contracts and exhausted matching capacity. An additional 676-pair control over
the original 26 base families confirms that crystallized and finer route
questions make identical arrangement decisions. Capacity bounds correspondence
attempts; graph normalization and route extraction are not charged to that
budget. Exhaustion produces Open, not absence.

## What remains to specify

This implemented read is an exploratory undirected topology law above the kernel.
It does not establish planar cyclic order around a joint, region ownership,
inside/outside, crossing depth, fill or palette relations, equal reach, metric
angles, radial symmetry or authoring provenance. It neither classifies bitmaps
nor learns a generator. Failed observational claims do not automatically become
material carrier tension.

A useful next question would preserve cyclic order and selected region adjacency
while still allowing flexible route geometry. A separate two-class alternation
contract could admit either checkerboard phase and arbitrary choices of the two
colours while rejecting a third class. That would require explicit fill and
adjacency participation; this run does not yet demonstrate it. Mechanical-path
questions can likewise be selected when construction grouping matters.

The current evidence supports specifying a concept as **what must correspond
plus what may vary**. Extra detail is a further selected question, not an
automatic correction of every accepted alternative.

## Reproduce and inspect

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --ornament-concepts
dotnet test Tests/QualitativeExecution.Tests/QualitativeExecution.Tests.csproj --filter FullyQualifiedName~OrnamentArrangement
node experiments/QualitativeFrieze/tools/check-ornament-concepts.cjs
dotnet test Gojo.slnx
```

Implementation:
[native arrangement reader](../../experiments/QualitativeFrieze/OrnamentArrangementRead.cs),
[authored cases](../../experiments/QualitativeFrieze/OrnamentArrangementCases.cs),
[study runner](../../experiments/QualitativeFrieze/OrnamentConceptExperiment.cs)
and [browser report](../../experiments/QualitativeFrieze/OrnamentConceptGallery.cs).
Recorded decisions are in
[diagnostics.json](../../experiments/QualitativeFrieze/results/ornament-concepts/diagnostics.json).

At this checkpoint, all **22 focused arrangement tests** and all **4,682
solution tests across 12 assemblies** passed. The independent gallery checker
validated every pair, 76 SVGs, 174 desktop/mobile states and 107 local references
without browser errors or overflow failures.
