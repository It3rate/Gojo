# Learning which evidence to request

Status: implemented exploratory policy training above the existing qualitative
readers, with a small inspectable local run. No kernel rules change.

The [gallery](../../experiments/QualitativeFrieze/results/evidence-selection/gallery.html)
shows selected episodes, including open replies, conflicts and a misleading
singleton. The [recorded data](../../experiments/QualitativeFrieze/results/evidence-selection/diagnostics.json)
contains all compact episode metrics. The executable implementation is
[EvidenceSelectionStudy.cs](../../experiments/QualitativeFrieze/EvidenceSelectionStudy.cs).

## What executes

These experiments run as a local .NET command. The browser displays saved HTML
and SVG; opening it does not launch C#, a remote model or a background trainer.

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --evidence-selection
```

The next evidence port is selected by a five-coefficient ridge-regression
policy fitted on acquired training outcomes. This is actual parameter fitting,
although much smaller than a neural model. The learner cannot alter native
compatibility or decide that an unsupported claim is true. It ranks available
requests; the existing `ProvisionalEquivalence.Assess` reads the resulting
publications, capabilities and hypotheses.

Each candidate contains an executable `UnitArrangementProgram`, actual role
bindings, repeated unmeasured span participation and proposed equivalences.
The source supplier holds its own publication. The receiver is given the
candidate constructions, advertised role ports and query context, but not the
supplier's selected candidate or unrevealed span words.

```text
receiver = (engine -> context) ->
           (hypotheses -> (advertised ports -> published observations))
port = actual role scope -> request seam
request = receiver-before -> actual port
response = request -> (published observations -> blocked)
features = port -> Closed(feature basis -> actual participating scopes)
training row = features ->
               (before assessment -> (response -> after assessment))
