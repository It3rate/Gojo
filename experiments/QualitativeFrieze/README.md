# Qualitative frieze generators

Authored generators combine the existing qualitative phase machinery with
explicit event routes, unmeasured shared reaches and sampling connections.
Their native source graphs contain relations and placeholders, with no carriers
or numeric locations. The authored gallery below is the generation checkpoint;
a following bounded feedback experiment adds explicit legality and preference
reads without changing the kernel.

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --output experiments/QualitativeFrieze/results
```

The gallery compares a coupled and directly sequenced square wave, a Greek-key
meander, slow/fast motions with drift under two sampling arrangements, three
pauses followed by movement, and alternating short/long reaches. Two explicit
positive measured interpretations render the same qualitative traces.

See [the experiment report](../../Gojo/docs/GojoQualitativeFriezeExperiment.md),
the [SVG gallery](results/gallery.svg), [HTML gallery](results/gallery.html)
and [diagnostics](results/diagnostics.json).
The [PNG preview](results/gallery-preview.png) shows the visually checked gallery.

The [learning experiment report](../../Gojo/docs/GojoFriezeLearningExperiment.md)
describes the next checkpoint: phase-cap and local retracing certificates,
global measured overlap/crossing checks, finite probe objectives and bounded
edits to phase-to-word bindings. The recorded run evaluates 720 candidates,
finds legal examples in all three families, and includes both crossed and
uncrossed examples. A local reversal check is not a global no-retracing proof;
finite probe coverage is not continuous area coverage.

Run the bounded feedback search with:

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --learn --trials 240 --seed 431 --output experiments/QualitativeFrieze/results/learning
```

The output targets are the [learning SVG](results/learning/gallery.svg),
[HTML](results/learning/gallery.html) and [diagnostics](results/learning/diagnostics.json).
The [learning preview](results/learning/gallery-preview.png) was visually checked.
The three profiles reorder fixed motion-word
bags; the meander bag includes one westward word, without a separate meander
reward. Complexity is a hard phase cap. Numeric utility reads probe occupancy,
clearance spread and symmetry; the secondary aspect ratio checks hard geometry
without recomputing that utility. Native search and archive surfaces retain
the grammar, budgets, actual archive transitions and ranking evidence.
The utility's weights are fixed. Phase structure, event routes, sampling,
reach identities and word multiplicities remain unchanged while word order
is searched. Complete retained witnesses can be much larger than the source
generator; this experiment makes no performance or compact-memory claim.

Crossings are properties of continuous periodic ink: two noncollinear
through-lines form a crossing even when they meet at sampled endpoints.
Ordinary joins and T contacts remain distinct from crossings and from
positive-length retracing. This is a straight-through ink criterion; general
bent-strand interleaving needs a separate path-continuation read. X junctions
with extra branches remain crossing sites, with all incident rays retained.
The measured source boundary currently requires whole calibrated source
ticks; fractional source points remain unresolved. Internal intersections and
symmetry maps still retain exact fractions. A finer explicit unit can make
fractional lengths representable as whole source ticks.

`FriezeFixture.Surface` connects the event network, motion frame, output law,
sampling routes, initial phases and reach-order obligations. `Run` bounds an
observation window; phase edges and trigger routes decide what fires. Motion
applications and committed samples remain separate. A sampled chord can hide
intermediate motion without removing it from the native trace.

`FriezeRealization` is a separate interpretation adapter. Its native calibration
binds actual axis/reach pairs to positive measured spans. Display names, pixels
and diagnostic counts are derived presentation data. They do not select source
behavior. A drawing is evidence for that calibration, rather than a proof of
all geometric symmetries or all possible unmeasured embeddings.

Run the focused experiments with:

```powershell
dotnet test experiments/QualitativeExecution.Tests -c Release --filter "FullyQualifiedName~Frieze|FullyQualifiedName~QualitativeCoverageTests|FullyQualifiedName~PhaseEventNetworkTests|FullyQualifiedName~QualitativeMotionFrameTests"
```
