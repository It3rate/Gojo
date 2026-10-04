# Reusable equations and the next learning question

The reconstruction rewrite was outstanding after the lifecycle follow-up. This
pass implements it in the connection experiment using existing Gojo mechanisms.
It does not change the six readers, test inputs, grants, scan order, fade rule,
or meaning of help. The earlier reports remain historical baselines.

## What now stays alive

Feature, accounting, helper-ablation, support/cap/fade, phase and target-delta
equations are constructed once. Their carrier/relation structure remains
connected. Accepted input retargets existing sites; pure reads calculate current
results without installing stored answers. Node and constraint relations now
retain their identities too, rather than merely retaining the output carriers.
The small outer admission envelope still records a version to reject stale
callers and keeps a bounded two-coordinate trace.

Known input participates through an ordinary affine unit span. Missing input
collapses that span, leaving the native feature read unresolved even when read
directly, outside the publication scheduler. It is not a host flag that hides a
stale numeric answer. Zero observed input still has a valid unit span and remains
distinct from missing input.

Current numeric results and residues are captured without recursively rebuilding
the entire instruction graph and every upstream result. Selected historical
numbers stay fixed; their references to operational structure remain live.
Those samples do not promise a frozen historical world. Full archival capture
is a separate explicit operation using the existing numeric graph format.

Rejected stale/duplicate admissions do not retarget sites. A failed read before
settlement restores its tentative site changes, including ordinary unresolved
reads and arithmetic overflow. Once physical settlement has begun, a failure
retains the inspectable physical state; the experiment does not provide a
transactional rollback of settlement.

The operational lookup used during an admission is a disposable index obtained
from connected native target rows. It owns neither the equations nor their
meaning and is rebuilt from those rows. No persistent host model or result cache
was introduced.

## Evidence and remaining cost

The [fixed trials](results-dynamic/first-run.md) and
[identity audit](../ConnectionAssembly.Audit/current-dynamic-counts.txt) distinguish
retention, reconstruction and actual participation. Detailed counts are in the
audit; each frame's selected witness is still newly captured, and its carrier
and relation scaffolding is still substantial. The change establishes reuse,
not a minimal representation or an allocation-free read.

| Measure | Lifecycle version | Reusable-equation version |
| --- | ---: | ---: |
| Retained carriers at arrival 240 | 3,990 | 1,600 |
| Retained relations at arrival 240 | 40,378 | 24,116 |
| New relation identities from arrival 240 to 241 | 38,733 | 9,423 |
| Cumulative allocation over the main 960 arrivals | 11.98 GB | 6.75 GB |

All original operational relations survive. The latest selected witness adds
662 carriers and 9,417 relations to the reusable world. Building instructions
up front increases the base graph, while reducing repeated construction and
total retained size. Main-run cumulative allocation fell about 44 percent;
these are single-run diagnostics, not peak memory or a controlled benchmark.

All five replays are compared with the lifecycle run by
[Compare-Replays.ps1](Compare-Replays.ps1). It excludes only physical identity
labels and graph diagnostics; all other recorded fields must match. The saved
[comparison](results-dynamic/behavior-comparison.json) reports the result.
The same rare-event fade failure and permissive help score remain visible.

Validation passed: 1,752 solution tests, 16 experiment check groups, all 2,640
arrivals across five fixed trials, and exact agreement of every compared replay
field. The native viewer built successfully and its final frame export was
visually checked.

The focused checks exercise changing signed inputs, retargeting an existing
recognition bound, stable node/constraint/instruction identity, direct missing
input reads, pure inspection, frozen selected samples, and real connection
withdrawal/reattachment. This is evidence for mutable inputs, mutable rule scope
and mutable participation. It is not evidence for unrestricted automatic
rewriting or discovery of new concepts.

No shared kernel change was needed for this rewrite. The preceding merged work
added `Carrier.Detach`, local attachment inspection and qualified consumer
interval-response reads in the shared Gojo projects. This pass puts existing
mutable sites and equations to work in `experiments/ConnectionAssembly`.

## Demo provenance

The same native `ConnectionAssembly.Viewer` opens
`results-dynamic/changing-patterns.json` by default. Its header now shows the
loaded file, content fingerprint and that it is a recorded replay. Measured
frames also show the actual attached-relation count. Restart after generating a
new recording; an already running window retains the file it loaded.

The previous default was `results-lifecycle`. Those updates intentionally kept
the observed behavior unchanged, so similar animation was expected. An explicit
old `--replay` argument still opens that older recording. The historical Core4
website has not been updated or deployed by this experiment.

## Direction for composites

The current network has composites in the narrow arithmetic sense: connected
products, sums and helper contributions. It has not learned a reusable temporal
concept such as a zigzag. Its generic help criterion can count a fall as help
for a failed rise expectation without improving any predicted rising quantity.
Ordered and shuffled heartbeat controls both serving about 85 percent of their
requests expose this limitation. Adding many composite candidates now could
reward this permissive criterion rather than develop better interpretation.

A useful next test should give one consumer a concrete next-arrival obligation
expressed as a directional/scale interval. Contributions must be issued before
the observation, and their value measured by native signed error reduction with
and without the same contribution. The new shared interval-response read can
measure that reduction, but a before/after comparison alone does not establish
causality; the paired transition and ablation must be explicit.

Compare a small ordered turn-then-run candidate against last-direction
continuation and a matched shuffled-event helper. Reversal and sustained rise
cannot be conjoined on the same arrival under these primitives: one requires
opposite adjacent signs and the other requires two positive signs. The temporal
order, onset and ongoing/end span need connected structure.

Repeat the same eligible events with longer quiet gaps. Distinguish no unmet
demand, no eligible opportunity, an unsuccessful eligible prediction and an
unresolved read. A rarely useful connection should survive silence, while one
that repeatedly fails on relevant opportunities should lose participation.

Other viable candidates are a continuous slope/curvature helper for smooth
signals, or a local mixture of existing predictors before constructing a new
composite. None should get credit merely for existing or agreeing with its own
constituents. Start with one consumer and bounded candidate cost; require added
subsequent usefulness before expanding the population or adding attention,
genetic search and learned dormancy policies.
