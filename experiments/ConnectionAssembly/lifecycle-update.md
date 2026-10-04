# Connection lifecycle, local usefulness, and the reconstruction audit

This follow-up adds reusable Gojo capabilities and reruns the same fixed signal
probe. It does not revisit poetry or the earlier timing experiments, and does
not implement a general learning or evolutionary policy.

## What changed in Gojo

`Carrier.Detach(relation)` withdraws actual attachment incidences by physical
relation identity. `Carrier.IsAttached(relation)` and `AttachedRelations` expose
local membership through a read-only live view. No connection-status registry
is introduced. The retained relation remains ordinary Gojo structure and can
later be reattached against current input using the existing `Attach` method.

Withdrawal is a topology change, not reverse material transfer. It preserves
carrier value, tension, shared sites and unrelated relations, and does not
settle or zero receivers. Discovery is local to the caller and relation sites'
attached worlds; it does not invent unreachable reverse owners. A last
published value retained after withdrawal is not a newly evaluated answer.
The experiment clears its current pulses before withdrawal, so its receipts
are already zero when a publication becomes dormant.

The shared structural library also adds
`OperationClaim.IntervalResponse(root, allowedInterval, before, after, residual,
transition)` and qualified readers. The native read connects one explicit
permitted interval, captured input sites, before/after interval claims, signed
reduction, residue and selected transition evidence. It does not use a named
concept class or a confidence field.

For example, an ordinary native sum takes input 3 plus producer contribution 4
to 7. Against a consumer interval `[6,8]`, unmet demand changes `3 -> 0`, a
reduction of 3. Against `[2,4]`, demand changes `0 -> 3`, a reduction of -3.
The same producer therefore helps one obligation and harms the other.

The read distinguishes:

- resolved zero initial demand: no currently unmet obligation;
- positive demand and zero reduction: the unmet obligation did not improve;
- positive or negative reduction: improvement or harm at the selected scale;
- open, misaligned, reversed, stale or residual-bearing input: no scalar verdict.

The allowed interval can be retargeted through its existing sites. A new read
uses current coordinates; previously captured samples do not drift. The
qualified interval reader rejects old cached claim results when their live
input or bound sites have moved. Transition evidence is retained, not executed
by the comparison; a before/after difference alone does not prove causality.
No observation automatically introduces new physical tension or support credit.

These changes live in `Gojo/Core` and `Gojo/Structures`, with dedicated tests in
`Gojo.Tests`. The response read supplies evidence for a future participation
rule; it deliberately supplies no universal fade, sampling or confidence law.

## What the large graph actually contained

The user's reconstruction concern was correct. The baseline decomposes as:

| Reachable selection | Carriers | Relations |
| --- | ---: | ---: |
| Current world without its latest witness | 217 | 2,294 |
| Added by the latest witness | 3,773 | 38,084 |
| Full measured world | 3,990 | 40,378 |

The witness accounts for 94.3 percent of retained relations. Of all carriers,
3,291 are ordered-group anchors. There were 42 distinct executable attached
relations: 30 helper products, six help sums and six pressure boundaries.
Attached relations are a subset of the graph, not an additional 42 objects
outside the relation count.

Advancing one observation replaced 3,827 carrier identities and 38,733 relation
identities while keeping the live port and connection identities. The network
was not accumulating that many observers: it rebuilt feature equations,
wrapper groups, claim/feedback records and copied read structures each time.
Only the latest witness is retained in the world, so retention is bounded but
construction churn is substantial. Most of that work is an experiment choice,
not a requirement that Gojo's operational structures be immutable.

The [reproducible audit](../ConnectionAssembly.Audit/README.md) gives section
breakdowns and before/after identity measurements. This follow-up does not
pretend to have fixed that reconstruction path. Existing mutable carriers,
tick sites and structural ports can support persistent equations. The next
implementation pass should reuse that operational structure and retain only
the historical evidence required by a selected read contract. Mutating an
already promised historical witness would not be the right shortcut.

The canonical prompt and its design notes now explicitly distinguish reusable
dynamic state from optional immutable historical captures.

## Same experiment with real withdrawal

The fixed six reader scopes, input sequences, first-neighbor discovery,
generic-help obligation, support grants and fade interval remain unchanged.
This isolates the lifecycle capability from a change of learning policy.
Supported publications attach, faded publications detach, and a later probe
reattaches the same retained identity. A new focused check verifies this full
cycle and its later actual receipt.

All recorded behavior in five runs is identical to the original gated run
after excluding physical identity labels and graph-size diagnostics. See
[the comparison hashes](results-lifecycle/behavior-comparison.json). The old
rare-event fade failure and overgenerous generic-help score therefore remain
visible; the new interval-response read has not been substituted into that
policy and is tested separately.

| Main run boundary | Open helper publications | All attached relations |
| --- | ---: | ---: |
| Zigzag, step 240 | 6 | 18 |
| Square, step 480 | 4 | 16 |
| Heartbeat, step 720 | 8 | 20 |
| Zigzag returns, step 960 | 15 | 27 |

There are 12 fixed attachments and the supported helpers, rather than 42
attachments at every boundary. Retained candidate templates and latest witness
still total 3,990 carriers and 40,378 relations. Main-run cumulative admission
allocation fell from about 14.05 GB to 11.98 GB across 960 arrivals. This is an
allocation measurement, not live memory use or a controlled speed benchmark.
Full results are in [the new report](results-lifecycle/first-run.md).

## Infrequent usefulness and future composites

Combining direction and sustained duration is a promising way to make the
question more specific. In these particular primitives, however, reversal
requires adjacent deltas of opposite signs while sustained rise requires two
positive deltas. Their same-arrival conjunction never occurs. The useful
structure is ordered: reversal event, then a positive run, with an onset and
current/end site connected through a time or observation span.

That span can expose how long the run persists and at what resolution. It
belongs to the calculus of the composite, rather than an opaque duration tag.
A longer meaningful interval can support less frequent observations; short
vibrations present more frequent opportunities. Neither interval nor sample
count alone establishes usefulness or independent evidence. Reinforcement
should depend on the unmet obligation and the opportunities to improve it,
with elapsed quiet time kept distinct from failed eligible predictions.

Explicit withdrawal now permits a retained relation to be dormant and resume
on current input. It does not yet discover a square-wave or zigzag composite,
select when that composite should wake, or learn how resistant it should be to
change. Those are future local growth/lifetime policies. The next small
learning experiment can use consumer-specific obligations and ordered
turn-then-run evidence before adding a larger collection of dormant concepts.

## Validation

- Full `dotnet test Gojo.slnx`: 1,752 passed, zero failures or skips.
- Connection experiment: nine focused check groups passed.
- Five fixed trials: 2,640 arrivals; recorded behavior matches the baseline.
- Allocation audit: reproducible count and identity analysis; no learner input
  comes from its diagnostics.

The old helper name containing "poetry" was only an agent label. It reviewed
connection code and checks; neither the branch nor these experiments were
performing poetry analysis. Current helper names describe the lifecycle,
allocation audit and local-utility work.
