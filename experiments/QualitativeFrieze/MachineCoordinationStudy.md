# Four small machines for coordinating change

## Start here

The previous experiment had a machine that could hear “grow”, “hold”, or
“shrink” and change an extent by one shared step. This experiment gives that
machine four more useful connections: notice work already waiting, wait for
a receipt, choose between competing requests, and observe a geometric gap.

Open the [recorded visual study](results/machine-coordination/index.html).
Its four tabs follow those four steps. Choose a situation, compare the
different rules, and move the time slider to inspect the actual messages,
responses, drawings and remaining tension. The browser displays recorded Gojo
states; it does not calculate a path to the desired picture.

The [new machine glossary](MachineCoordinationGlossary.md) explains each
mechanism from its connected parts. It extends the [earlier glossary](MachineGlossary.md),
which separates the existing construction engines, transformations and shape
reads. The [prospective protocol](MachineCoordinationProtocol.md) records the
predictions made before this campaign.

The main result is that the four mechanisms address different problems. None
is a general “make this shape better” operation. Their combination makes the
successes more understandable, and makes several kinds of failure distinguishable.

## What the machines actually are

| Machine | Connected parts that do the work | What changes |
| --- | --- | --- |
| Pending-adjustment gate | An advertised live tension port, two three-state domains, and nine native response rows | Repeated requests in the direction of existing debt become holds |
| One outstanding request | A capacity-one carrier, held publication, dispatch correspondence, processing receipt, and timeline | New observations wait until the previous receipt returns |
| Response arbiter | Two response slots, an explicit vacancy, and sixteen native selection rows | One response is selected, or the response remains unfilled |
| Geometric gap claim | Two section roles, a selected normal, a shared reach calibration, desired-gap port, and permitted motion correspondence | A measured separation selects a coarse correction |

These are supplied native structures, not names that dispatch to a shape
solver. Constructors select a row set or connect the participants. The readers
then follow that actual structure. The drawing templates remain the three
authored constructions from the [first settlement study](results/machine-settlement/index.html).

```text
public source observation
  -> candidate response slots and selection law, when present
  -> held request, dispatch and receipt, when present
  -> live pending-adjustment read and gate, when present
  -> permitted signed extent change
  -> local capacity and tension settlement
  -> exposed extent -> captured sample -> drawing role
  -> native ordered drawing words
  -> selected geometric claim / independent diagnostic read
```

Every dynamic actuator in this campaign uses the earlier comparison-driven
feedback law. The gate produces an input for that law; it is not a universal
safety interlock for a clock law that deliberately ignores comparison sign.

## Read the bits in their context

The three bits in the support mask address three participants of a selected
native domain. They are not three independent commands, a physical magnitude,
or a replacement for the old ODV participation word.

| Bits | Original extent comparison | Permitted response after a gate or arbiter |
| --- | --- | --- |
| `001` | Current extent is greater than its reference | Decrease one step |
| `010` | The exposed extents agree | Hold: introduce zero adjustment |
| `100` | Current extent is smaller | Increase one step |
| `111` | Multiple comparisons remain possible | Several responses remain possible |
| `000` in this report | Missing or unresolved read | No resolved response |

In particular, a gate can turn `100` into `010` while the goal remains unmet.
That means **hold the response**, not **declare equality**. The original
observation and the gate's law read remain connected in the private witness.
Goal checks use actual source/state or gap comparisons, never this transformed
hold symbol. The shared packet grammar serves this bounded local actuator
chain; a general cross-domain response service should expose its response
contract separately from observational truth.

The existing finite-law compiler derives compact support lookups from the
native rows. There are 64 mask-pair entries for the pending gate, 256 for the
arbiter, and 32 for the gap's comparison/polarity law. These counts describe
derived lookup payloads, not the complete machine, archive or event history.

## Step 1: notice an adjustment already waiting

The actuator can now advertise its live signed tension through an explicit
port. A local gate observes only the sign needed for its decision. Its private
witness records the sample; the outgoing packet exposes no private magnitude.

If a positive request is already held, another positive request becomes a
hold. A negative request remains permitted and first accounts for the positive
debt. The rule is symmetric for negative debt. Missing pending information
leaves the response unresolved; it does not pretend that there is no debt.

The campaign ran 24 conditions: four situations, two response arrangements,
and three starting values. With capacity three and a desired extent of six,
the original repeated-request machine accumulated 25, 27 or 28 pending units.
The gate kept one pending unit in each case. **Neither version reached six.**
The improvement is bounded unfulfilled work, not successful geometry.

