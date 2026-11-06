# Pens, motifs and friezes that can feel their work

A pen can receive a request to use more space, turn away from a saturated passage or join the next bay. Its machine and frieze have separate evidence and accounts. Their requests travel through bounded, signed passages and can succeed, be blocked, or end the drawing still in flight. The judges see the finished design. The native generator runs without any of them.

**[Open the local illustrated study](../../artifacts/local-tension-ornament/Index.html).** Start with the eight authored engines or scrub an actual [pressure timeline](../../artifacts/local-tension-ornament/wave1/Explore.html). The [printed comparison](../../artifacts/local-tension-ornament/plate/Gallery.html) relates two Gojo constructions directly to Meyer's published plate.

| Where to look | What it explains |
| --- | --- |
| [Results and limitations](Results.md) | What changed when a pen received tension, when routing weights changed, and when a fourth owner remembered earlier drawings. |
| [Native pressure law and eight policy bytes](Runtime.md) | Ownership, signed lanes, capacity, acknowledgements, actuators and the final public byte. |
| [Eight authored engines and X controls](Engines.md) | Geometry supplied to search, legitimate stops, crossings, incidence and editable construction. |
| [Eight judgment perspectives](Criteria.md) | Continuity, overlap, joints, spacing, two scales of symmetry, redundancy and source comparison. |
| [Historical sources and retrieval limits](HistoricalResults.md) | Training and held-out reference packets and source-family confusions. |
| [Committee, width and joint measurements](Measurements.md) | How often judges participated, what their combinations achieved, and which sequence comparisons had enough evidence. |
| [First-wave protocol](Protocol.md) | Registered budgets, grouped predictions, controls and scoring margins. |

The [recorded evidence](RecordedEvidence.zip) includes the galleries, source packets, results, finalized machines, inputs and checksums. Extract it into an empty `artifacts/local-tension-ornament` directory on another checkout to restore the local study. The [complete local archive](../../artifacts/local-tension-ornament/CompleteRun.zip) additionally contains scientific search histories and the exact recorded .NET executables. The plate-study original used its own run folder. Its verified copy in the archive is named `plate`.

### Reproduce the primary competition

From the repository root, run the following commands with .NET 10 and a fresh output folder.

~~~powershell
$trial='artifacts/tension-replication'
$runtime='artifacts/tension-replication-runtime'
dotnet publish experiments/OrnamentTension -c Release -o $runtime
foreach ($phase in 'freeze','train','assay') {
    dotnet "$runtime/OrnamentTension.dll" $phase $trial 8
    if ($LASTEXITCODE) { throw "Failed phase $phase" }
}
dotnet "$runtime/OrnamentTension.dll" replay "$trial/replay-inputs.json" "$trial/replayed.json"
Get-FileHash "$trial/replay-expected.json","$trial/replayed.json"
python experiments/OrnamentTension/Analysis.py $trial
~~~

To regenerate the primary gallery, first publish `experiments/OrnamentTensionInspection` using the same runtime, run `Inspections.py RUN INSPECTION_DLL`, then `Gallery.py PARENT_OF_RUN` with the run under `wave1`. The checked local pages work offline. Python, Pillow and Markdown are used to rebuild the review pages. No human preference data were collected in this experiment.
