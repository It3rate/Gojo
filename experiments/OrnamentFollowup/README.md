# Does training make a better proposer?

This optional second-stage study tests a training engine, independently of the primary campaign. The parent report and a new prediction have to exist before the follow-up can be registered. The follow-up runs in a separate process and references the original primitive machine.

For each selected goal, the trainer reads the primary evolution ledgers only. For each single-parent mutation it asks whether the child's total utility strictly exceeded its recorded parent's utility on the same training bank. The denominator includes every such evaluated mutation, including repeated genomes, no-ops and failures. Random immigrants and crossovers have different parent contracts and stay outside these rates. The full generation chain must reproduce the final history hash of its locked primary winner.

Each of the nine mutation families gets `1 + floor(15 × improvements / attempts)` tickets. A family with zero attempts gets one ticket. The resulting packed four-bit ticket references choose *which structural edit to attempt during later training*. They are independent of the evaluator and the generator at deployment. Rates can reflect the earlier distribution of parents, modules and opportunities. They establish neither a causal effect for individual edits nor the magnitude of a successful edit's gain.

Three controls compete. Uniform assigns one ticket to each family. Learned assigns its derived tickets. Permuted assigns the same distribution shifted cyclically by one family. Every candidate starts with the same fresh program as its peers at each first-generation position. Subsequently every eighth slot is an immigrant, and the remaining slots select a parent uniformly from up to four distinct training elites. Each arm generates and evaluates exactly one candidate per slot, under the same count, split, replicate seed, bank and parent rule. The selected mutation family is one of the primary grammar's single-parent moves. Crossover and the initial campaign's demes are outside this comparison.

The training bank interleaves goal/family strata under a fresh seed and favors distinct groups before extra sibling variants. Validation selects among the preregistered training shortlist using the previous scorer. The transport artifact is a raw `winner.genome` in each island. Its score, training and validation cases, proposer and search history are unnecessary at deployment.

## Operations

Use a separate Release output directory. The referenced `PrimitiveMeasure.dll` must match the primary protocol's `RuntimeHash`. If the primary executed a separately frozen build, place that exact frozen DLL in the follow-up output. The CLI checks its SHA-256.

```powershell
dotnet build experiments/OrnamentFollowup -c Release
dotnet experiments/OrnamentFollowup/bin/Release/net10.0/OrnamentFollowup.dll verify-source SPEC.json PRIMARY_RUN
dotnet experiments/OrnamentFollowup/bin/Release/net10.0/OrnamentFollowup.dll freeze SPEC.json PRIMARY_RUN PRIMARY_REPORT.md FOLLOWUP_RUN
dotnet experiments/OrnamentFollowup/bin/Release/net10.0/OrnamentFollowup.dll train FOLLOWUP_RUN 8
dotnet experiments/OrnamentFollowup/bin/Release/net10.0/OrnamentFollowup.dll lock FOLLOWUP_RUN
dotnet experiments/OrnamentFollowup/bin/Release/net10.0/OrnamentFollowup.dll assay FOLLOWUP_RUN
```

The schema below is illustrative. The investigator must fill in the prediction, selected goals and hashes after locking the first-wave report. The template supplies no follow-up result. Fresh searches reuse the primary training and validation corpus with a newly sampled training bank and fresh candidate streams. Transfer to new recipe groups is assessed on the reserve.

```json
{
  "Id": "choose-after-primary",
  "Prediction": "Write the proposed direction, consequence and boundary here before training.",
  "ExpectedRelation": "higher",
  "Goals": ["balance"],
  "PrimaryProtocolHash": "ProtocolHash field of primary protocol.lock.json",
  "PrimaryFrozenSha256": "SHA-256 of the whole primary frozen.json file",
  "PrimaryReportSha256": "SHA-256 of the exact first-wave report file",
  "Seed": 8675,
  "Replicates": 4,
  "Generations": 32,
  "Population": 32,
  "TrainingEpisodes": 24,
  "Shortlist": 4,
  "Margin": 10
}
```

Hashes are 64 uppercase hexadecimal characters. Select one or two primary goal names. Use a fresh seed and a positive utility margin. The example budget is per goal and arm. Allowed decision relations are `higher`, `lower` and `equivalent` and use the existing group-level classification rule. The same relation applies to both controls. No data-dependent change to goals, budget, margin, seed, controls or direction is allowed after freeze.

Freeze checks the primary protocol, test completion, archived files, frozen finalists and first report. A prior primary reserve assay prevents registration. File operations are local. This guard cannot detect a human or other program independently viewing reserve cases outside the managed workflow. Train and lock request only training and validation splits. Assay first verifies the new finalist lock, then reads the reserve. Its groups bind both arms on the same episode and replicate. Transformed siblings and competing trials within one family are dependent observations.

The optional source audit recomputes the primary genesis from its protocol, budget, goal, method, replicate, seed, training bank and validation bank. It verifies every generation link and content checksum against the final history hash of the locked primary winner. It reports integrity only. The first real follow-up attempt stopped at this check before candidate training because the earlier importer expected an empty genesis. The corrected importer uses the primary's actual bound-genesis schema.

## Records

| File | What it contains |
| --- | --- |
| `spec.json`, `primary-report.md`, `followup-seal.json` | The frozen plan, parent report, runtime fingerprints and source path. |
| `mutation-evidence.csv`, `proposer.json` | Useful and total exposure counts for every considered family, weights and actual packed ticket rows. |
| `islands/<goal>/<arm>/<rep>/gNNNN.json.gz` | Every paid proposal, including failures, with genome, parent, edit, training merit and a chained checksum. |
| `islands/.../winner.json`, `winner.genome` | Training shortlist, validation ranking, history hash and first-ranked executable bytes. |
| `new-proposal-yield.csv`, `frozen.json` | Paid mutation yield and immutable manifest of all validation-selected finalists. |
| `reserve/cases.csv`, `groups.csv`, `contrasts.csv` | Realized reserve outcomes, per-group paired differences and decisions. A completion record protects the finished files. |
| `reserve/replay-inputs.json`, `replay-expected.json` | One cold-replay example per selected follow-up winner, emitted after lock. |

The cold replay command accepts the primary `CampaignCli.ReplayInput[]` JSON format (program hex, rail input and optional wire condition):

```powershell
dotnet experiments/OrnamentFollowup/bin/Release/net10.0/OrnamentFollowup.dll replay INPUT.json OUTPUT.json
dotnet test Tests/OrnamentFollowup.Tests -c Release
```

The focused tests create artificial primary archives and independent synthetic inputs in temporary directories. They exercise both phases and a deliberately changed ledger without accessing the scientific campaign or its reserved outcomes.

## Completed proposal-transfer-v2

Did favoring mutation families that had helped earlier parents improve a fresh search? On the reserve, all four comparisons were inconclusive. The registered rule required a gain of at least 15 utility points in 32 of 48 independent parent groups. The table shows how often the learned proposal met that margin and its mean gain across all groups.

| Goal | Comparison | Groups gaining at least 15 | Mean gain |
| --- | --- | ---: | ---: |
| Infill | Learned minus uniform | 25 / 48 | −2.04 |
| Infill | Learned minus permuted | 28 / 48 | +23.46 |
| Balance | Learned minus uniform | 2 / 48 | −2.96 |
| Balance | Learned minus permuted | 1 / 48 | −12.97 |

An independent check reproduced these verdicts from 2,304 reserve case rows. In a fresh process with only the primary runtime and replay inputs, all 24 finalist programs reproduced every recorded result field for their replay example. The local certificate is `artifacts/local-ornament/verification/proposer.json`.
