# Connection assembly: first capability probe

This is separate from the timing experiments. No kernel changes and no other
test suites are part of this pass. Freeze these rules before examining results.

The test asks whether supplied native readers can expose success and unmet
requests, recruit real producer-to-consumer contributions, retain infrequent
usefulness, and withdraw unused contributions when the signal changes. It does
not ask the network to discover the supplied readers or forecast exact events.
Success here is local recognition/accounting on a later arrival through a link
that was already available before that arrival. It is not advance timing credit.

## Structure

```text
world = G(shared input frame, bounded recent coordinates,
          primitive structures, finite connection neighborhood, fade policy)
primitive = G(shared input port, native feature/constraint structure,
              raw success port, engagement carrier, request port,
              received help, accounted read, unresolved pressure, latest witness)
connection = G(producer success port, consumer request port,
               bounded support, gate coordinate,
               live product publication, receipt port, latest utility witness)
```

The six supplied readers recognize sustained small rise, sustained small fall,
flat change, a large change, direction reversal, and a change in nonzero
same-direction magnitude. The first two require two adjacent deltas of the
same sign with magnitude 1..3. Flat means current delta zero. Jump means current
magnitude at least 4. Reverse means adjacent nonzero deltas of opposite signs.
Scale change requires adjacent nonzero deltas of the same sign and different
magnitudes. These are declared tick-scale scopes, not universal word meanings.
Native equations calculate differences, products and magnitudes; connected
inequality bounds carry each reader's constraints, with no operation enum.
All readers receive identical input and source identity. Their several successes
are not independent observations.

Warmup, missing and nonconsecutive observations do not earn credit or bridge a
gap. A delivered unknown observation clears the short trace and published pulses;
no report performs no update. Repeated inspection never refreshes support.

There are six readers and 30 possible directed helper slots. Initially every
gate is zero. A successful reader publishes a one-packet recognition signal.
Its engagement gains 8, capped at 48. An engaged reader that does not recognize
the current arrival requests one packet of help. Existing gates act first:
`receipt = producerSuccess * gate * consumerRequest` is an actual native live
publication. A consumer reads the sum of receipts. Unmet requests remain visible
as pressure; a connected helper's receipt can discharge the current request.

An existing link earns 6 support, capped at 48, only when removing its receipt
would change that consumer from accounted to unaccounted. Redundant receipts
earn no duplicate credit. After evaluating the existing links, an unserved
request can probe the first currently successful other reader in its fixed
local neighborhood whose slot has zero support. A new probe receives 3 support
and opens its gate for the NEXT arrival. Discovery is a supplied local traversal
policy, not an emergent search law. No credit is granted for the discovery frame.

Engagement and link support fade by one every fourth complete arrival, clipped
at zero without negative debt. Support greater than zero keeps the gate open.
Occasional useful links can survive many unproductive arrivals; old links
eventually withdraw. Caps, fade and recruitment grants are inspectable supplied
policy, not learned confidence. Every scalar support change uses native
arithmetic and explicit tension admission.

Core currently has attachment but no public detach operation. This experiment
therefore tests opening and closing executable gates in a finite neighborhood.
Zero gate means no functional contribution; it does not claim graph deletion.
Do not repeatedly attach new copies of the same live publication.

## Inputs and observation

Run equal-length 240-arrival blocks of zigzag, square, synthetic heartbeat and
zigzag again. A second run changes order and sign and ends flat. Keep waveform
labels, generation phase and future values outside every learner structure.
Join blocks at their current level; account for any real discontinuity normally.
Zigzag uses small constant steps; square uses plateaus and isolated larger edges.
Heartbeat is a supplied repeating short pulse sequence with a longer quiet gap,
not a physiological model. Add a deterministic shuffled pulse control and a
larger-step zigzag to expose ambiguity and dependence on the supplied scale.
Also start an ordered heartbeat from a fresh world for the same 240 arrivals,
so the shuffled control has a matched initial state. This control was added
during pre-run review, before any trial results were generated.

Capture raw recognition, requested/received/unmet help, actual gates/support,
new probes, useful/duplicate receipts, withdrawal, and a few retained graph
sizes. Observer history is outside the learner. Report functional ablations,
not graph pictures alone. Keep all first results even when they look poor.

Focused checks only: isolated square edges do not become sustained rise/fall;
continuous zigzag has reversals but no large changes; a zero gate blocks a real
producer perturbation; missing frames remain open; unused support fades slowly;
current-frame proposals earn no current-frame help; repeated reads do not
advance/fade/reward; repeated pattern changes do not accumulate live attachments.

A small Avalonia/Skia replay may show the recorded input, native ports and
connections, support, receipts and pressure. Rendering must never derive links
from labels or correlations. Explain that it replays recognition/help, not an
autonomous future-prediction demonstration.

## Lifecycle follow-up

The original protocol and results above remain the baseline. After adding core
withdrawal, the same readers, inputs and participation/fade policies now open a
link by attaching its retained publication and close it by detaching that same
identity. Pulses clear before topology changes, so withdrawal never implicitly
retracts a receipt. Dormant templates remain available for reuse. Results go to
`results-lifecycle`, separate from the original run. The new consumer-specific
interval-response capability has separate focused tests; generic-help policy
is deliberately unchanged here so the lifecycle effect can be compared.

## Dynamic-equation follow-up

Keep the same observations, readers, grants, bounds, recruitment order and fade
policy. Construct and connect operational equations once, retarget their input
sites at admitted boundaries, and keep selected historical numeric observations
separate from the changing structure. Repeated local inspection must not move
sites or refresh support. Rejected duplicate/stale admissions must not mutate
the live network. Missing input must remain incomplete.

Measure retained graph sizes, reused physical identities and admission
allocation separately. A smaller selected witness alone is not evidence that
operational equations are reused. A frozen numeric sample is not a promise that
its live structure references describe a frozen historical world.

Save this run in `results-dynamic`. Compare all recorded behavioral fields with
`results-lifecycle`, excluding only physical IDs and graph-size diagnostics,
using `Compare-Replays.ps1`. Preserve both baselines. This pass changes execution
and evidence cost; it is not a new temporal-composition or learning policy.
