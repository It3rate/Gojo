# Four machines for coordinating change

## Start here

These four machines help a small drawing system respond without losing track
of what it knows, what it already requested, or what is still unresolved.
They do different jobs:

| Machine | Simple purpose | The question it answers |
| --- | --- | --- |
| Pending-adjustment gate | Stop repeating a request that is already held. | “Am I asking for more of the same blocked change?” |
| One-outstanding-request channel | Wait for a particular request to be processed before sending another. | “Has that observation been dealt with yet?” |
| Response arbiter | Select one response from two offered responses. | “Do they agree, or have we explicitly given one priority?” |
| Geometric gap claim | Inspect the space between two selected sections and publish a correction. | “Is this particular gap smaller than, equal to, or larger than the desired gap?” |

They extend the fifteen mechanisms in the earlier
[machine glossary](MachineGlossary.md). That glossary describes construction,
transformation, repetition and drawing behavior. This one describes how small
machines coordinate their responses. A controller, a communication passage,
a sensor and a shape construction remain different structures, even when
they cooperate to produce one picture.

The [coordination study](MachineCoordinationStudy.md), its
[prospective protocol](MachineCoordinationProtocol.md), and the
[recorded visual report](results/machine-coordination/index.html) show how
these parts are exercised together. The mechanisms and their failure cases
below describe implemented scope; the campaign is where their utility is
measured.

## The shared response language

The extent actuator already accepts a small publication in its own comparison
basis. Its finite law turns a settled direction into one signed unit request.
The new machines reuse that surface.

| Reported support | Meaning at the actuator's response input | What it is not |
| --- | --- | --- |
| `001` | Decrease is the admitted response. | A copy of the supplier's exact magnitude. |
| `010` | Hold: zero requested change. | Missing input, or proof that every goal is satisfied. |
| `100` | Increase is the admitted response. | Permission to bypass the local capacity. |
| `111` | Decrease, hold and increase remain alternatives. | Three simultaneous physical requests. |
| Open support, reported as `000` | The publication does not settle a response. | A measured zero. |

These masks are compact diagnostic reads of native participant scopes. They
do not replace the original ODV word or add a universal meaning to its bits.
The native scope still distinguishes open support from a closed empty set.

In particular, a gate can publish **hold** because its law prevents another
request, even while the original comparison says “increase.” The retained
witness explains that difference. A downstream reader must not treat the
gate's hold as fresh evidence that the geometry or the goal is equal.

## 1. Pending-adjustment gate

**Purpose:** do not keep adding the same request when the actuator is already
holding that change as tension.

Suppose an extent has reached its local upper limit. A further positive
request becomes positive tension. Without another rule, every new “increase”
publication can add another positive request. The gate reads that existing
pending adjustment and replaces further requests in the same direction with
a deliberate hold. An opposed request remains available to account for the
held tension.

```text
comparison publication -----------+
                                 |
advertised pending-tension port -> finite response law -> filtered publication
                                 |
                          private read witness
```

The native parts are an explicitly advertised live tension port, separate
comparison and pending-sign domains, a nine-row finite law, and the receiver's
publication contract. The magnitude of the pending tension is retained in a
private captured sample. The law uses only its sign.

For resolved inputs, the supplied law is:

| Requested response | Negative pending tension | No pending tension | Positive pending tension |
| --- | --- | --- | --- |
| Decrease | Hold | Decrease | Decrease |
| Hold | Hold | Hold | Hold |
| Increase | Increase | Increase | Hold |

The gate evaluates alternatives through the same law. It can settle a hold
if all admitted alternatives lead to hold; it does not silently choose a
direction from conflicting alternatives. An unavailable pending read remains
open rather than being treated as zero debt.

The outgoing publication retains the original observation event. A suppressed
request therefore cannot later be replayed as a second request merely because
the tension changed. Only an opaque gate identity is sent as evidence; the
private pending magnitude does not cross that boundary.

**What it does say:** this particular new request is redundant in the direction
of already held change, under the supplied law.

**What it does not say:** the goal is achieved, the tension has disappeared, the
capacity should change, or the previous request was mistaken. The gate does
not erase debt. An opposed request may first reduce debt without producing
visible movement.

Implementation: [pending-adjustment gate](Composition/PendingAdjustmentGate.cs),
[extent actuator](Composition/CoupledExtentMachine.cs), and
[focused tests](../../Tests/QualitativeExecution.Tests/PendingAdjustmentGateTests.cs).

## 2. One-outstanding-request channel

**Purpose:** allow one observation to be in flight, and require its own
processing receipt before admitting another.

