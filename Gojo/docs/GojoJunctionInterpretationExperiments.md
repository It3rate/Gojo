# Junction evidence and competing interpretations

Executed exploratory study, 2026-10-20. This follows the
[construction-context experiments](GojoConstructionContextExperiments.md).
It adds a small native observer over supplied segments, actual shared sites
and unmeasured position words. It does not parse images or discover a complete
ornament grammar.

Open the [interactive readings](../../experiments/QualitativeFrieze/results/junction-interpretations/gallery.html)
or the [all-example overview](../../experiments/QualitativeFrieze/results/junction-interpretations/overview.html).
Each example separates the visible drawing, actual source incidence and
candidate interpretations. Selecting a candidate changes the observational
reading and its illustration; it never rewrites the source.

## Why junctions help without deciding everything

A local pattern of rays constrains useful interpretations. Opposed rays may
continue a strand; a third ray may branch or terminate; aligned separated
endpoints may be fragments of one occluded contour. Those are distinct from
proving actual shared identity, common object ancestry, border ownership or
permitted material flow.

Research supports treating these as revisable cues. Rubin's junction
manipulations affected completion judgments, motivating a bounded search
launched by local evidence. The full paper was unavailable in this session;
the primary abstract and indexed discussion were inspected.
[Rubin, 2001](https://pubmed.ncbi.nlm.nih.gov/11374204/).

Tse and Albert give completion examples without conventional tangent
discontinuities, and discuss how context affects border ownership. This
limits any universal rule that a particular junction shape must imply one
occlusion interpretation. The present implementation is an engineering
experiment informed by these distinctions, rather than a tested model of
human perception.
[Tse and Albert, 1998](https://bpb-us-e1.wpmucdn.com/sites.dartmouth.edu/dist/9/2172/files/2020/01/2.pdf).

## Separate source facts, projected support and hypotheses

The existing `QualitativeStrokeGraph` reader supplies actual site incidence,
outward rays and opposed/cooriented ray-pair witnesses. Its letter-like
summaries have narrow meanings:

- T: three actual incident rays with one opposed pair;
- Y: three actual rays with no opposed pair;
- X: four actual rays covered by two opposed pairs;
- L: two distinct nonstraight rays; a right angle is not established.

A perceptual X formed by independent crossing strands therefore differs from
an actual four-arm X junction. A T does not establish absorption or a legal
transfer lane. Original stroke order is preserved even when a junction read
exposes the inverse outward ray of an incoming stroke.

The new native observation contract is:

```text
surface = actual graph -> Closed(actual site -> formal position word)
read    = surface -> (actual junctions -> (projected intersections ->
          (facing gaps -> (candidates -> (blocked -> open)))))

intersection = ((actual stroke -> actual stroke) -> projected point word)
               -> Closed(actual bindings, interval/order proofs)
gap          = ((actual endpoint -> actual endpoint) -> proposed chord word)
               -> Closed(actual ray pair, bindings, displacement proof)
candidate    = actual cue -> (Closed(proposed endpoint continuations) ->
               (Closed(retained cut endpoints) ->
               (Closed(proposed front -> proposed back) -> Open(obligations))))
```

`QualitativeJunctionEvidence` observes this connected structure. The helper
and diagnostic labels do not supply the relationships. The native source and
all reads contain only relations and placeholders.

Intersections are solved only for supported axis-aligned intervals in the
declared independent-axis formal frame. The reader extracts axis participation
and uses native cancellation to compare the proposed point with each interval
end. Same-face residual participation proves ordering without a measured
length. Empty residual identifies endpoint contact; nonempty residuals in both
directions distinguish a strict interior crossing. No distance tolerance or
screen-coordinate intersection enters the read.

Facing gap endpoints must have opposed outward rays and a nonzero collinear
displacement. A transverse stroke across that gap can support a proposed depth
ordering. The alternative of retaining the cuts stays connected to the same
gap evidence. Missing relative order remains open; a known incompatible
position claim or offset gap remains blocked.

Actual graph cuts, open topology, duplicate/missing bindings and zero strokes
cannot quietly certify projected cues. Candidate formation also checks the
supplied chord against its actual endpoint bindings. Foreign or copied cue
occurrences do not become members of an actual evidence read.

## Eight small examples

| Case | Observed construction | Source components | Sites with multiple incident strokes | Incident stroke-pair occurrences | Additional read |
| --- | --- | ---: | ---: | ---: | --- |
| J01 | Two unjoined crossing strokes | 2 | 0 | 0 | One strict projected crossing, two unresolved depth-order proposals |
| J02 | Four arms sharing one actual central site | 1 | 1 | 6 | Actual X incidence; no extra intersection is invented |
| J03 | Three rays sharing a Y-like branch | 1 | 1 | 3 | Actual Y incidence; original object ancestry remains unspecified |
| J04 | Three rays sharing a T-like branch | 1 | 1 | 3 | Actual T incidence; no absorption law is inferred |
| J05 | Separate stem endpoint reaches a rail interior | 2 | 0 | 0 | One projected endpoint contact, no strict crossing or actual T |
| J06 | Facing collinear fragments with a transverse strand in the gap | 3 | 0 | 0 | One gap, a hidden-continuation proposal and a retained-cut alternative |
| J07 | The same separated fragments offset on the other axis | 3 | 0 | 0 | Collinear completion refused with displacement evidence |
| J08 | Two diagonal strokes with no shared site | 2 | 0 | 0 | Selected angular-intersection read remains open |

J01/J02 and J04/J05 have matching visible support under the selected display
interpretation. Their actual endpoint-incidence queries differ. The connection
count always names its basis: shared sites, per-site unordered incident stroke
pairs, connected stroke components or a candidate observational grouping.
It is not a count of all reachable relations in the study: sharing a frame or
an unmeasured reach does not connect independent strokes.

Under J06's proposed endpoint continuation, three source components would form
two observational groups. Under the cut alternative, three remain. The source
still has three components in either case. Neither hypothesis installs a
physical connection, claims a hidden strand exists, or grants a transfer route.
All six candidate readings across the study retain open obligations, even when
the cue that motivated them is completely supported.

## What this contributes to recognition

The examples demonstrate a bounded candidate-producing step between raw
incidence and larger pattern interpretation. It can retain a supported local
cue, a proposed continuation/depth order, an alternative cut interpretation and
the evidence needed for a stronger question.

The next discriminating recognition task is to combine several such sites
through qualitative relative order and compare partial glyph or ornament
templates. Counting T/Y/L/X summaries alone loses arrangement: matching local
counts cannot establish a letter or an ornament. An A-like template should
expose the relationship of its apex, two side continuations and crossbar, while
leaving proportions and incidental embellishment open. That comparison has not
been implemented in this round.

Other open areas include general angular intersections, parallel-band boundary
ownership, explicit rounding/tangent evidence, context-dependent ranking,
remote closure interventions, learned proposal generation and accepted
commitment of a candidate into a new construction. No exact weave, common
object identity, flow direction or aesthetic preference is inferred here.

## Presentation and capture

The renderer supplies illustrative unit lengths, perpendicular screen axes,
colour and line weight after the native reads. Drawing-only views preserve
ambiguity. Actual-topology views add native shared-site and endpoint markers.
Candidate views add purple dashed proposed continuations and orange proposed
foreground strands. Those embellishments depict candidate claims rather than
asserting them as source facts.

The captured archive has 2,439 reachable native participants and zero carriers
or carrier locations. Restoration preserves the reachable participant count.
It retains the native evidence graph; the separate JSON/UI model is a
presentation projection and does not own interpretation.

## Reproduce

```powershell
dotnet run --project experiments/QualitativeFrieze -- --junction-interpretations
dotnet test Tests/QualitativeExecution.Tests/QualitativeExecution.Tests.csproj --filter FullyQualifiedName~JunctionInterpretationTests
node experiments/QualitativeFrieze/tools/check-junction-interpretations.cjs
dotnet test Gojo.slnx
```

Focused tests protect actual versus projected incidence, cut/continuation
alternatives, unchanged source topology, foreign occurrences, unknown reach
order, unsupported angular reads, offset gaps, incompatible endpoint promises,
malformed/open source guards and absence of numeric source participants.

Verified checkpoint: all 4,416 solution tests pass, including nine new focused
tests. Browser checks pass all eight cases, 22 interpretation selections and
43 local references, with unchanged source facts under every selection,
distinct depth-order illustrations, actual/projected marker checks and
desktop/mobile layouts. The native archive round-trip preserves all 2,439
reachable participants and contains no numeric participants.
