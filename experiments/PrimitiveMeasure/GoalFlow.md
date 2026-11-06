# Where a goal gets its drive

In the race, B is trying to reach the finish. The reserve opens a gate. The race script supplies B's first request and its retry. The question here is whether we can remove those outside requests and still get an attempt, a wait through an obstacle, and a stop when the finish is reached. The smallest working example has a desired finish, a sensed finish, a bit of available work, and a passage between them.

**Concept.** A difference between a target and a sensed state can direct locally available work until that difference clears.

**Limit.** The tests supply the target, the destination, the response wiring, and the opportunities to settle. Choosing a destination or learning a better route would require further evidence and structure.

## What the original race connected

The organizer observes `A finished AND B finished`. A separate device releases its one stored activation when `B running AND contest incomplete AND B requesting AND ordinary gate closed`. Its released state opens an additional route. B still has to request passage on a beat when that route is open. The runners have no connection pointing them toward the reserve. [Race wiring and timeline](RaceOrder.md)

There is a subtle dependency. While B is running, the observed contest is necessarily incomplete. Removing the goal input deprives the helper of evidence and it holds. Installing an independently adjustable “I want to finish” reference lets us ask a different question. If we withdraw that reference while B is still running, does B still spend its available work?

## A runner with a request of its own

```text
desired finish ──┐
                 ├── desired AND NOT sensed ── gap
sensed finish ───┘                              │
                                                ▼
work supply ────[gap]────> pending ────[gap AND open gate]────> spent
                                                                  │ permission
body on course ───────────────────────────────────────────────────┴──> body at finish
                                                                           │
                                                          sensed finish <──┘
```

Setting the target changes the gap read immediately. Here the target bit means “seek an occupied finish.” Clearing that bit withdraws this demand. Seeking an empty finish would require an outgoing route and its own response law. On the next clock the gap admits a transfer from the work supply into the pending site. Another closed beat keeps the same participation there. When the passage opens, the work activation moves to spent. The runner's own activation moves to the finish, which clears the gap through its connected read. The clock gives the passages opportunities to settle. Every external finish-request bit in this run stays off.

| Moment | Gap | Location of work | Location of body |
| --- | --- | --- | --- |
| Before setting target | Clear | Supply | On course |
| Set target, observe | Present | Supply | On course |
| Clock with gate closed | Present | Pending | On course |
| Clock again, still closed | Present | Pending | On course |
| Clock with gate open | Clear | Spent | Finished |
| Clock again | Clear | Spent | Finished |

The gap is information that may be read repeatedly. Pending contains one actual, conserved participation in the work circuit. This is a one-bit analogue of a held request. Signed tension and a calibrated magnitude belong to the separate kernel experiment below.

| Change | What the structure actually does |
| --- | --- |
| Keep the same target and supply, omit the two work passages | The gap reads present through every opportunity. The work stays in supply and the runner stays on course. |
| Allow the original reserve to help, then withdraw the outside request | The helper opens the gate. The body waits until a second outside request arrives. |
| Keep the normal gate closed | Work stays pending. Repeated reads and beats require no additional fuel. |
| Open the gate with an empty supply | The gap stays present and the pending site stays empty. |
| Disconnect or conceal the finish sensor | The gap has either no compatible read or both possibilities. Neither observation releases fuel. |
| Withdraw the target while work is pending | Opening the gate leaves the packet parked. Restoring the target allows it to complete. |
| Complete the finish through a second authorized passage | The actual finish clears the gap. Opening the original passage afterward leaves the parked work there. Reclaiming that allocation would require an explicit return passage. |

Two structural rows connect the work sites. The packed bytes `58 42 01` contain `[supply, gap, pending]` and `[pending, admission, spent]` at three bits per local reference. In the body owner, `18` connects on-course, permission and finish at two bits per reference. The gap read itself also uses `18` in its own read table. Its two inputs are the target and the sensed finish, and its output is an observation. The table and connected sites give those bytes their meaning.

## A standing target that acts more than once

