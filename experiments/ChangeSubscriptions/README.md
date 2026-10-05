# Change subscriptions

A small native Gojo experiment in partial usefulness, stable live reads and
increasing resistance to repeatedly harmful connections. It is separate from
`FailureSubscriptions` and does not change old experiments or the kernel.

One deliberately imperfect predictor offers three structural reads:

```
current delta -> current delta + 2 * clamp(delta,-1,1)
              -> direction read -> independently adapting subscriber
              -> amount read    -> independently adapting subscriber
              -> coarse amount  -> continuously attached observer
```

The prediction concerns the next observation. On a steady sequence of small
increases it can be helpful for direction and harmful for amount. Larger steps
can make the same source useful for amount. Predictions need only improve the
consumer's zero-change reference; occasional errors do not independently trigger
escalation. Both the reference and ordered direction loss are supplied choices.

## Run

From the repository root:

```powershell
dotnet build experiments/ChangeSubscriptions/ChangeSubscriptions.csproj -c Release
dotnet experiments/ChangeSubscriptions/bin/Release/net10.0/ChangeSubscriptions.dll --checks-only
dotnet experiments/ChangeSubscriptions/bin/Release/net10.0/ChangeSubscriptions.dll --campaign --output experiments/ChangeSubscriptions/obj/my-campaign
dotnet experiments/ChangeSubscriptions/bin/Release/net10.0/ChangeSubscriptions.dll --wake-campaign --output experiments/ChangeSubscriptions/obj/my-wake-campaign
```

The output directory must be empty. A campaign runs five fixed traces against
unthrottled, fixed-delay2 and escalating-delay32 policies, as declared in
[PROTOCOL.md](PROTOCOL.md). The runner records a source/build fingerprint, phase
summaries, per-arrival diagnostic JSON, and a final native graph per run. It does
not tune policy from those reports. Existing results are never overwritten.

The optional wake campaign compares fixed-delay2, escalating-delay32 and the
same escalating policy with a bounded early trial on six traces. Its declared
rules are in [WAKE_PROTOCOL.md](WAKE_PROTOCOL.md). During a delay, a separate
native attachment watches a coarse read. A changed cell supported by two
successive observations permits up to two early forecasts. Their later outcomes
enter the existing review; waking never clears a poor record. Cheap revision
polls, notices, initial synchronizations, follow-up inspections and paid early
issues have separate counters. These counts do not establish processor savings.

`final.native.zip` holds the complete retained Gojo world, including active and
detached relations, pending promises, validity and partial evidence blocks.
Focused checks restore and continue it independently. This small runner has no
campaign-resume command or saved fixture cursor; the older long-run experiment
continues to provide that facility.

## What is native and what is supplied

The source equations, selected reads and their residue, validity, publication
revisions, subscriber receipts, actual live attachments, comparison evidence,
block sums, retry ladder, deadlines and cost counters are connected Gojo
structure. Their operational sites and equations are reused. Only a current
frame and pending next-observation promises need to be retained; optional older
witnesses can be released. A captured arrival rejects stale shared envelopes.

The optional wake anchor, candidate, confirmation progress, finite allowance,
delay epoch and actual cheap publication connections also live in that graph.
Transport retains them and can continue with independent mutable identities.
The helper's caller owns the proof that token consumption follows a paid issue;
same-arrival repeats cannot consume twice. There is no general transaction ledger.

The host admits external fixture observations and schedules the local read,
comparison, settlement and notification helpers. Their control branches inspect
native claims. The source recipe, block size4, retry ladder, read perspectives,
reference, and work cost1 are authored. We are testing their interaction, not
claiming they evolved or run under an autonomous asynchronous kernel scheduler.

Reports are disposable projections. They cannot affect the actors. Allocation
and wall time distinguish the scheduled native work from per-arrival report
work; graph samples are retained size at phase boundaries, not peak memory.

## Important distinctions

- **Read versus event:** an attached reader can inspect the current supported
  value without creating a notification or new prediction evidence.
- **Change versus validation:** small raw changes can leave direction or a
  coarse read unchanged while later observations still validate predictions.
- **Validity versus held value:** explicit missing input withdraws current
  support without replacing the inspectable last value with a factual zero.
  Recovery needs two consecutive observations to form a fresh one-step delta.
- **Current state versus every transition:** notifications coalesce unobserved
  revisions. Reconnection synchronizes current state; it does not replay a full
  event archive. Subscribe to additional event surfaces if intermediate changes
  themselves matter.
- **Error versus comparative harm:** only a negative aggregate benefit over four
  resolved uses raises retry resistance. Neutral accuracy and costly redundancy
  are tracked separately; missing evidence does not enter the block.
- **Use versus payment:** each issued subscribed forecast incurs one modeled
  access unit, even if its future outcome is missing. This is a work counter,
  not a full currency market or a measured processor cost.
- **Silence versus explicit unavailability:** the fixture supplies availability
  events. Silence alone cannot prove either continued support or failure.

The retry helper assumes once-only admitted outcomes from its enclosing
scheduler. Arbitrary direct replay of a paid receipt is not deduplicated there.
Notification filtering currently reduces downstream notices; source processing
and observer scheduling still run on every arrival. Price negotiation, shared
exploration budgets, learned attention, automatic discovery and guaranteed
delivery of every transition remain outside this experiment.

See [ASSESSMENT.md](ASSESSMENT.md) for retained campaign results once available.
See [WAKE_ASSESSMENT.md](WAKE_ASSESSMENT.md) for the bounded wake campaign.
