# Executable qualitative ornament programs

Experimental format, 2026-10-18. `OrnamentProgram` separates a reusable native
recipe from its selected parameters, finite sampling window and generated
occurrences. The recipe can be saved, restored into a fresh native world and
executed without calling the C# authoring factory that originally assembled it.
This is an executable foothold above the kernel, rather than a permanent file
standard or a universal ornament language.

Open the [six generated examples](../../experiments/QualitativeFrieze/results/ornament-program/gallery.html).
The [readable inspection](../../experiments/QualitativeFrieze/results/ornament-program/encoding.txt)
shows the actual stored motifs, transform substitutions, parameter choices and
phase rules using local human aliases. It is derived from the native graph and
is identical after standalone archive replay. The
[preview](../../experiments/QualitativeFrieze/results/ornament-program/ornament-preview.png)
compares four realizations of the same program.

The implementation is
[OrnamentProgram.cs](../../experiments/QualitativeFrieze/OrnamentProgram.cs).
Its source is ordered Gojo relations and placeholders. A JSON envelope carries
that graph through the existing compact archive; it does not contain an
ornament opcode list, SVG instructions or a separate semantic scene graph.

## Definition, arrangement and hierarchy

A local motif and the disposition of its occurrences answer different
questions. A motif can be shared wherever it participates. Its placement,
selected transform, current drawing state and physical site identities belong
to each occurrence. A larger arrangement can later become a unit in another
arrangement without requiring every lower occurrence to duplicate the reusable
definition.

This is compatible with the earlier
[hierarchical recognition experiment](GojoHierarchicalRecognition.md): a
definition and its instances can participate in multiple levels of reading.
The present format implements generation of one finite, periodic arrangement.
It does not yet serialize the complete recognition service hierarchy or turn
an upward query into a motif-changing action.

Shared definition is also different from shared site. Two occurrences of one
rectangle recipe have independently acknowledged execution, independent heads
and fresh drawing sites. Placing equal formal positions does not join their
sites. A join or negotiated connection still needs an explicit relation and
its witness.

## Native v0 contract

All scopes below use `StructuralConstraint.Closed` unless explicitly open.
Identity-sensitive references select actual participants, rather than similar
shapes found by a recursive search.

```text
program = frame -> (motifs -> (transforms -> (parameters -> layout)))

motifs = Closed(actual UnitArrangementProgram surfaces)
transforms = Closed(actual QualitativePatternTransform surfaces)
parameters = Closed(parameter slot -> Closed(actual alternatives))

cycle = Closed(phase identities) -> Closed(phase -> successor)
layout = (column cycle -> row cycle) ->
         ((U step parameter -> V step parameter) -> rules)
rule = (actual column phase -> actual row phase) ->
       (motif parameter -> transform parameter)

bindings = Closed(actual parameter slot -> actual chosen alternative)
window = (actual starting column phase -> actual starting row phase) ->
         (Closed(column occurrence slots) -> Closed(row occurrence slots))
sample = program -> (bindings -> window)
```

The ordered phase scope and actual successor links must agree, including the
return from the last phase to the first. Every phase pair has exactly one rule.
A finite rule chooses a motif slot and a transform slot; their bindings choose
actual native definitions from the advertised alternatives. The two step slots
select finite formal move words in the same frame.

Column and row occurrence slots bound the sample. They are fresh, distinct
forward placeholders. Preflight rejects a sampling slot that aliases any
definition participant, including a phase, parameter or shared move reach; the
opposed face of a definition slot is not a fresh sampling identity. Generated
physical sites are separate again.
The integer convenience arguments to `OrnamentProgram.Window` create these
finite scopes. Execution follows their entries and the native successor links;
those integers are not uncalibrated distances hidden in motif definitions.

The existing path-program contract remains visible inside each motif:

```text
path = start word -> Closed(chord words)
port = role -> (actual path -> actual chord or terminal path)
motif = frame -> ((paths -> ports) -> (closed paths -> evidence))

move = actual frame direction -> unmeasured reach participant
transform = (source frame -> target frame) ->
            (actual move substitutions -> evidence)
```

Repeated participation in one reach is retained. A transform may map a source
move to several target participations. This supports qualitative substitutions
without asserting calibrated lengths, angles or Euclidean equivalence.

## Executing the source

`Generate(bindings, window)` reads the native inventories, cycles, rules and
bindings, then preflights the entire selected patch before creating any drawing
actuator. It checks actual finite-choice membership, required bindings, complete
move words and the selected transformations.

For each selected row and column it then:

