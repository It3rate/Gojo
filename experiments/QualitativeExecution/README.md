# Qualitative execution experiment

The initial supplier publishes only detector categories. No measured scalar,
numeric zero or unit is stored in its observation graph.

```csharp
var fixture = new ThresholdFixture();
var view = fixture.Forward;
var source = new Placeholder();
var observation = view.Observe(source, new Placeholder(),
    StructuralConstraint.Closed(view.Above),
    StructuralConstraint.Closed(view.Falling));
var nativeRead = view.ReadNative(observation);
var compiledRead = view.ReadCompiled(observation);
```

Both reads support the same `Approaching` participant, retaining the detector
publication and native law as evidence. Unmeasured trend should instead be
`view.TrendDomain`, preserving all three alternatives. Within the threshold band
does not mean numeric zero. An optional later measured adapter is separate from
this initial interface.

See the [experiment report](../../Gojo/docs/GojoQualitativeExecutionExperiment.md)
for native shapes, assumptions, uncertainty controls, query behavior and limits.
The [relative reference follow-up](../../Gojo/docs/GojoRelativeReferenceExperiment.md)
distinguishes fixed-marker, previous-sample and longer-window comparisons.
The [readiness assessment](../../Gojo/docs/GojoStructuralAlgebraReadiness.md)
maps these results to topology, pattern, memory and closed-loop experiments.
The [laws and dynamics checkpoint](../../Gojo/docs/GojoStructuralLawsAndDynamicsExperiment.md)
adds finite equation checks, shared landmarks, event-based stock and feedback,
and a minimal categorical light reflex. Its contracts and limits are explicit;
it introduces no new kernel primitive.
The [resolution and schedule checkpoint](../../Gojo/docs/GojoResolutionAndScheduleExperiment.md)
connects finite resolutions and independent observation schedules, then attaches
measured interpretation to the same state and landmark identities. These routes
also compose through the existing categorical reflex.
The [large compute comparison](../RefinementPerformance/README.md) exercises
connected native, compiled and measured workloads and records a repeatable
performance baseline, including the new 64-identity finite batch compiler.
The [conditional and temporal mechanics checkpoint](../../Gojo/docs/GojoConditionalTemporalMechanics.md)
adds question-directed suppliers, explicit finite action selection, conditional
packet passages, driven phase recurrence and shared motif generation/recognition.
Its small rule set separates answers, permission and effects, and tests uncertain
branches without implicitly forking participation into every possible future.
The [qualitative frieze experiment](../QualitativeFrieze/README.md) then exercises
these mechanics with independent and triggered motions, unmeasured reaches,
explicit sample grouping and separate measured drawings.

Run tests and the diagnostic benchmark from the repository root:

```powershell
dotnet test Tests/QualitativeExecution.Tests/QualitativeExecution.Tests.csproj
dotnet run --project experiments/QualitativeExecution/QualitativeExecution.csproj -c Release -- --iterations 2048 --output experiments/QualitativeExecution/results
```
