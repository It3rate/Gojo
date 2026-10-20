# Construction contexts and possible continuations

Executed exploratory study, 2026-10-20. This follows the
[Gestalt exploration plan](GojoGestaltViewsExploration.md), using simple path
constructions and a shared fan lattice rather than depending on the latest
interior-cell catalogue. It implements bounded construction and observation
experiments, not a general Gestalt recognizer or learned concept system.

Open the [interactive gallery](../../experiments/QualitativeFrieze/results/construction-contexts/gallery.html)
or the [all-step overview](../../experiments/QualitativeFrieze/results/construction-contexts/overview.html).
The gallery supports step buttons, a slider and a component-colour toggle.
The field study shows actual membership, accepted construction and a common
ornament program placed in each selected context.

## Question and result

Does including a construction add useful meaning beyond the current drawing?
The examples distinguish three kinds of context:

- an operative construction exposes actual paths, chord selectors, ports and
  closure promises available for a subsequent edit;
- retained history answers questions about earlier constructions, even when
  those constructions no longer describe the current fine shape;
- an observational grouping selects useful units from the whole source, which
  a further construction can actually consume.

These contexts make different questions answerable. A captured current program
still executes after losing its history. A closed path can expose chord
refinement as well as whole-path replacement. No single construction is shown
to be universally preferable, and a refused selector is not proof that a
different instruction could never perform the requested edit.

For the supplied cursor receiver, the four-chord closed path uses six accepted
actions, including reset and jump; four independently reset sides use twelve.
The retained and captured twenty-chord contours both use twenty-two accepted
actions. These are concrete execution differences and equalities within this
fixture, rather than a general speed benchmark or a learned preference.

## Native contracts

All source meaning resides in connected relations and placeholders. Paths,
starts, chords, shared unmeasured reaches, role publications, replacement
focuses and retained rungs are actual native participants. Captions, strings,
arrays and JSON models describe or display those participants.

The new narrow ordered-support question is:

```text
instruction = (actual source program -> actual target program)
              -> Closed((actual path -> actual chord)
                        -> (actual path -> actual chord), ...)
receipt     = instruction -> (matched -> (blocked -> unresolved))
match       = actual selector pair
              -> (start-equality reduction -> end-equality reduction)
```

`ConstructionSegmentSupport` requires coverage of every actual source and
target chord occurrence. Distinct lookalike occurrences remain distinct.
Copied/foreign paths, missing coverage, open selections, independent reaches,
foreign frames and fabricated favourable receipts cannot establish complete
support correspondence. The read preserves source identity without merging
cursor sites, inventing contact or asserting Euclidean regularity.

Edits use the existing `QualitativeLatticeLadder` contracts. Replacement
preserves the actual carried paths, explicit introduced paths, port
correspondences, withdrawn roles, blocked/open evidence and history. Chord
refinement preserves selected endpoint displacement through native reduction;
it makes no straightness promise. Every presented step is executed through an
acknowledged `UnitArrangementNode` and its native graph-join read.

## Cases

| Case | Construction and follow-up | Observed distinction |
| --- | --- | --- |
| C01 | One closed four-chord path versus four separately reset side paths; try the same actual side-path selector | Complete ordered support correspondence; the side is a path member only in the split construction. Refused selection retains the previous drawing. Four reset paths do not acquire a contour-closure contract from coordinate agreement. |
| C02 | Replace the closed path by independent sides, move one side, then explicitly propose revised side spans and a new closed traversal | A ladder can withdraw closure and traversal constraints while retaining support correspondence, then establish a different closure. The final reclose is an authored proposal, not automatic negotiation. |
| C03 | Refine one coarse chord into five endpoint-preserving chords, repeat the bend around the contour, then repeat the complete unit | Chord count changes 4 → 8 → 20. Fine changes preserve formal endpoint displacement and closure, while changing the contour. Four translated occurrences share the actual detailed chord scope; a native two-phase repeat also executes the same selected motif four times. |
| C04 | Retain the notched contour's history or capture its present structure into a fresh independent world | Both execute the same current drawing. Only the retained ladder answers the actual earlier-square question. Old selectors cannot select the independently captured paths. The external loss receipt is outside the standalone world's internal history. |
| C05 | Read courts, overlapping vertex diamonds or row parents from one 48-chamber fan field; use each read for construction and ornament placement | The chosen organization changes subsequent topology and placement without changing the source. All three use the same actual ornament program. |

