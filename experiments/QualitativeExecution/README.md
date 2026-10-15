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

Run tests and the diagnostic benchmark from the repository root:

```powershell
dotnet test experiments/QualitativeExecution.Tests/QualitativeExecution.Tests.csproj
dotnet run --project experiments/QualitativeExecution/QualitativeExecution.csproj -c Release -- --iterations 2048 --output experiments/QualitativeExecution/results
```