When the goal falls to one, the permitted negative response first accounts
for retained positive tension, then shrinks the visible extent. Both versions
eventually reach one in the recorded horizon, but the gate has much less old
work to account for. No tension is deleted or relabeled as success.

This is appropriate for repeated advisory observations. It would not justify
discarding independently arriving conserved material. The controller chooses
whether to introduce a new adjustment; already admitted change remains subject
to the carrier's accounting law.

## Step 2: let one message finish its round trip

The second machine reserves one slot when it accepts a publication. Dispatch
does not free that slot. The receiver processes the observation, its local
receipt service connects that processing step to an opaque receipt, and the
return of that exact receipt releases the reservation.

The outward publication has no path into private supplier samples. The return
receipt has no path into the receiver's private transition history. Private
witnesses preserve those correspondences for inspection.

The campaign schedules three beats to delivery and three beats for the receipt
to return. That timing is supplied apparatus; the channel's occupancy, held
packet, dispatch, receipt and operation history are native structure. The
channel's own beat counts operations, while the viewer uses experiment beats.

There are 45 conditions: five situations, three arrangements, and three starts.
Both acknowledgement arrangements reach the fixed and reversed goals from all
three starts. The older pipeline sometimes happens to match at the last frame
while continuing to respond to obsolete comparisons. A matching final value is
therefore reported separately from waiting, changing and unresolved behavior.

Two failures are particularly useful:

- **Lost receipt:** one processed request leaves the reservation occupied.
  There is no automatic timeout or retransmission. One start happens to need
  only that one step and reaches its goal, but its channel is still stuck.
- **Source changing during travel:** the protocol does not predict a moving
  target. None of the tested arrangements matches the continuously alternating
  source at the end of this run.

At insufficient capacity, acknowledgement alone only slows debt accumulation:
the tested totals are 9, 11 and 12 units instead of the pipeline's 66, 68 and 69.
Combining acknowledgement with the pending gate holds this to one unit.
Receipts continue to return even for held or unresolved responses. They mean
**processed**, not **goal achieved** or **all tension settled**.

## Step 3: make competing requests explicit

Two sources now observe the same receiver. Each can fill a response slot only
when its own comparison supplies a resolved response. Missing, ambiguous,
foreign or empty support remains in its witness and leaves that slot unfilled.
Vacancy is not a fourth physical comparison and is not zero.

The supplied selector laws are:

- **Agreement:** select a response only when both filled slots agree.
- **Prefer first:** use the first filled slot, otherwise the second.
- **Prefer second:** the reverse preference.

The comparison control alternates sources on successive beats. All four use
the pending gate downstream, so their capacity behavior can be compared
without unbounded repeated debt obscuring the selection issue.

There are 60 conditions. When both goals are four, all arrangements reach
four from all three starts. For goals two and six, the priority rules reach
their preferred goal from every start. The losing goal remains unmet and its
offer remains read residue. There is no compromise scalar, summed command,
or claim that both goals became true.

Agreement can advance while both sources request the same direction. It then
stops when their responses disagree. Depending on the starting value, it can
stop at two, three or six. Reaching one of those goals does not remove the
conflict. If the preferred comparison disappears or becomes ambiguous, the
priority law explicitly falls back to the other filled slot. That is a
selection policy, not an estimate of the missing comparison.

Two identical requests also produce one response rather than two adjustments.
This is coordination of observations, not addition of two material transfers.
Whether priority is fair, appropriate or learned is outside this experiment.

## Step 4: ask for a gap rather than a picture

The geometric sensor receives two actual native straight sections. It reads
their ordered control words through a selected cardinal frame, checks for a
positive longitudinal overlap, and measures their constant separation along
the selected normal. It compares that gap with an advertised desired gap.

A second connected relation states how increasing an actuator's extent moves
the selected boundary. This is essential guidance: a difference alone does
not say which legal operation will reduce it. The finite polarity law combines
the comparison with that motion direction and publishes a coarse correction.

```text
actual section A -> selected section role A
actual section B -> selected section role B
selected normal + shared-reach calibration -> gap read
desired-gap port -> comparison with that read
actuator extent -> moved section role -> positive-motion word
comparison + motion polarity -> permitted correction
```

No rendered coordinates or target silhouette decide the action. Exact vector
reads are calculated from the current native words in the selected shared
basis. Captured extent samples remain connected to their actual advertised
ports and their drawing-role bindings.

The 48 conditions cover three constructions, four situations, two response
arrangements and two starts. The lower half's height is the adjustable role.

| Construction | Effect of one actuator step on the selected gap | Recorded behavior |
| --- | --- | --- |
| Cardinal cap | One shared reach | Reaches gaps six and five |
| Diagonal cap | One shared reach between its horizontal sections | Reaches gaps six and five |
| Inset cap | Three shared reaches | Reaches nine; alternates between nine and twelve when asked for ten |