policy = feature basis -> Closed(training rows)
step = before -> (features -> (response -> after))
```

A response must belong to an actual outstanding request at this receiver.
Copied ports, foreign replies and response replay do not import evidence.
Requests return only the roles advertised by that port. Native witnesses retain
the before state, request, response and resulting assessment. These are
non-draining observations, not transfers of conserved material or automatic
carrier-tension updates.

## Learned read and evaluation

The numerical backend uses five features calculated from connected structure:

1. An intercept.
2. The fraction of currently compatible candidate pairs distinguished by the
   proposed port's predicted role words.
3. Overlap between advertised roles and the selected query's actual roles.
4. The number of advertised roles divided by two.
5. The fraction of advertised roles with an already closed publication.

Pairwise candidate distinctions are consequences of the proposed constructions;
they are not counted as independent sensor reports. A pair service publishes two
roles for one advertised request. The experiment compares service calls, not
equal communication bandwidth.

The training target is the observed decrease in compatible candidates, divided
by the maximum possible decrease from that before-state. An outcome with no
compatible candidates receives target -1. Ridge regularization is 0.05. This is
an explicitly chosen finite-library acquisition objective, not a general
confidence or truth law. The model sees training acquisitions; the conflict and
capability controls test the native guardrails, not learned recognition of those
failure classes.

The five coefficients can be recomputed from the native training rows without
the original training object. The archive is restored into fresh graph identities
and must reproduce the same coefficients. No measured numeric carriers are
introduced into the qualitative source/evidence world. Numerical coefficients
and normalized features are derived statistical reads over that world.

After a policy stops, a separate full-source assessment checks its answer. This
assessment is excluded from the receiver and from policy features. One compatible
candidate under acquired evidence is a conditional completion. It counts as
correct identification only if that same candidate also survives the independent
full-source assessment.

## Recorded small run

Seed 731, at most four requests per episode:

| Split | Construction families | Runs |
| --- | --- | ---: |
| Training | Single exception; period-four width arrangement | 48 |
| Held-out | Contiguous block; reflected pair | 32 per policy |
| Controls | Open replies; simultaneous conflict; no executable programs; true program unavailable | 4 |

Family selection, parameter variant and source-candidate choice are enumerated
separately. Every family/variant combination contains all four source choices.
Training uses four or five cells; evaluation uses eight or nine. The held-out
families use the same four-edge-cell vocabulary, shared role contract and
unmeasured participation basis. This tests transfer across arrangements, not
generalization to unknown ornament primitives. Repeated source choices and
parameter variants are not separate learned concepts.

There are 142 acquired training outcome rows and 180 total episode executions:
48 training, 128 evaluation executions over the same 32 held-out cases, and four
controls. Only selected evaluation worlds are retained in full.

| Policy | Correct / 32 | Mean requests |
| --- | ---: | ---: |
| Fixed advertised order | 0 | 4.000 |
| Seeded random | 19 | 3.219 |
| Candidate disagreement | 32 | 1.250 |
| Learned ranking | 32 | 1.250 |

The fixed order deliberately visits vertical services first, where these
candidates agree; it is an easy negative baseline. The stronger disagreement
rule matches the learner. The largest fitted coefficient is approximately
0.979 for candidate disagreement. The useful finding is that this small learner
recovers an effective acquisition preference while native readers retain
authority. There is no demonstrated advantage over the simple disagreement
rule, no statistical confidence interval, and no claim of an optimal policy.

The four retained controls end differently:

- Open words leave four candidates compatible after all four requests.
- Contradictory same-event reports leave zero compatible candidates and visible
  conflict witnesses after one request.
- No executable programs leave four blocked candidates and no requests.
- Omitting only the true program produces a misleading singleton after one
  request. It is retained as a conditional sample, but independent source
  evaluation rejects it as identification.

Selected before/after samples execute through `UnitArrangementNode`. An actual
stroke reader checks the realized span words against proposed parameter words;
a separate actual-incidence read checks closure. There are 18 fresh audited
output executions. The generic motif reader from the earlier small experiment
retains its original search budget and remains incomplete on all 18 larger
arrays. Its unsearched structure is retained. The closure check does not claim
that the generic recognizer successfully discovered these larger arrangements.

## Local cost and infrastructure implications

On the recorded Ryzen 7 5700G / 16-logical-processor machine, this first-use
Release training/evaluation pass took 4.70 seconds wall time and 5.28 seconds
aggregate process CPU. It cumulatively allocated 4.74 GB of managed objects;
working memory immediately after the pass was about 223 MiB. Cumulative
allocation measures garbage throughput, not required RAM.

That timer excludes SDK build, process launch, the 18 output audits, gallery
rendering and native capture/restoration. The retained audit world has 3,124,244
participants and its complete compressed archive is 28,003,509 bytes. This
archive includes all training outcome proofs, selected evaluation episodes and
fresh output audits; it is much larger than the five learned coefficients.
Archive processing can require the original graph, serialized text and restored
graph to coexist. The after-training memory figure is not a whole-report peak.

The [separate local cost probe](GojoLocalExperimentCosts.md) measures the earlier
four-case study and isolates native capture/restoration overhead. Its timings
must not be substituted for larger training episodes.

The next few routing-policy experiments fit ordinary CPU execution. A large
neural decoder or generator would be a different workload; these timings do not
budget its GPU requirements. Before increasing dataset size substantially:

- Stream compact derived training rows and metrics, with their selected read
  basis and provenance. Keep full witnesses for failures and a bounded audit
  sample rather than every execution.
- Replace unbounded retained training histories with a declared compact fitting
  contract or checkpointed sufficient statistics. Preserve selected proofs and
  explicitly qualify the loss of exact replay. The current policy archive is
  deliberately an exact small-run audit contract and does not do this yet.
- Add restartable jobs, deterministic episode IDs, dataset-family splits and
  checkpoint/resume before long runs. Shared immutable definitions can be reused
  without merging fresh source/event identities.
- Measure representative larger graphs, search capacities and model sizes before
  choosing local workers, remote CPU jobs or GPU training.

This run supplies a cost landmark and a real learning loop. It does not yet
provide distributed training, a streaming dataset runner, bounded adaptive
memory or a production model-serving system.

## Verification

```powershell
dotnet test Tests/QualitativeExecution.Tests --filter "FullyQualifiedName~EvidenceSelection"
node experiments/QualitativeFrieze/tools/check-evidence-selection.cjs
dotnet test Gojo.slnx
```

Focused tests exercise hidden-truth isolation, identity-sensitive request
boundaries, replay rejection, native policy reconstruction, open and conflicting
evidence, source-qualified identification and fresh executed output reads.
Browser checks cover all 12 retained episodes, local assets, native archive
presence and desktop/mobile layout.

The recorded full solution run passed all 4,485 tests, including the 19 new
focused checks. Release compilation reported no warnings or errors. Browser
verification covered 12 episodes, 18 outputs and 22 local references, with no
console errors or horizontal overflow at desktop and mobile widths.
