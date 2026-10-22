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

The [evidence calculation fixture](../../Gojo/docs/GojoEvidenceCalculationFixture.md)
adds selected finite statistical interpretations over native unmeasured
partitions: conditional model weights, question/response inversion, explicit
dependence, and separate empirical rate support. Exact fractions are derived
diagnostics. The canonical state retains the input partitions and event
correspondences. See [StatisticalEvidence](StatisticalEvidence.cs),
[EvidenceSupport](EvidenceSupport.cs), and the
[inspectable calculations](../QualitativeFrieze/results/evidence-calculation/gallery.html).

[CompatibilityEvidence](CompatibilityEvidence.cs) adds an explicit aggregate
channel over native supported, contradicted and unresolved checks. It averages
one uniformly selected distinct witness; repeated routes add no weight and a
classification conflict remains unresolved. This is a supplied likelihood
interpretation, not acquired support. The
[100-shape weighting experiment](../../Gojo/docs/GojoWeightedShapeEvidence.md)
tests this adapter against the earlier ordinal ranking on fixed pools.

[QualitativeCurve](QualitativeCurve.cs) adds an above-kernel native polynomial
construction over unmeasured motion words, one bounded shared parameter and
actual interpolation dependencies. Its local readers distinguish tangent,
corner and cusp joins, signed bending, conservative inflection and regularity
certificates, and convexity conditional on an explicit oriented interior.
The [mixed curve study](../../Gojo/docs/GojoQualitativeCurveEvidence.md) exposes
sparse evidence and compares receiver completions without private source handles.

Run tests and the diagnostic benchmark from the repository root:

```powershell
dotnet test Tests/QualitativeExecution.Tests/QualitativeExecution.Tests.csproj
dotnet run --project experiments/QualitativeExecution/QualitativeExecution.csproj -c Release -- --iterations 2048 --output experiments/QualitativeExecution/results
```