1. Reads the rule for the actual current phase pair.
2. Resolves the bound motif and transform.
3. Creates a fresh `UnitArrangementNode` at the current formal placement word.
4. Lets the existing acknowledged path execution produce strokes and ports.
5. Records the actual row/column slots, rule, selected definitions, placement and
   resulting revision as an application.
6. Advances the formal placement through the selected step word and advances
   the phase through the actual cycle relation.

Definitions are shared across the occurrences that select them. Runtime
wrappers are rebuilt local readers over the native source; they are not stored
as durable object identities in the file. Generated applications retain the
actual chosen source definitions, while the runtime motif readers reuse their
native paths, ports and closure claims.

The run also carries native lowering correspondences from each actual source
motif or transform surface to its rebuilt runtime surface. A blocked runtime
transform can therefore be traced back to the selected source definition
without claiming that the two surface-root identities are identical.

Different samples of one definition can select different advertised transforms
or step words. This changes the bindings, rather than mutating the reusable
definition. More general neighbour-driven negotiation remains available in
earlier experiments but is not a rule of this v0 generator.

## Incomplete and incompatible inputs

An unbound slot is not a zero, an unchanged transform or a default motif. Open
inventories and an open sampling window stay unresolved. A finite choice that
excludes the selected participant blocks it. Supplying a structurally similar
but independently identified definition does not establish membership.

The generator keeps `Blocked` and `Unresolved` separately, and creates no units
when preflight has either kind of remainder. Important cases include:

| Input | Read |
| --- | --- |
| Required parameter has no binding | Unresolved binding obligation |
| Two bindings claim one slot | Blocked ambiguous selection |
| Chosen definition is outside a closed alternative scope | Blocked choice |
| Alternative inventory is open | Unresolved source contract |
| Cycle links disagree with the ordered phase scope | Blocked repetition structure |
| A phase pair has no rule | Unresolved layout obligation |
| A phase pair has two rules | Blocked ambiguous rule |
| Sampling scope is open | Unresolved finite-window contract |
| Sampling slots repeat, or starting phase is foreign | Blocked window |
| Selected transform cannot map the actual motif | Blocked application |
| Complete move substitutions invalidate the motif's contour closure claim | Blocked application before any unit is created |

These are observational and construction obligations. They do not introduce
material `Carrier` tension. If a later system reacts to an obligation by
transferring material or changing a persistent machine, that response needs its
own explicit route and settlement history.

## Save, load and frame correspondence

`Save` captures the `sample` root through `GojoStructureCompactJson` and wraps
the resulting version-3 native archive in an ornament envelope with version 0.
The selected definition, bindings and finite window are saved. Previously
generated strokes, runtime heads and execution histories are not included
unless the source itself deliberately retains them as reachable evidence.

`TryLoad` first restores an independently identified relation/placeholder world.
Within that world, shared references remain shared, including the reference
between a motif inventory, its parameter alternatives and its chosen binding.
Process-global placeholder IDs are not the durable correspondence between
the old and restored worlds.

Execution currently requires the existing standard qualitative motion laws.
The loader uses an exact standard-frame reader and an explicit correspondence
to rebind the restored sample into an executable local frame. This is a narrow
compiler boundary. It does not search loosely for enough familiar-looking
fields, reconstruct missing laws or accept arbitrary semantically equivalent
motion algebras.

The returned sample retains both the restored `Source` and `Correspondence`.
The executable program is derived from that native source. Loading does not
invoke the original C# stimulus factory, interpret names as instructions or
import rendering coordinates. An accepted transport can still contain an
incomplete layout or binding; `Generate` is the separate executable preflight.

The v0 import boundary accepts relation/placeholder source graphs and rejects
carrier-bearing source archives. This is a deliberate limit of the current
reader, rather than a limit of Gojo's general compact archive.

## What compactness means here

Measure these roots separately:

| Root | What its size means |
| --- | --- |
| Reusable program | Shared motif definitions, transforms, finite parameter domains and periodic layout law |
| Program plus bindings and window | A selected executable finite sample description |
| Generated applications and drawing | Expanded occurrences, actual sites and acknowledged output |
| Retained audit graph | Source correspondences and the history needed for the promised replay/read |

A large patch need not duplicate a motif definition in its source file. Its
window still declares a finite number of occurrence slots, and its expanded
output still costs work and storage when requested. Small source size is not
proof of cheap expansion or good recognition. An archive rooted in the entire
audit graph measures retained evidence, not the minimal executable recipe.

Root-relative normalization can be another explicit read with an anchor and
change-of-basis correspondence. It must not merge distinct sites merely because
they have equal local positions. Coarse bins would likewise need an explicit
resolution and retained loss or preimage contract; they are not introduced by
this format.

