# Experiments and retained research

**Start with the [chronological experiment overview](OVERVIEW.md).** It covers
both this folder and `experiments/Minds/`, explaining what each study tried, what happened
and what can be concluded, with links to the detailed evidence.

This folder contains interpretation, prediction, market, swimming,
primitive-number, local-learning, composition and service-economy studies.
Their limitations and failed trials remain part of the record. The table below
provides direct access to the swimming and subsequent learning suite; the
overview includes the earlier studies and Minds as well.

See [Primitive numbers and control](PRIMITIVE-NUMBER-ANALYSIS.md) for the conceptual
mapping from presence, orientation and coarse regions to calibrated quantities,
prediction and control, grounded in these experiments.

| Study | Source and documentation | Retained results / review |
| --- | --- | --- |
| Phototaxis swimmers, compact backend and cognitive trials | [Simulation](Phototaxis/README.md), [browser launcher](Phototaxis.Browser/Start-Preview.ps1), [headless runner](Phototaxis.Runner/Program.cs) | [Earlier runs](../artifacts/phototaxis) |
| Sampling into primitive distinctions | [Sampling emergence](SamplingEmergence/README.md) | [Review](../artifacts/sampling-emergence) |
| Relative signals and decaying reference | [Relative signals](RelativeSignals/README.md) | [Runs](../artifacts/relative-signals) |
| Primitive agency and lookup arithmetic | [Primitive agency](PrimitiveAgency/README.md) | [Runs](../artifacts/primitive-agency), [sweep](../artifacts/primitive-agency-sweep) |
| Primitive foraging | [Primitive forager](PrimitiveForager/README.md) | [Review](../artifacts/primitive-forager) |
| Four local/composite learning experiments | [Primitive learning](PrimitiveLearning/REPORT.md) | [Runs](../artifacts/primitive-learning) |
| Composite emergence | [Composite emergence](CompositeEmergence/REPORT.md) | [Runs](CompositeEmergence/results) |
| Retry, retention and lifecycle adaptation | [Adaptive lifecycle](AdaptiveLifecycle/REPORT.md) | [Runs](AdaptiveLifecycle/results) |
| Local service economy | [Ecology report](UtilityEcology/REPORT.md) | [Review](UtilityEcology/review.html) |
| Progress alongside error | [Progress report](UtilityEcology/PROGRESS-REPORT.md) | [Review](UtilityEcology/progress-review.html) |
| Finite seed and paid candidate admission | [Seed report](UtilityEcology/SEED-REPORT.md) | [Review](UtilityEcology/seed-review.html) |
| Qualitative motif and disposition recognition | [Recognition report](../Gojo/docs/GojoQualitativeRecognition.md), [observation adapter](QualitativeFrieze/QualitativeRecognitionObservation.cs) | [Interactive gallery](QualitativeFrieze/results/qualitative-recognition/gallery.html) |
| Recognition evidence and ordinal placement | [Firmness report](../Gojo/docs/GojoQualitativeRecognitionFirmness.md), [evidence reader](QualitativeFrieze/QualitativeDispositionEvidence.cs), [placement](QualitativeFrieze/QualitativeRecognitionPlacement.cs) | [Interactive gallery](QualitativeFrieze/results/recognition-firmness/gallery.html) |
| Hierarchical pattern recognition | [Report](../Gojo/docs/GojoHierarchicalRecognition.md), [phase grammar](QualitativeFrieze/QualitativePatternGrammar.cs), [layered study](QualitativeFrieze/QualitativeHierarchicalRecognitionStudy.cs) | [Interactive gallery](QualitativeFrieze/results/hierarchical-recognition/gallery.html) |
| Executable ornament program encoding | [Format and findings](../Gojo/docs/GojoOrnamentProgramEncoding.md), [native program](QualitativeFrieze/OrnamentProgram.cs) | [Six samples and runnable archive](QualitativeFrieze/results/ornament-program/gallery.html) |
| Bourgoin geometric ornament constructions | [Findings and sources](../Gojo/docs/GojoBourgoinEncodingStudy.md), [native field assembler](QualitativeFrieze/OrnamentProgramField.cs) | [Source comparison gallery and replayable packets](QualitativeFrieze/results/bourgoin-patterns/gallery.html) |
| Bourgoin source corrections and compartment refinements | [Corrections and checks](../Gojo/docs/GojoBourgoinRefinementStudy.md), [refined diamond fields](QualitativeFrieze/BourgoinPatternLibrary.DiamondFields.cs), [interlocking chambers](QualitativeFrieze/BourgoinPatternLibrary.InterlockingShapes.cs) | [Fifteen cases, first-pass comparisons and overlays](QualitativeFrieze/results/bourgoin-patterns/gallery.html) |
| Bourgoin construction vocabulary | [Shared identities, layers and finite curved profiles](../Gojo/docs/GojoBourgoinVocabularyStudy.md), [exact native construction read](QualitativeFrieze/BourgoinConstructionRead.cs) | [Thirty-six constructions on one sheet](QualitativeFrieze/results/bourgoin-patterns/overview.html) |
| A grammar in colour | [Native enrichment, finer unmeasured authoring and design hierarchy](../Gojo/docs/GojoOrnamentDesignCollection.md) | [108 designs](QualitativeFrieze/results/ornament-designs/gallery.html), [all designs on one page](QualitativeFrieze/results/ornament-designs/overview.html) |

## Fresh checkout

```powershell
git clone -c core.longpaths=true --branch master https://github.com/It3rate/Gojo.git Gojo
cd Gojo
python experiments/ResearchArchive/Restore-LargeResults.py --restore
dotnet test Gojo.slnx -c Release
```

Use Python 3.10 or later and .NET 10. The clone command enables Windows long-path
support for the existing nested sample resources. The browser additionally needs the .NET
WebAssembly workload for its browser project. The archive command restores one
170 MiB forager result from a roughly 3 MiB lossless gzip. It checks both hashes,
keeps identical existing files and refuses conflicting files. All other retained
research records are ordinary Git files. Build outputs and caches are excluded.

Read [archive details](ResearchArchive/README.md) and the
[preservation manifest](ResearchArchive/preservation.json) for the earlier
ignored evidence, including historical test receipts. Source line-ending rules
preserve the current study's captured source hashes on checkout. Old studies
retain their own declared source/provenance and replay limits; publishing them
does not expand those claims.

## Run the latest study or swimming demo

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --recognition-firmness
dotnet run --project experiments/QualitativeFrieze -c Release -- --hierarchical-recognition
dotnet run --project experiments/QualitativeFrieze -c Release -- --ornament-program

dotnet run --project experiments/QualitativeFrieze -c Release -- --bourgoin-patterns
dotnet run --project experiments/UtilityEcology -c Release -- --seed-trial --pilot --phase-length 512 --output experiments/UtilityEcology/results/new-seed-pilot
./experiments/Phototaxis.Browser/Start-Preview.ps1
```

Use a new output directory for future trials. The seed mode is optional; omitting
`--seed-trial` retains the existing ecology modes. Its checked rollback package
remains in `UtilityEcology/backout/seed-trial-20261012/`.

HTML reviews are saved outputs; GitHub's file view shows their source. Read the
Markdown report on GitHub, or serve/open the review locally as its README directs.
No API key is needed for the seed/ecology studies. Optional TypeSafe/Jev trials
read credentials from the environment; credentials are not part of this archive.
