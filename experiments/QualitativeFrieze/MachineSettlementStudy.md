# Machines, their shapes, and what actually settles

## Start here

Two drawing constructions have separate width and height controls. The first
follows a supplied goal. The second can hold its own values, alternate on a
clock, or respond to a small message saying **increase, equal, or decrease**.
Those messages drive actual Gojo laws and signed carrier changes. The drawing
then reads the resulting state.

The experiment lets us see the machines behind the picture. A hexagon-like
opening is a read of two opposed constructions; an extent follower is the
mechanism that makes their spans agree. They are related, but they are not the
same concept.

Open the [interactive recorded study](results/machine-settlement/index.html).
Start with **Stretch the opening**, then try **Shrink the flat extent to zero**,
**Three-beat-old comparison**, and **The receiver cannot stretch far enough**.
The slider visits actual recorded states. The diagram shows the message,
requested change, accepted change, phase and remaining extent tension.

The [machine glossary](MachineGlossary.md) names fifteen existing mechanisms,
their inputs, their connected parts, and their limitations. It distinguishes
observed shapes, reusable actions, executing controllers and candidate engines.

## What is implemented in this pass

There are **four independent extent regulators**, grouped into two drawing
views: a width and a height regulator for each view. Each owns a value,
capacity, phase, finite response law, effect outlets and transition history.
The comparison sensors are separate connected structures. They observe only
explicitly advertised ports.

```text
goal width -> public comparison sensor <- first width port
                            |
                  comparison support only
                            |
                finite law -> signed unit outlet
                            |
                   first extent carrier

first width port -> public comparison sensor <- second width port
                              |
                    comparison support only
                              |
                  second regulator's law
                              |
                     second extent carrier

height uses the same mechanism through separate ports

four captured extent publications
    -> actual role bindings in two drawing constructions
    -> ordered phase expansion and reflected frame
    -> geometric, seam and zero-phase reads
```

The follower does not receive the other owner's exact value or the target
picture. The sensor's private witness retains the sampled values and the
actual source/receiver port correspondence. The transmitted packet contains
the comparison support, its basis, an event identity and an opaque sensor
identity. There is no link from that packet back into the private samples.

Each clock event permits at most one signed unit request per regulator.
Changes enter through the existing carrier tension mechanism. The attached
clamp retains an adjustment that cannot fit. Every admitted command satisfies:

```text
requested change = visible extent change
                 + change in held extent tension
                 + unrouted request
```

An observation is not a quantity transfer. These requests adjust an extent;
they do not claim to conserve ink, area, or the sum of all widths. A future
fixed-total-space experiment would need an explicit exchange law.

## A concrete encoding chart

The compact comparison mask addresses the three participants of a native
ordered comparison domain. It is a derived local read, not a replacement for
the historical ODV word.

This pass deliberately uses small integral carrier values for the adjustment
mechanics. Its public decisions remain coarse, and the drawing words retain
unary shared-unit participation. It is a bridge to the existing value kernel,
not a claim that exact extent counts were already present in a bare ODV bit.

| Published mask | Native support | Feedback-law response |
| --- | --- | --- |
| `001` | Current extent exceeds the source extent | Request one negative step |
| `010` | The two exposed extents agree | Request zero change |
| `100` | Current extent is smaller | Request one positive step |
| `111` | All three comparisons remain possible | Keep the response unresolved |
| `000` | No comparison supplied; open support | Keep the response unresolved |

The native law has three comparison participants, two phase participants and
three signed output participants. Six rows define one response policy. The
existing finite-law compiler derives a 32-entry byte lookup over the possible
input supports. Open support remains open; it is not silently compiled as a
closed empty set.

The three supplied policies differ in their **actual law rows**:

| Policy | Meaning of its rows |
| --- | --- |
| Hold | Every resolved comparison permits zero change. |
| Clock | The selected phase permits +1 or -1 regardless of comparison sign. |
| Feedback | Comparison sign selects -1, 0 or +1 in either phase. |

The executor does not dispatch on those names. An ambiguous comparison may
still yield a unique clock response because all admitted alternatives select
the same action in that phase. It cannot yield a unique feedback direction.

A missing comparison can consume an externally supplied clock event without
moving an extent. Pausing the clock consumes neither the event nor the phase.
Replaying an already consumed event does not deliver the request again.

The clock's wrap and its preserved cycle participation are separate from
extent tension. All displayed tension totals concern **extent adjustments**,
not every carrier in the complete graph. Likewise, “quiet” means no recent
accepted extent movement and no extent tension; the supplied clock may still
be running, and a missing observation may still leave a goal unresolved.