## Verification and measured observations

The focused run passed all 34 cases: 20 executable-program tests and 14 exact
frame-reader/rebinding tests. They cover restored aliases and fresh worlds,
independent occurrence state/sites, archive-only execution, actual phase and
rule selection, incomplete inputs and false-read prevention. Negative imports
include a missing outer version, null tables/items, missing and duplicate
identities, invalid storage node kinds and missing required references. The
archive-only command path also passed its separate replay check without
constructing the original study. `dotnet test Gojo.slnx` passed **3,943 tests**,
with zero failures or skips, including **1,513** qualitative execution tests.
Desktop and mobile gallery checks passed with no horizontal overflow, script
errors, SVG parsing errors or broken local links. The saved inspection is
identical to the archive-only replay inspection, and the generated previews
were visually checked.

```powershell
dotnet test Tests/QualitativeExecution.Tests/QualitativeExecution.Tests.csproj --filter "FullyQualifiedName~OrnamentProgramTests|FullyQualifiedName~QualitativeMotionFrameReadTests"
dotnet test Gojo.slnx
```

Generate the examples, then replay the saved sample through the separate input
branch, which does not construct the original study:

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --ornament-program
dotnet run --project experiments/QualitativeFrieze -c Release -- --ornament-program --input experiments/QualitativeFrieze/results/ornament-program/program.gojo.json --output experiments/QualitativeFrieze/results/ornament-program/replay
```

The generated [diagnostics](../../experiments/QualitativeFrieze/results/ornament-program/diagnostics.json)
record the following results. Every case has zero blocked and zero unresolved
entries. P02–P06 execute the loaded definition; P01 uses the original definition.

| Case | Change to the shared definition's sample | Accepted units | Native strokes | Expanded execution and retained-audit objects |
| --- | --- | --- | --- | --- |
| P01 | Alternating diamond and turned hook; 6 by 4 window | 24 | 120 | 132,779 |
| P02 | Same bindings; 3 by 3 window | 9 | 44 | 48,291 |
| P03 | Same bindings and window size; different starting column phase | 24 | 120 | 132,779 |
| P04 | Rebind motif and transform slots to mirrored hooks and turned zigzags | 24 | 120 | 130,955 |
| P05 | Bind both motif slots to diamonds and choose shorter step words | 24 | 96 | 116,139 |
| P06 | Choose a row step containing both horizontal and vertical participation | 24 | 120 | 132,151 |

The reusable definition has 622 distinct reachable objects: 503 relations and
119 placeholders. A 6 by 4 selected sample has 665 objects, while the 3 by 3
sample has 657. Every counted source and expanded graph has zero numeric
elements. These are graph object counts through ordered relation links, rather
than minimum description lengths or storage-bit estimates.

The current minified JSON sample archive is 49,984 UTF-8 bytes. Null transport
fields are omitted while required explicit zero fields remain present. Its explicit
frame/import correspondence reaches 2,593 objects. These measurements show the
difference between a shared recipe and expanded acknowledged execution; they
do not show a computation-speed advantage. JSON representation overhead,
source sharing and retained runtime audit are separate optimization questions.

## Current limits and next experiments

The format supplies a periodic two-cycle disposition and authored motif and
transform alternatives. It does not discover a layout, choose an aesthetic
winner, infer full wallpaper groups, normalize arbitrary local frames or learn
unseen motifs. Formal horizontal/vertical participation is not proof that a
calibrated Euclidean space is applicable to an observation.

For a larger data collection, retain the reusable program separately from its
chosen sample and from the blind observation supplied to recognition. Different
construction programs can produce equivalent visible arrangements; keep both
the construction history and any independently justified output correspondence.
An observed motif graph does not uniquely identify the machine that drew it.

Before treating v0 as a broad dataset standard, test recursive composition,
explicit seam and shared-component agreements, alternative sampling schedules,
and the bridge from recognized role graphs to executable constructions. The
existing generation and recognition mechanisms provide footholds for those
tests; the present archive does not serialize them all as one live machine.
Source-book/page references and authoring assumptions should accompany a
reconstruction without selecting execution. Training and evaluation partitions
should distinguish source families and construction recipes as well as individual
parameter samples, so near-identical variations do not stand in for independent
generalization.

A useful next step is to freeze several independent program files, restore
them without their authoring factories, and feed the generated native graphs
to the existing recognizer through its blind observation boundary. Separate
definition transfer, chosen parameters, generated sites and recognized
interpretations in that test. Then a recognizer can request a refinement or
propose a changed binding without confusing observation, definition sharing and
material mutation.
