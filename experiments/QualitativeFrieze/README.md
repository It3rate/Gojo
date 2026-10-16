# Qualitative frieze generators

Authored generators combine the existing qualitative phase machinery with
explicit event routes, unmeasured shared reaches and sampling connections.
Their native source graphs contain relations and placeholders, with no carriers
or numeric locations. Learning and goal optimization are outside this round.

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
dotnet test experiments/QualitativeExecution.Tests -c Release --filter "FullyQualifiedName~PhaseEventNetworkTests|FullyQualifiedName~QualitativeMotionFrameTests|FullyQualifiedName~FriezeFixtureTests|FullyQualifiedName~FriezeRealizationTests"
```