## Three constructions driven by the same controls

The templates contain native direction/extent-role uses in ordered phases.
Integer control publications expand into repeated references to one common,
uncalibrated reach. The conversion explicitly connects the sampled tick basis
to that reach. No absolute screen length is assigned to a primitive.

| Construction | Ordered phases of the first half | What the opposed pair can show |
| --- | --- | --- |
| Diagonal cap | Right and up by H; right by W; right and down by H | A stretched six-sided opening; a diamond-like limit at W=0 |
| Cardinal cap | Up by H; right by W; down by H | A rectangle-like outline when the two halves agree |
| Inset cap | Up by 3H; right by W+2H; down H; left H; down H; right H; down H | A paired outline with inward turns |

The second half uses the same instruction structure in a reflected vertical
frame. Reflection changes the spatial basis. Traversal order is preserved.
Independent extent inputs can still produce unequal shapes.

The diagonal cap is a deliberately authored construction. It does not claim
that the earlier four-strand weave was originally generated this way. It also
does not claim a regular hexagon: the diagonal and horizontal sides have
different construction roles. The glossary separately describes the existing
equal-partition circle construction and the openings read from the weave.

### What happens at the diamond limit

As W reaches zero, both horizontal phases have known zero extent. They remain
participating phases, with two distinct endpoint sites apiece. The visible
support looks like a diamond, while the construction still has six sections.

Removing those roles would be a structural simplification requiring its own
correspondence. The renderer does not perform it. This is precisely why a
shape name is not a sufficient description of the machine.

### What happens at the seams

The two paths initially have separate site identities. The experiment
explicitly declares a start/start and an end/end correspondence, then uses the
existing native graph-join reader to assess each declaration.

Equal positions admit a shared **quotient view**, preserving the independent
source paths below it. Unequal ends leave a blocked declaration. This is not
an inferred material weld or permission for flow to cross between the pens.
Coincident internal sites are not merged unless separately declared.

These are newly realized observations of the live extent state. They are not
a persistent pen cursor moving through a continuously deformed material path.
That stronger temporal geometry contract remains future work.

## What the recorded campaign found

The [prospective protocol](MachineSettlementProtocol.md) preceded the run.
There are **108 controller conditions**: twelve disturbances, three response
policies and three starting conditions. They are exercised through three
drawing constructions, yielding **324 construction/condition views**. The
three views reuse the same control problem; they are not independent evidence
of discovering feedback. Each run receives 28 clock events per regulator.

| Disturbance | Feedback meets both goals and pair agreement, with no remaining extent tension |
| --- | --- |
| Stretch, shrink to zero, increase height, or change both | 3 of 3 starts in each condition |
| Reverse the goal later | 3 of 3 |
| Temporarily missing or ambiguous follower comparison | 3 of 3 after observations resume |
| Permanently missing follower comparison | 0 of 3 |
| Three-beat-delayed comparison | 0 of 3 within the recorded horizon |
| Follower capacity below the desired extent | 0 of 3 |
| Alternating incompatible follower goals | 0 of 3 |
| Smaller goal after blocked growth | 3 of 3, after accounting for earlier tension |

Across the 36 conditions per policy, feedback meets the combined criterion in
24; hold meets it in none; the clock meets it in one. That clock case happens
to align when observations disappear. It is a useful reminder that one correct
terminal result does not establish an adaptive mechanism.

The first regulator reaches its own supplied goals in these conditions even
when its follower fails. The data therefore reports leader goal attainment,
pair agreement, remaining extent tension, and lack of movement separately.

### Following a changed span

For the first starting condition, the requested flat extent changes from two
to six at beat four. Each sensor samples before any owner responds.

| Beat | Requested extent | First extent after response | Second extent after response | Follower's published comparison |
| --- | ---: | ---: | ---: | --- |
| 3 | 2 | 2 | 2 | Decrease, from the preceding sample |
| 4 | 6 | 3 | 2 | Equal |
| 5 | 6 | 4 | 3 | Increase |
| 6 | 6 | 5 | 4 | Increase |
| 7 | 6 | 6 | 5 | Increase |
| 8 | 6 | 6 | 6 | Increase |
| 9 | 6 | 6 | 6 | Equal |

The follower's one-beat lag comes from the connected observation schedule. It
does not look ahead to the first regulator's next state.

### The failures explain the mechanism

**Delay:** an old increase signal can remain in flight after the target has
been reached. The follower overshoots, may accumulate tension at capacity,
and later reverses on another old signal. Different extents can cycle out of
phase. The study supplies no damping, predictive cancellation or automatic
latency compensation.

