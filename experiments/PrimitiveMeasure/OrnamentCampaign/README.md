# A local tournament for small ornament machines

A strip of ink marks has a few marks missing, misplaced, or unexpectedly added. A reader moves along the strip and sends one local signal at a time. A second structure can spend stock to add a mark, recover one, or move one. Which pairs of structures learn to do useful work on an unfamiliar strip? Do they still work after we save just their bits and disconnect the trainer?

We ran the competition, opened its blind results, then designed three new tests from what went wrong. The clearest discovery was a tiny reflector that also copied accidents. Giving the reader a correctly bound source let a second machine distinguish the two cases. The attempts to learn a better search strategy were inconclusive. [Read the results and limits](CampaignResults.md).

**Start visually.** Open the [local results inspector](../../../artifacts/local-ornament/primary/Explore.html). Compare the live and cut wire on the window seed. Then switch to the perturbed seed and compare live and frozen sensing. The [drawing gallery](../../../artifacts/local-ornament/primary/Gallery.html) shows what happened when independent horizontal and vertical machines were connected to the same fabric. The [owner-exchange heatmap](../../../artifacts/local-ornament/owner-exchange/Explore.html) lets you plug one trained reader into another writer and inspect the measured difference.

## What runs where

| Part | What it does |
| --- | --- |
| Stored generator | Two ordered tables of one to four Boolean rows each. Sensor rows reference local input ports and earlier rows. The writer reads the exported support, its own prior signals, its site and stock. Writer rows bind passages to stock, the current site, or a neighbor. At most one funded transfer occurs on a beat. |
| Shared substrate | Marks occupy a ring of equal, uncalibrated site steps. The frame can be reversed. A one-beat delay, a missing publication and an uncertain publication have distinct behaviors. Site protection, stock and available attempts bound what can settle. |
| Gojo runtime | Packed endpoint references, truth nibbles and passage endpoints specify each machine. A 512-entry table compiled from the Gojo bit relations evaluates Boolean support. A second table implements the controlled passage. A direct evaluator reads the same relations for verification. |
| Supplied observations | The apparatus binds local occupancy, gap comparisons, reflected occupancy, original ink and goal bits to public ports. The reader may ignore any of them. Binding a reflected site already supplies substantial geometric knowledge. |
| External training and judging | The local host generates seeds and mutations, ranks output and keeps histories. A machine receives neither its score nor a completed target picture while it runs. Search budgets, indexes, ensemble statistics and timing belong to the apparatus. |

The four goals are to balance the spacing at a given count, approach a target gap, complete a reflection, or repair a repeat boundary. Unknown metric lengths stay open. A count of sites is possible because this study explicitly supplies a common unit-step rail. Later fabric trials supply an additional independent axis and a crossing law.

## Records and reproduction

The [design](Protocol.md), [judge and decision rules](ScienceProtocol.md) and [prospective follow-up specifications](FollowupPlans/) are in the repository. The focused projects describe the [proposer](../../OrnamentFollowup/README.md), [seam](../../OrnamentSeamFollowup/README.md) and [mirror](../../OrnamentMirrorFollowup/README.md) studies. Each contains the commands for its own runner.

The [recorded evidence](RecordedEvidence.zip) includes the input manifest, finalist bytes, prediction ledger, blind and reserve observations, drawings and replay examples. Extract it into an **empty** `artifacts/local-ornament` directory to use the links above on another checkout. The [complete local archive](../../../artifacts/local-ornament/CompleteRun.zip) also includes every primary and proposer generation, the seam pools and the frozen .NET executables. The compact evidence includes the data needed by the audit below. The training histories in the complete archive are needed to retrain the history-based proposer.

To check the recorded experiment, from the repository root:

~~~powershell
python experiments/PrimitiveMeasure/OrnamentCampaign/Audit.py artifacts/local-ornament
dotnet test Tests/PrimitiveMeasure.Tests -c Release --filter FullyQualifiedName~OrnamentCampaign
dotnet test Tests/OrnamentFollowup.Tests -c Release
~~~

To replay the primary winners with the frozen runtime, choose a new output name. Compare its SHA-256 with `primary/replay/verification.json`. The replay command reads only the published inputs and packed programs.

~~~powershell
$localRun = 'artifacts/local-ornament'
dotnet "$localRun/runtime-1/PrimitiveMeasure.dll" --ornament-campaign replay "$localRun/primary/replay/inputs.json" "$localRun/replayed.json"
Get-FileHash "$localRun/replayed.json"
~~~

To run an independent primary replication, use a **new** run directory. This creates a new seal for the executable you build. Each search generation has a recoverable history. The same build can resume an interrupted training run.

~~~powershell
$replica = 'artifacts/ornament-replication'
$runtime = 'artifacts/ornament-replication-runtime'
dotnet publish experiments/PrimitiveMeasure -c Release -o $runtime
$runner = "$runtime/PrimitiveMeasure.dll"
foreach ($phase in 'freeze','train','lock','assay','fabric','make-replays') {
    dotnet $runner --ornament-campaign $phase $replica 8
    if ($LASTEXITCODE) { throw "Failed phase $phase" }
}
dotnet $runner --ornament-campaign replay "$replica/replay/inputs.json" "$replica/replay/observed.json"
Get-FileHash "$replica/replay/expected.json","$replica/replay/observed.json"
~~~

The recorded primary report was sealed before the follow-up questions were chosen. `Report.py` verifies the primary finalists and blind file hashes before regenerating that specific report and gallery. `Inspect.py` builds the primary results inspector. `VocabularyAudit.py` reads the frozen programs to compare their wiring and Boolean functions. `Bundle.py` rebuilds both local archives with per-file checksums. Each tool runs locally.
