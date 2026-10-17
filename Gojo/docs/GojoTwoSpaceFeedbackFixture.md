# Two adapting spaces exchanging observations

Status: exploratory selected contracts, September 7, 2026. This extends the
[landmark calibration experiment](GojoLandmarkCalibrationFixture.md) with
publication, local receipt and response consumption. No global convergence,
source truth or automatic trust law is assumed.

Try the [stepped demo](examples/two-space-feedback.html). Its four scenarios
and 23 stages are checked against the actual Gojo execution by the
[syntax tests](../../Tests/Gojo.Encoding.Tests/GojoSyntaxTwoSpaceTests.cs).
The [authored fixture](../../Tests/Gojo.Encoding.Tests/Fixtures/Syntax/two-space-feedback.gojo)
uses existing syntax. The new [exchange readers](../Structures/LocalCalibration.Exchange.cs)
compose with the previous calibration and signed-response readers.

## An example: two estimates of the same quantity

Imagine two local estimators observing the amount of water in a vessel through
different signals. Space A receives a landmark of ten; space B receives twenty
in another input basis. Their initial calibrations predict five and nine in a
common comparison unit. Neither has direct access to the other's signal or
calibration. These are chosen exact toy premises, not learned tolerances around
the landmarks or a probability model of the sensors.

B also uses a different output basis: its native eighteen corresponds to A's
nine. The connecting relation explicitly maps two B ticks to one A tick. The
two subject slots and contexts are different too; the link records their
correspondence. Similar numbers or compatible units do not establish it.

Both publish before either changes. A receives B's eighteen as nine and
proposes moving halfway from five to nine. B receives A's five as ten in B's
basis and proposes moving halfway from eighteen to ten. A's actuator moves
from fifty to seventy; B's moves from one hundred eighty to one hundred forty.
Their new predictions agree at seven in A's comparison unit.

The application order remains explicit. When A applies first, B still has the
earlier publication of five. B does not reach into A's actuator and substitute
its newer seven. After both changes, new publications can carry the new values.

| Step | A's current view | B's current view in A's unit | Retained messages, in A's unit |
| --- | ---: | ---: | --- |
| Publish | 5 | 9 | A: 5; B: 9 |
| A applies half response | 7 | 9 | A: 5; B: 9 |
| B applies half response | 7 | 7 | A: 5; B: 9 |
| Republish and receive | 7 | 7 | A: 7; B: 7 |

These messages are non-draining observations. Material change occurs only
when the receiver separately applies its proposed delta to its own actuator.
No transfer of water is implied by communication about the amount of water.

## Agreement, oscillation and separate evidence

Under this particular captured-pair schedule, with compatible conversions and
no rounding or blocked actuator change, the chosen response law reads:

```text
A_next = A + alpha_A * (B_published - A)
B_next = B + alpha_B * (A_published - B)
```

When both publications capture the current pair, the signed difference after
both applications is `(1 - alpha_A - alpha_B)` times the earlier difference.
With both response fractions one half, it becomes zero. With both fractions
one, its sign reverses: the estimates swap between `(5,9)` and `(9,5)`. The
tests stop the full-response case after each local six-attempt budget. This
explains the small example; it is not a convergence result for arbitrary
delays, nonlinear relations, rounding, capacities or networks.

The halves are selected response strengths. They do not encode source
confidence, statistical independence, utility or responsibility for error.
The driver chooses these policies and the delivery order; the system has not
discovered them. No global element computes the answer seven for the spaces.

Each space also retains its own reference observation: two for A, four in B's
basis. After agreement at seven, explicitly selected responses to those local
references change the views to two. New peer publications then agree at two.
This shows that agreement can be challenged from outside the exchange loop;
the local reference observations still remain premises, not guaranteed truth.

Another test supplies incompatible local references: two for A and sixteen
for B, which reads as eight for A. Local full responses establish `(2,8)`;
peer half responses establish `(5,5)`. Returning to local evidence disturbs
that agreement again. The local histories retain both kinds of response and
their still-incompatible residuals. Neither the current peer agreement nor
the accumulated number of messages certifies a correct interpretation.

## Structure before execution

Each participant keeps its own calibration state, focus, application history,
response policy and attempt controller. The new boundary uses ordinary groups:

```text
publication port = group(unit frame, producer sequence carrier)
publish request = group(current calibration state, prediction witness,
                        publication port, publication event,
                        exposed antecedents, exposed expectation)
message = group(observation, exposed antecedents, exposed expectation)
observation = group(publication port, publication event, copied scalar,
                    subject slot, context)
antecedent = publication port -> publication event
prediction request = group(local report, current candidate)
comparison request = group(prediction, imported observation, feedback map,
                           local report -> imported observation)

link = group(remote publication port, local feedback port,
             remote subject -> local subject, remote context -> local context,
             remote-to-local calibration)
inbox = group(link, latest receipt or open slot, consumed receipt or open slot)
receive request = group(message, current inbox, local arrival event)
receipt = act(message, receive request, imported observation,
              unresolved participation, exposed evidence)
respond request = group(plan, current local state, local application history,
                        local attempt controller, receipt, current inbox)
```

The publication's local witness retains its private prediction and calibration
premises. Only its message crosses the boundary. Exposed antecedents identify
immediate observations by port and event; they do not contain a neighbour's
private graph and are not traversed to infer independence or confidence.
The antecedents can themselves remain an open slot. An empty group means no
antecedents are exposed, not that the observation is statistically independent.
The exposed list is not a claim to complete causal ancestry.

Sequence coordinates order publications on one selected port. They are not
elapsed time or a clock shared by the spaces. The receiving space records an
arrival event on its own clock. It can reject an older publication than one
already received, but cannot know about a newer publication still in transit.
Application counts and their timelines use separate, per-space carrier axes.
There is no comparison of a producer sequence coordinate with an arrival
coordinate or an application count to determine global age.