A second apparatus holds a one-bit reference meaning “keep this cell occupied.” Two supply sites each start with a packet. The same target-and-vacancy read gates both supply passages. On each external use, the cell transfers its contents to one of two receiving sites. Those receivers retain what was used, so all the participation is accounted for.

| Moment | Occupied supply sites | Cell occupied | Packets at receiving sites | Gap |
| --- | ---: | --- | ---: | --- |
| Set the target | 2 | No | 0 | Present |
| Settle | 1 | Yes | 0 | Clear |
| Use the cell, then settle | 0 | Yes | 1 | Clear |
| Use it again, then settle | 0 | No | 2 | Present |

The same target generates a second repair after the first use without someone issuing another order. When fuel runs out, the measured need remains. This table counts occupied sites for inspection. The control law reads participation bits.

## When the gap really is signed tension

The numeric kernel supports two distinct constructions. Both are exercised in the [kernel settlement tests](https://github.com/It3rate/Gojo/blob/master/Tests/Gojo.Tests/Experiments/GoalSettlementTests.cs).

| Construction | Where the change enters | Observed result |
| --- | --- | --- |
| Import a finite draw | Project a three-tick request through a signed frame, then explicitly import it once into an empty account. | Projection alone leaves the account at zero. Import leaves tension of −3. Connecting two supply ticks reduces it to −1. The final tick brings it to zero. Another read leaves it paid. Another explicit import issues a new draw. |
| Maintain a live comparison | Connect target and completed work to an output that publishes `completed − target`. | Raising the target to 3 settles the output at −3. Independent supply raises completed work to 2 and then 3, and the published gap follows −1 and 0. Lowering the target can also close the gap while the delivered work stays put. |

In the second construction the signed deficit is real on the output carrier. The supply route has its own law. It can deliver work even with the target at zero. To make the deficit *direct* delivery, it needs an outgoing response connection like the regulator's passage. A goal can therefore settle into a gap automatically, while settling the *world* toward that goal depends on which routes and resources are connected.

## Other forms of motivation

- **An appetite can persist between thresholds.** In the existing phototaxis controller, hunger and satiation use two thresholds. At the same intermediate fullness, steering can depend on whether the creature was recently hungry or fed. Memory supplies persistence without fresh commands. [Appetite test](https://github.com/It3rate/Gojo/blob/master/Tests/Phototaxis.Tests/DesignedControllerTests.cs)
- **Uncertainty can motivate a question.** The existing timing bench can spend stock to discriminate between two machines that still fit the observations. A repeatable result earns a test it can use again. The thing being sought is evidence. [Timing bench](TimingSocket.md)
- **Stored pressure can select a route as it settles.** In the draw experiment the stock transfers when its one-to-one route is attached. An observer can describe that motion as an arc toward paying the debt. To claim that the debt caused the delivery, we would also need to show that varying the debt changes the delivery. The maintained-gap control delivers available stock even at a target of zero.
- **Several needs can compete for one opportunity.** The follow-up connects three actions to independent claims about purse, standing and pickup. Experience changes which outlet earns the next opportunity, and a due pickup can change which need gets attention. [Which action helps?](TensionChoice.md)

## Reproduce the observations

```powershell
dotnet run --project experiments/PrimitiveMeasure -- --goal-flow experiments/PrimitiveMeasure/data/goal-flow
dotnet test Tests/Gojo.Tests --filter FullyQualifiedName~GoalSettlementTests
```

The [runner timeline](data/goal-flow/runner.csv) contains ten supplied trials and 64 recorded moments. Its `finish_observed` field shows the body's actual finish site. Its `finish_read` field shows what the work circuit was allowed to sense. In the runner CSV, Boolean support is 0 for an absent read, 1 for known off, 2 for known on, and 3 for either possibility. The [connection rows](data/goal-flow/wiring.csv) expose each circuit's packed local references. The [maintenance timeline](data/goal-flow/maintenance.csv) records the two stock packets throughout the run. The low five bits of its `material_hex` field hold the two stock sites, cell, and two receiving sites, in that order.