The square-like drawings have a selected equal-axis display interpretation.
Their native promises concern formal displacement, common reach participation,
order and closure. Perpendicular screen axes and equal illustrative calibration
do not prove a general geometric square law. A remembered coarse square also
does not certify straight sides of the present notched contour.

## Observation becomes a construction context

The shared source consists of 48 actual triangular chambers in a 4-by-3 fan
patch. Authored proposals produce:

| Read | Groups / constructed roles | Distinct source chambers selected | Complement | Contact passages | Overlap relations | Shared motif placements |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| V01 courts | 12 | 48 | 0 | 17 | 0 | 12 |
| V02 vertex diamonds | 6 | 34 | 14 | 4 | 7 | 6 |
| V03 row parents | 3 | 48 | 0 | 2 | 0 | 3 |

Each row parent consumes four actual lower court reads. Diamond memberships
overlap, so 48 membership occurrences cover only 34 distinct source chambers.
That is repeated observation, not duplicated physical ownership or independent
evidence. The unselected boundary chambers remain in the complement.

The actual selected `OrnamentView` feeds `OrnamentViewNetwork`. Its exposed
contact topology and word-landmark bindings feed `OrnamentNetworkConstruction`.
Each accepted contact becomes an independently acknowledged edge actuator.
The selected equal-share word-landmark read is an explicit additive-basis
interpretation; it is not an inferred universal geometric centre.

One eight-chord diamond-and-spokes program is reused at all 12, 6 or 3
representative landmarks. The actual published binding supplies each placement
word; input port, placement receipt and resulting construction remain connected.
This is a producer-consumer dependency rather than shared ancestry alone.

Only boundary contacts become construction passages. Shared membership remains
an observation channel. The V02 projected strands cross without an invented
central junction; bridge gaps and actual endpoint dots make this visible.

## Presentation, capture and limits

The renderer reads executed native graphs and word bindings. It supplies
screen coordinates, a unit illustrative length for each open reach, colour,
stroke width and framing. Steps share a fixed window within a case; the final
four-unit repeat has its own larger window. The source field is a grey guide
behind each downstream network. Neither SVG geometry nor colour proposes or
validates native memberships or joins.

The combined archive contains 941,389 reachable native participants and zero
carriers or carrier locations. It retains verbose source, read, actuator and
historical witnesses; its approximately 8.8 MB compressed size is not a compact
concept encoding or an efficiency result. Archive restore preserves the
reachable participant count. The diagnostic structural counts are distinct
from conceptual complexity, search effort and perceptual salience.

Automatic proposal discovery, cue weighting, reconstruction of alternate
recipes from a drawing, learned ranking, free-angle polygon geometry and
automatic distribution of corrective change remain undemonstrated here.
Observational mismatch is read evidence; this study does not silently convert
it into material tension.

The next discriminating experiment would hide construction history, supply
several independently justified candidate contexts, and ask different future
tasks. Record retained promises, withdrawn constraints, actual relations
inspected and new bindings required. Then test whether task-conditioned
selection benefits from retaining or reconstructing a particular context.

## Reproduce and verify

```powershell
dotnet run --project experiments/QualitativeFrieze -- --construction-contexts
dotnet test Tests/QualitativeExecution.Tests/QualitativeExecution.Tests.csproj --filter "FullyQualifiedName~ConstructionContextStudyTests|FullyQualifiedName~ConstructionViewStudyTests"
node experiments/QualitativeFrieze/tools/check-construction-contexts.cjs
dotnet test Gojo.slnx
```

Focused tests cover executed native structure, exact versus lookalike identity,
endpoint correspondence, false support receipts, withdrawn closure, open and
foreign focus, standalone history loss, actual lower-read composition,
overlapping membership, accepted topology consumption and shared motif inputs.
The browser check exercises every recorded step in both display modes, sliders,
downloads, local links, the overview and desktop/mobile layouts.

Verified checkpoint: all 4,407 solution tests pass, including 18 new focused
structural tests. Browser verification passes 68 control-state checks and 52
local references, with actual endpoint markers and both non-joined V02
crossings checked separately. The generated native archive restores with the
same 941,389 reachable participants and no numeric participants. Existing
unrelated xUnit analyzer warnings remain in older tests.