The “resolution probe” uses a representable target of five for the first two
constructions and an unrepresentable target of ten for the inset. These are
positive and negative controls, not a claim that every target in that scenario
is off the construction's spacing.

The inset's failure does not produce fractional interpolation, an invented
smaller unit or false equality. Its extent law keeps proposing opposite steps
around the unavailable gap. Detecting that recurrent unresolved claim and
requesting a finer construction is a future mechanism, not already learned.

The geometry also exposes a cross-level conflict: the diagonal cap can reach
the desired gap while its ending position differs from the other cap's end.
The local claim succeeded; the complete outline did not thereby close. The
viewer reports endpoint position agreement separately. Even agreement of
positions would not establish shared site identity or a material weld.

During withheld control observations, the independent evaluator still records
the actual after-state gap for the plot. That diagnostic information is not
sent to the actuator. The corresponding input support remains missing.

This sensor is deliberately narrow. Nonparallel or unsupported sections,
point-only overlap, a wrong-side frame, absent calibration, or tangential/no
response motion cannot supply this constant-gap correction. It does not
measure every whitespace region, minimum clearance or collision in a drawing.

## What has been verified and what remains supplied

All 177 conditions retain their starting state and each observed frame. Four
representative complete traces agree between native laws and their compact
readers. Component tests additionally cover finite support combinations,
missing and conflicting evidence, packet privacy, event replay, blocked
capacity, response polarity and composed restoration during an active request.

The four exported native graphs are [pending](results/machine-coordination/pending.gojo),
[channel](results/machine-coordination/channel.gojo),
[arbitration](results/machine-coordination/arbitration.gojo), and
[gap](results/machine-coordination/gap.gojo). The actual exported byte streams
are decoded during the run before they are reported as archives.

The gate, channel, selector and actuator can be rebound after a composed
restore and continue execution with their shared aliases intact. There is a
remaining API gap for the geometric reader: its decoded curve/motion frame
does not yet have a public exact rebind constructor. Saving and decoding that
native geometry is verified; restarting the gap reader solely from its decoded
graph is not yet demonstrated.

Graph byte counts include whichever connected history the measured root
retains, especially the channel's events and timeline. Exported exemplars also
include native drawings, samples and private witnesses. These are inspection
and transport costs, not minimal operational bits or a performance benchmark.
The complete report is a bounded apparatus, not a single learned intelligence.

We supplied goals, capacities, shared units, shape phases, observation surfaces,
motion correspondence, response rows, priorities and delivery schedule. We did
not supply intermediate target pictures or let an aesthetic evaluator choose
individual actions. These remain small integral carrier experiments with
coarse public decisions; the original pre-numeric participation bit has not
been redefined.

## What this suggests next

The clearest next geometric test is to let a gap claim and an endpoint claim
coexist, each connected to a permitted extent role. In the diagonal example,
changing height satisfies the gap but alters horizontal reach. A connected
width response could then attempt to restore the seam. That would test two
levels of meaning acting together, with explicit conflict if capacity or
resolution prevents satisfying both.

Other concrete gaps are receipt recovery after loss, a reader for restarting
the saved geometric frame, and a local detector for repeated unresolved
oscillation. Those additions should be inspectable machines with their own
failure conditions, rather than an outside optimizer quietly correcting the
picture. No discovery, automatic topology change, general interpolation or
learning of priorities is claimed here.

## Reproduce

```powershell
dotnet run -c Release --project experiments/QualitativeFrieze -- --machine-coordination
python experiments/QualitativeFrieze/RenderMachineCoordinationNotes.py
dotnet test Tests/QualitativeExecution.Tests/QualitativeExecution.Tests.csproj --filter "FullyQualifiedName~PendingAdjustmentGate|FullyQualifiedName~AcknowledgedComparisonChannel|FullyQualifiedName~ExtentResponseArbiter|FullyQualifiedName~GeometricGapClaim|FullyQualifiedName~MachineCoordinationStudy"
dotnet test Gojo.slnx
```

Implementation: [campaign](Composition/MachineCoordinationStudy.cs),
[pending gate](Composition/PendingAdjustmentGate.cs),
[request channel](Composition/AcknowledgedComparisonChannel.cs),
[response arbiter](Composition/ExtentResponseArbiter.cs), and
[gap claim](Composition/GeometricGapClaim.cs).

The [recorded JSON](results/machine-coordination/study-data.json) supplies the
interactive report. Markdown and HTML describe the same results and limits.