Sending and completing are different events. This passage keeps its slot
occupied after dispatch. The receiver can process the observation, produce a
receipt, and send that receipt back. Only the exact issued receipt releases
the slot.

```text
available slot -> admit observation -> dispatch -> receiver processes it
      ^                                               |
      |                                               v
      +------------- exact receipt returns <----- issue receipt
```

The native structure retains the receiver contract, a capacity-one occupancy
carrier and clamp, the actual request event and packet, a dispatch relation,
the issued receipt and its private processing proof, accepted event history,
and a transition timeline. Waiting is readable from this structure; it is not
only a flag in a transport object.

| Situation | Local behavior |
| --- | --- |
| Slot available; new compatible occurrence offered | Retain the request and occupy the slot. |
| Slot occupied | Keep the current request; retain the refusal as evidence. |
| Request retained but not yet dispatched | Permit its one dispatch. |
| Receiver consumed that occurrence | Its receipt service may issue an opaque processing receipt. |
| Exact issued receipt returns | Release the slot. |
| Wrong, copied-looking or old receipt returns | Keep waiting. |
| Receipt does not return | Keep waiting; no automatic timeout is supplied. |

The convenience request operation samples the connected comparison only when
the slot is free. Offering an already sampled packet is also possible, but
does not make that packet fresh. Delay can still make an observation stale
before it reaches the receiver.

A processing receipt says **“this occurrence was consumed.”** It does not say
that the actuator moved, that its tension settled, or that the goal was met.
A missing or ambiguous publication can be processed without producing an
adjustment. A capacity-blocked request can be processed while leaving tension.

The receiver sees the comparison packet. The sender sees the opaque receipt.
The channel's private witness connects the receipt to the actual retained
receiver transition. Neither outward message needs to disclose that private
transition.

**What it does say:** a later request cannot overtake an unacknowledged request
through this capacity-one passage.

**What it does not say:** there is reliable delivery, automatic recovery,
fresh information at arrival, or a globally synchronized system. A lost
receipt is a communication wait; it is not itself blocked adjustment tension.

Implementation: [acknowledged channel](Composition/AcknowledgedComparisonChannel.cs)
and [focused tests](../../Tests/QualitativeExecution.Tests/AcknowledgedComparisonChannelTests.cs).

## 3. Response arbiter

**Purpose:** turn two offered responses into at most one response for the same
actuator, while preserving disagreement.

Two sources may both want a pen to change. Agreeing requests do not need to
become two physical changes. Opposed requests do not automatically establish
a sensible compromise. The arbiter gives those cases an explicit local law.

```text
first publication  -> singleton eligibility --+
                                             +-> finite selector -> one publication
second publication -> singleton eligibility --+          |
                                                        v
                                             retained unselected offers
```

Each input has three possible settled responses and an unfilled candidate
slot. Missing, ambiguous, empty or incompatible input leaves that slot
unfilled, with its original packet retained as the explanation. The unfilled
slot is not a fourth direction and is not zero.

Three supplied law sets are currently available:

| Offers | Agreement required | Prefer first | Prefer second |
| --- | --- | --- | --- |
| Same settled response | One copy of that response | That response | That response |
| Different settled responses | Unresolved | First response | Second response |
| Only first is settled | Unresolved | First response | First response |
| Only second is settled | Unresolved | Second response | Second response |
| Neither is settled | Unresolved | Unresolved | Unresolved |

The policy names select actual finite-law rows at construction. Execution
reads those rows. Priority is therefore a supplied structural choice, not a
secret preference in the reporting code.

The private witness retains both candidate reads, the selected law result,
and offers not accounted for by the selected response. This is **read residue**.
It does not automatically add signed tension to the actuator. A separate
response route would be needed to turn that residue into further action.

Selection creates a new outgoing event. That event is a decision over the
offered observations, not evidence that the observations were freshly sampled.
Retries should reuse its packet; repeatedly making new selections is distinct
from retrying delivery of one selection.

**What it does say:** the selected response follows the admitted agreement or
priority law, and remaining offers are still inspectable.

**What it does not say:** the preferred source is more truthful, the lower
priority goal has been satisfied, or the choice is fair over time. No learned
priority, weighted compromise or fairness controller is supplied here.

Implementation: [response arbiter](Composition/ExtentResponseArbiter.cs) and
[focused tests](../../Tests/QualitativeExecution.Tests/ExtentResponseArbiterTests.cs).

## 4. Geometric gap claim

**Purpose:** let a selected piece of whitespace ask for a correction through a
known response route.

