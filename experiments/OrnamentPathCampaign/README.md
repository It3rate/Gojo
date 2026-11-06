# Drawing an ornament from its paths

An ornament in this experiment starts with a blank strip. Each machine has up to three owners that move pens in horizontal, vertical or diagonal directions. One owner can signal another to wait, draw, leave a branch, or attach to a shared endpoint. Changing the machine can therefore change *how* the line is constructed as well as what the drawing looks like. The original experiment with marked sites on a small tile is a separate study.

**Start with the [local visual overview](../../artifacts/local-path-ornament/Index.html)**. The [design sampler](../../artifacts/local-path-ornament/sampler/Explore.html) shows the same twenty programs drawing at two calibrations. Select a drawing to inspect its actual strokes. In the [tournament gallery](../../artifacts/local-path-ornament/wave1/Explore.html), scrub the clock or inspect a joint to see which owner, row and endpoint produced it. The [before-and-after study](../../artifacts/local-path-ornament/shape-refinement/Gallery.html) changes the judges and compares the resulting machines on a common scale.

## What is given, drawn, judged and learned

- **Given to a program.** A repeat anchor, an ordered clock, a starting pulse and references to shared extents. A and B may be stretched independently. The serialized rows contain direction, an extent reference, start and destination bindings and a Boolean law. Coordinates materialize when a particular realization binds those extent slots. The runtime never reads a raster or a desired drawing.
- **Drawn by a program.** Every actual bay executes. The machine records ordered, identity-bearing sites, directed strokes, waits, blocked requests, branches, mergers and changes to writable frame bits. An apparent crossing can have two independent histories. The original binary can draw by itself after the training process is removed. [Encoding and connections](Encoding.md).
- **Judged after drawing.** Twenty-eight readings in six perspectives examine craft, rhythm, field, symmetry, topology and seams. Historic precedents and perceptual studies informed the questions. The weights, preferred ranges and named briefs were supplied for this experiment. [All criteria, sources, counterexamples and limits](Criteria.md).
- **Selected by training.** Candidate machines compete through changes to their rows, bindings, clocks and owner combinations. Most arms receive six editable construction priors. Another arm starts from unrestricted programs. A local frame write can influence later strokes or survive to a new run. Changes to the program's owner count, topology and judge between generations belong to the external training experiments.

[Results in plain language](Results.md) · [How often each criterion mattered](MeasurementResults.md) · [Protocol and predictions](Protocol.md) · [Independent history inspection](HistoryAudit.md).

## Replaying or reproducing locally

The recorded inputs, finalist bytes, blind outcomes and exhibits are in [RecordedEvidence.zip](RecordedEvidence.zip). To restore the displays on another checkout, extract the archive into an empty `artifacts/local-path-ornament` folder. The [complete local archive](../../artifacts/local-path-ornament/CompleteRun.zip) adds the paid search histories and recorded runtimes for historical verification. Follow-up designs and measurements live in the [history refinement](../OrnamentPathRefinement/README.md), [retention](../OrnamentPathReview/Retention.md) and [blinded preference](../OrnamentPathReview/PreferenceProtocol.md) studies.

From the repository root, the following commands run a new primary competition under a new seal. Use a fresh run directory.

~~~powershell
$trial = 'artifacts/path-replication'
$runtime = 'artifacts/path-replication-runtime'
dotnet publish experiments/OrnamentPathCampaign -c Release -o $runtime
foreach ($phase in 'freeze','train','assay') {
    dotnet "$runtime/OrnamentPathCampaign.dll" $phase $trial 8
    if ($LASTEXITCODE) { throw "Failed phase $phase" }
}
dotnet "$runtime/OrnamentPathCampaign.dll" replay "$trial/replay-inputs.json" "$trial/replayed.json"
Get-FileHash "$trial/replay-expected.json","$trial/replayed.json"
python experiments/OrnamentPathCampaign/Gallery.py $trial
~~~

The drawing and scoring runtime requires .NET 10. Building the local illustration sheets requires Python and Pillow. Opening the finished galleries requires only a browser. Human votes from the local A/B view can be exported privately. The two AI observation panels have their own recorded provenance and do not contain human votes.