**Capacity:** the width-limited follower stops at three while the desired span
is six. In the representative run, 23 positive adjustment units remain held
after beat 28. They are not 23 units of geometric gap: they are accumulated
requests from a controller that keeps asking. This distinguishes current
goal mismatch from historical adjustment pressure.

**Release:** after the desired span falls to one, negative requests first
account for previously accumulated positive tension. Visible shrinking begins
later. Deleting that history would produce a faster-looking but different
machine.

**Conflicting goals:** the experiment alternates between following the first
extent and requesting extent one. This is a supplied selector exposing an
unresolved conflict, not a learned arbitration rule or simultaneous weighted
optimization.

## A small test of keeping a useful machine

The retention bench supplies three candidate laws: hold, clock and feedback.
Each candidate is restored fresh at extent two. One challenge asks for five,
the other for one. A native finite relation checks whether each initial
unequal comparison becomes a resolved equality. A second native gate requires
both challenges to qualify before retaining the actual candidate archive.

Only feedback qualifies. Fresh restored uses reach goals zero, three and six.
A goal of nine stops at capacity eight with four pending adjustment units
after ten pulses. Missing and ambiguous observations do not establish success.

This is a bounded selection and retention mechanism. The candidate laws,
sensors, trial length, and acceptance criterion are supplied. It discovers
neither a new law nor a construction topology. The retained native structure,
rather than its report label or a replacement byte buffer, supplies execution
on the fresh trials.

## Encoding, restoration and cost

All 36 first-start controller cases agree between the native finite-law reader
and the compiled reader. Focused tests also compare the admitted supports,
save and restore blocked owners, preserve port aliases across a composed
archive, reject replayed events, and prevent private sample leakage.

The live four-regulator composition with its sensor connections occupies
13,607 bytes in the existing general native graph transport in this run.
This is an inspectable general-purpose graph representation, not a claim of
minimal bit packing. The derived response lookup is only one small part of
the complete controller contract.

Two retained native study archives, [stretch](results/machine-settlement/stretch.gojo)
and [capacity](results/machine-settlement/capacity.gojo), are 1,020,452 and
995,262 bytes respectively. They include private sensor witnesses, captured
state publications, all recorded native drawing realizations and declared
seam witnesses, as well as the four controller histories. That difference makes history cost visible;
it must not be reported as the operational cost of a six-row law.

Timing in the JSON is local diagnostic timing, including construction and
controller graph encoding. It excludes the subsequent drawing realizations,
HTML writing and independent native/compiled comparisons. This pass is not a
performance benchmark or evidence that cost stays constant at larger scales.

## What guidance we supplied, and what remains open

We supplied the seed phases, public ports, unit correspondence, capacities,
desired extents, comparison sensors, candidate response laws and event schedule.
The actuator has no target pixels and receives no exact target magnitude.
The viewer does not invent intermediate coordinates between saved frames.

This demonstrates settlement of exposed construction parameters. It does not
infer a correspondence between arbitrary starting and ending shapes. An
instruction such as “become more hexagonal” still needs a connected shape
claim, a reader that can assess it, and legal response routes. General
whitespace corridors, collision avoidance, topology change and persistent
geometric deformation are not supplied by this scalar controller.

The most useful next additions are suggested by the failures: an exposed
pending-adjustment read, explicit request/acknowledgement or latency policy,
competing-response arbitration, and a geometric gap claim that activates a
permitted correction without giving the receiver a target outline. Each
should be added as a named, inspectable mechanism with a failing control.

## Reproduce and inspect

```powershell
dotnet run -c Release --project experiments/QualitativeFrieze -- --machine-settlement
python experiments/QualitativeFrieze/RenderMachineSettlementNotes.py
dotnet test Tests/QualitativeExecution.Tests/QualitativeExecution.Tests.csproj --filter "FullyQualifiedName~Coupled|FullyQualifiedName~ExtentRouteRetention|FullyQualifiedName~MachineSettlementStudy"
dotnet test Gojo.slnx
```

Implementation: [extent machine](Composition/CoupledExtentMachine.cs),
[comparison link](Composition/CoupledComparisonLink.cs),
[drawing template](Composition/CoupledShapeTemplate.cs),
[retention bench](Composition/ExtentRouteRetention.cs), and
[campaign](Composition/MachineSettlementStudy.cs).

The [recorded JSON](results/machine-settlement/study-data.json) includes every
condition summary and step-by-step first-start traces. The HTML viewer uses
only that recorded data. Human labels remain outside the execution semantics.