The claim selects two actual straight sections in one shared cardinal frame.
It checks that they have positive longitudinal overlap, reads their separation
along a selected normal, compares that gap with the desired gap, and publishes
a coarse response. Its input is the sections' native control words, not the
renderer's point arrays or a target silhouette.

```text
first section ----+
                  +-> selected normal / shared overlap -> gap read
second section ---+                                          |
                                                            v
desired-gap port -> explicit tick-to-reach calibration -> comparison
                                                            |
actuator port -> moved section role -> positive motion ------+
                                                            |
                                                     response publication
```

The native claim keeps two open section roles, the chosen normal, the desired
gap port, explicit calibrations, the receiver contract, and a response binding.
Each observation binds concrete sections to the roles. The response binding
states which selected section moves when the actuator increases, and gives
that positive-motion word. A small finite law then maps the comparison and
motion polarity to the actuator's response.

| Gap comparison | Increasing the actuator opens the gap | Increasing the actuator closes the gap |
| --- | --- | --- |
| Gap too small | Increase | Decrease |
| Gap equal to desired gap | Hold | Hold |
| Gap too large | Decrease | Increase |

The motion correspondence is supplied guidance. The sensor does not discover
which internal parameter moves which boundary. This matters when the same
response mechanism is attached to different constructions.

For a cardinal cap, one height step moves its selected horizontal section by
one reach. For the supplied inset cap, the top horizontal section uses three
height contributions, so one height step moves it by three reaches. The same
gap comparison can therefore have different available corrections. A desired
gap can lie between those corrections; the sensor must not invent fractional
height to claim success.

A zero gap between two nonzero parallel sections is measurable. It does not
mean that the sections vanished or became one shared site. A zero-length
section, by contrast, does not establish the positive-length corridor required
by this reader.

The current scope excludes curved or oblique sections, missing positive
overlap, a reversed normal-side relation, an unavailable calibration, and
response motion that is zero or is not parallel to the selected normal. Those cases
retain an open read. This is not a general minimum-clearance calculation.

The private witness retains exact geometry reads, the calibrated measurement,
the desired sample and the response-law read. The outgoing packet contains
only the response support, its receiver basis, an event and an opaque claim
identity. Observation alone does not introduce tension; the actuator's
admitted response does.

**What it does say:** this selected constant gap supports a particular
correction under the declared geometry, calibration and motion relation.

**What it does not say:** the whole ornament has good spacing, the sections
bound a complete empty region, or no other section will be hit by the response.
It is a geometric read with a correction route, not a “hexagon engine.”

Implementation: [gap claim](Composition/GeometricGapClaim.cs),
[authored drawing templates](Composition/CoupledShapeTemplate.cs), and
[focused tests](../../Tests/QualitativeExecution.Tests/GeometricGapClaimTests.cs).

## How the four machines can cooperate

A useful composition keeps each responsibility visible:

```text
published comparisons or a selected gap claim
    -> optional response arbitration
    -> one-outstanding-request passage
    -> receiver's pending-adjustment gate
    -> extent actuator and its local capacity
    -> processing receipt
    -> next permitted observation

public extent state -> authored drawing template -> later geometry observation
```

The order is meaningful. Applying the pending gate at receipt time lets it
inspect the receiver's current pending adjustment. Applying it before a long
delay would use an older pending read. Likewise, admitting a newly created
arbitration event to the passage is different from retransmitting the same
already selected event.

Each stage can succeed in its own narrow sense while the overall goal remains
unmet. A channel may acknowledge an unresolved observation. An arbiter may
select a valid response while retaining another goal as residue. A gate may
correctly hold while capacity keeps a gap wrong. The actuator may execute
every request correctly while a coarse correction repeatedly crosses the
desired gap.

This makes several superficially similar states distinguishable:

| Observed state | Where its explanation lives |
| --- | --- |
| No selected response | Open support or an unfilled response slot. |
| Selected hold | A resolved zero-change output of the actual law. |
| Waiting for receipt | The channel's occupied request/dispatch/receipt structure. |
| Change cannot be absorbed | Signed tension at the actuator's local capacity. |
| Another goal remains unselected | The arbiter's retained read residue. |
| Gap cannot be assessed | The gap claim's geometry, overlap or calibration evidence. |
| Visible drawing changes | Accepted extent change read through the connected construction. |

The construction still decides the visible shape. Opposed diagonal caps can
show a stretched six-sided opening; cardinal caps can show a rectangular
opening; inset caps reuse their height role differently. These four machines
coordinate inputs to those constructions. They do not replace them with a
shape label, and they do not establish that those construction rules were
learned.