A well-formed newer message with an open scalar replaces the current concrete
receipt with a pending observation. Retained old evidence remains historical;
this selected response policy does not silently fall back to it. An invalid
route or unrepresentable calibration does not install a new current receipt.

Applying a received response requires both the local state version used for
the plan and the exact current receipt. The selected receipt can cause at most
one application in that inbox, including a zero application. A new publication
can request another response without becoming independent evidence. Each
application enters the local actuator through signed tension.

Inbox and state histories are caller-owned, sequential context. There is no
hidden delivery registry or concurrent transaction over both spaces. Retained
premises and publication events are fixed for their version. Independent
packet restoration creates another world; reconnection requires correspondence.

## Delayed and pending messages

In the delay experiment B first receives A's five, responds halfway and now
predicts seven in A's unit. B's original nine is still in transit, followed by
a newer seven. If nine reaches A first, A can respond to it: it is the newest
publication A has received. This is a locally available view, not an assertion
that the sender has remained unchanged.

When the newer seven arrives, it becomes A's current receipt. An older nine
arriving again cannot replace it. The receiver compares producer sequence
coordinates only within the same connected publication port. Equal sequence
coordinates with different event identities are also refused as a sequence
collision; this does not make their observation identities equal. The sender
owns its publication sequence. The publishing reader does not maintain a
hidden sequence registry.

A planned response depends on two distinct versions: the receiver's local
calibration state and its selected current receipt. Changing either invalidates
that proposal. A newer receipt invalidates the old plan even when its scalar
value is unchanged. After an application, replanning against the same receipt
cannot consume it again, including when the earlier delta was zero. Receiving
a genuinely newer publication can permit another response while still retaining
its dependence on earlier evidence.

A newer partial publication may instead say the current value is unavailable.
Receiving this well-formed message succeeds as admission and installs an open
observation. Numeric comparison then remains unresolved. The previous concrete
receipt stays historical; this selected current-response policy does not use
it as a fallback. A later concrete publication allows the experiment to resume.
The authored fixture constructs that partial announcement directly. The current
publishing reader exports completed predictions from a settled calibration;
it does not discover or schedule invalidation announcements automatically.

## Boundaries this experiment deliberately keeps

- Each action mutates one free local actuator through the previous signed
  response contract. There is no atomic mutation of both spaces, coupled
  material transfer, automatic queue or concurrent scheduler.
- Only the exported message enters the receiver. Its receipt preserves basis
  conversion, subject/context correspondence, publication identity, arrival,
  exposed antecedents and open expectation. No private ancestry traversal is
  needed to perform that read.
- A received scalar is interpreted conditionally through the authored link.
  The receiver does not verify the sender's private mathematics or its truth.
  Invalid local arithmetic, routes, stale versions and unrepresentable results
  are different concerns from whether the external claim is useful.
- Application budgets belong to each space. Exhausting A does not stop B or
  spend B's attempts. Resolution remainder and unpaid actuator draw continue
  to use the existing response law; neither disappears into communication.
- Received messages, templates and link premises are owned as fixed versions.
  Publication copies scalar/event coordinates, but this is not a deep freeze
  or authenticated network protocol. The caller retains current inbox and
  state histories together; dropping those histories discards their guarantees.

## Verification

1. Distinct input and output bases, subject/context correspondence, private
   calibrations and clocks; exchanging messages does not drain either source.
2. Half responses to captured peer publications, compared with full responses
   under the same explicit delivery order.
3. Separate local evidence can challenge agreement. The harness supplies event
   order and published evidence, never a hidden answer to either participant.
4. Delayed, repeated, reordered and pending publications; a newer receipt
   invalidates an unconsumed plan even if the numeric value happens to match.
5. Resolution stalls, bounded attempts, copied publications and restoration
   with messages in transit and one participant already changed.

Additional tests refuse a rewritten receipt scalar or arrival event, retain
exact fractional conversion, preserve the previous inbox on overflow, and
restore independent application timelines with their message evidence. A
message restored alone cannot reconnect by numeric coincidence; an explicitly
supplied new port, basis and subject/context correspondence makes that read
available again.

The numeric inventory discovers the new fixture and compares its complete
graph through two restorations. The demo is a standalone local HTML page with
no external assets. Its displayed actuator and publication values are checked
against Gojo execution; desktop/mobile browser checks cover all 23 stages.

The existing syntax constructs the participants and all request patterns. The
host selects the readers and event order; these selected contracts can inform
a future compiler without introducing one now.

On September 7, `dotnet test Gojo.slnx` passes **1,350 tests**: 782 kernel,
392 encoding, 155 CAD and 21 gear-ladder. This pass adds 25 semantic/demo cases
and one numeric-inventory case for the eighteenth authored syntax fixture.
After moving prediction/comparison request construction into authored patterns,
the focused exchange and complete numeric-inventory checks also pass.

The next question is how a space selects and changes its responsiveness when
local evidence and neighbouring requests disagree. A small next experiment
could retain two response policies, measure their subsequent local effects
and costs, then explicitly revise participation. That would test a proposed
utility law. The present work establishes the exchange and failure boundaries
on which that experiment can depend; it has not learned trust or blame.

**Subsequent clarification:** agreement alone does not supply a predictive or
reward objective. The next implemented prerequisite is the
[prospective calibration fixture](GojoProspectiveCalibrationFixture.md): an
explicit future target, a forecast recorded before feedback is available, and
later assessment against that target. It begins with sample-derived affine
continuations. Learning responsiveness should use subsequent consequences,
not treat a quiet exchange as sufficient evidence of usefulness.
