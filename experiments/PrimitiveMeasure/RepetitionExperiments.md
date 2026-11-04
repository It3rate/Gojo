# Repetition and composed shape structures

How do you draw a square when nobody has chosen its size? Use the same
positive length each time you move, make a quarter-turn in the same sense,
and repeat until you face the starting direction after a whole turn.
For a rectangle, alternate two lengths. Reusing a length is what makes
opposite sides match. The controller decides when the walk stops.
[Step through the repeated walk](https://gojo.inklingai.ca/encoding/explorer/?layer=14)

Now leave the rectangle instructions alone. Have a second device move the
starting place during a five-beat wait. The next rectangle follows the same
instructions at its new location. You can also construct a square by joining
four equally spaced marks on a circle. Each construction supplies different
ways to vary the result. [Try the moving pattern](https://gojo.inklingai.ca/encoding/explorer/?layer=15) · [Try the circle construction](https://gojo.inklingai.ca/encoding/explorer/?layer=13)

Round 10 encodes the repeated body, its controller and its geometric
interpretation separately. The examples require no assigned side lengths
or coordinates.

```powershell
dotnet run --project experiments/PrimitiveMeasure -- --repetition
dotnet test Tests/PrimitiveMeasure.Tests/PrimitiveMeasure.Tests.csproj
```

## The small mechanism

One occupied V site represents an activation. An incoming clock pulse permits
at most one handoff from its current site to a successor. The handoff uses the
existing eight-byte passage law: a bitmap of allowed complete before/after
patterns for `[source, control, target]`. Feedback is an ordinary connection
back to an earlier site. There is no power opcode or hidden repeat counter.

The same O/D/V encoding remains unchanged. State sites and guard observations
use the existing fixed-positive-context, one-value-bit arrays. A guard can bind
to a public read port on another structure. This is observation: it does not
drain the supplier or create another material flow. Resolved observations are
imported into the local guard sites even when the activation stays put.

```text
participate using A -> turn -> participate using B -> turn -> check
       ^                                                      |
       |                         stop absent -----------------+
       |                                                      |
       +--------------------------------       stop present -> done
```

The diagram names the sites for explanation. The stored successors, shared
parameter references, guard bindings and law bits determine what happens.
An unknown guard preserves alternative successors and refuses mutation.
No admitted observation is different from a known absent guard.
When the law has the same material outcome for either guard value, the external
guard is not consulted. An unconditional continuation therefore remains usable
if the original branch condition later becomes uncertain.

For one pulse, two simultaneously enabled outgoing rows are a conflict. The
controller holds instead of choosing by array order or copying activation.
Round 9's explicitly ordered priority sequence remains available as a different
composition; this new controller does not quietly supply that priority.

## Samples and their limits

| Construction | What runs or is read | What remains qualified |
| --- | --- | --- |
| Square body | One shared reach participates at four successive phases, then completes | A stable reach and the attached equal quarter-turn basis justify the geometric square |
| Rectangle body | A, B, A, B participate over four phases | A and B are shared unknown reaches, not fresh samples chosen on each pass |
| Three-part body over four phases | Twelve participations before both body boundary and phase return agree | This establishes recurrence; it is not a four-edge rectangle |
| Two-part body over five phases | Ten participations before completion | No stored lap counter is consulted |
| Continuous rectangle controller | Feedback continues through further traversals | The demonstration's observation budget is not a stop rule |
| Reusable requested motif | Request enters the body; completion stays visible until request is released | This is a level handshake, not automatic retriggering every beat |
| Rectangle and five-beat wait | Five distinct waiting stages precede another request | Beats are supplied events, not seconds |
| Displacement during the wait | Another component completes a shared formal offset; next motif binds to its new placement | Offset direction and magnitude can remain open |
| Independently moving placement | Each body participation binds to the then-current placement | Local repetition survives; world-space closure is not established |
| Circle without a polygon | A shared radial reach throughout a whole contour, with an explicit return binding | Selected homogeneous planar Euclidean constraint; no field simulation |
| Triangle from circle | Three equal contour parts with successive straight chords | No radius or side length assigned |
| Square from circle | Four equal contour parts with successive straight chords | Same square family as the path construction under compatible frame/reach bindings; histories differ |
| Pentagon from circle | Five equal contour parts with successive straight chords | Five arbitrary markers would not establish regularity |
| Hexagon and further polygons | Same shared-part construction with more connected stages | A different partition, not a larger radius |
| Rectangle from circle | Four positive arc shares A, B, A, B, joined by successive chords | Opposite vertices are antipodal under this whole-circle contract; A need not equal B |
| Conditional fork and rejoin | One guard selects one branch; both branches lead to one later state | Rejoining alternatives is not addition of two amounts |
| Completion barrier | A third activation advances when both supplied completion ports are present | Observes both; does not consume their completion states or power the workers |
| Contact-controlled loop | False feeds back, true completes, unknown holds at the check | Contact evidence is supplied; no self-distance detector has been invented |
| Shrinking spiral candidate | Turn/body feedback and the stopping interface are available | A changing-reach relation and proximity read remain open; the current contact example does not shrink |

## Exact rectangle body encoding

The standalone rectangle controller uses these local port positions:

| Port | Role |
| --- | --- |
| 0 | Participate using A |
| 1 | Request one turn |
| 2 | Participate using B |
| 3 | Request one turn |
| 4 | Check the stopping condition |
| 5 | Completion |
| 6 | Request guard, reserved for the requested-body form; initially present here |
| 7 | Stop guard, bound to the phase cycle's starting-site read |
| 8 | Ignored control position for unconditional rows |

Only one of the state sites 0 through 5 is occupied. Guard bits are observations,
not additional activations. The initial packed value bytes are `41 00`: state
0 and the reserved request guard are present. The requested-body form adds an
idle state and uses that request guard; its port positions consequently differ.

Each reference uses six bits. Each three-reference row uses eighteen bits.
Rows are contiguous within their table, low reference and low bit first;
only the last byte of each array has unused padding.

| Shared law | Ordered rows `[source,guard,target]` | Packed bytes |
| --- | --- | --- |
| Unconditional | `[0,8,1] [1,8,2] [2,8,3] [3,8,4]` | `00 12 04 88 20 20 C3 80 10` |
| Control present | `[4,7,5]` | `C4 51 00` |
| Control absent | `[4,7,0]` | `C4 01 00` |

Their law bytes are unchanged from the earlier passages:

| Law | Bytes |
| --- | --- |
| Unconditional | `01 00 04 00 12 20 48 80` |
| Present | `01 02 04 00 10 20 48 80` |
| Absent | `01 00 04 08 12 20 40 80` |

The parameter attachment table stores `[participation port, parameter-slot
reference]`: `[0,0] [2,1]`, packed as `00 20 04`. Parameter 0 is A and parameter
1 is B. Row roles identify their local address spaces: parameter 0 is not an
alias of control-state port 0. These are formal shared reach slots, implicit
in the local parameter-reference space, rather than allocated amount stores.
Stable nonzero reaches are the geometric construction's input promise. The
controller can visit those roles before any physical reach or measuring device
has been bound; its completion alone does not establish physical movement.

## Phase and geometric turning

The separate phase structure has state sites 0 through 3 and guard site 4.
Its present-controlled rows are:

```text
[0,4,1] [1,4,2] [2,4,3] [3,4,0]
```

They pack as `00 11 04 84 20 10 C3 40 00`. Its guard observes the OR of the
body's two public turn-site ports. The body stop guard observes the phase
structure's public starting-site port. The OR is an ordinary read-law bitmap,
not a second material source.

One supplied beat first advances the phase structure if a turn is requested,
then advances the body. At a check site the phase is held. The resulting trace is:

| Participation | Parameter reference | Phase reference |
| --- | --- | --- |
| First | A | 0 |
| Second | B | 1 |
| Third | A | 2 |
| Fourth | B | 3 |

After the first body pass, phase is 2 and the stop read is absent. Feedback
returns activation to site 0. After the second pass, phase is 0 and the stop
read is present. Activation moves to completion site 5. The check occurs after
participation, so the initial phase 0 does not trigger an immediate exit.

A bare phase cycle proves none of these steps is a quarter-turn. The experiment
adds a correspondence to four **equal parts of a whole circular traversal**.
The binding rows `[phase site, exposed arc start, exposed arc end]` are:

```text
[0,1,5] [1,5,6] [2,6,7] [3,7,2]
```

These pack as `40 50 04 85 21 18 C7 70 08`. The contour itself carries the common
radial reach, whole traversal, equal-part references, and end-to-start binding.
`HasEqualTurnBasis` reads that connected evidence; it is not a stored flag.
Unequal contour parts or a broken exact contour contract invalidate this read.

The square uses a body with only one participation and one turn. Its same
parameter reference therefore appears in all four phases. Both constructions
require the shared reach to stay stable during the construction. A live source
that changes its hidden length while still reading V=1 does not satisfy that
promise merely because its reference stayed the same.

The phase cycle can also drive beats without any contour binding. Its positions
then remain ordered control stages. No geometric meaning is inferred.

Three kinds of return remain separate. Returning to a controller stage is
recurrence. Returning to a geometric position is closure in that read. Binding
the new endpoint to the original endpoint establishes topology. The repeated
body provides the first; suitable reach/turn constraints justify the second;
the contour's explicit return connection supplies the third. None is inferred
merely because two stored V bits are equal.

## Composing a motif with another device

The parent exposes a drawing request, followed by five waiting states:

```text
Draw --body complete--> Wait0 -> Wait1 -> Wait2 -> Wait3 -> Wait4
 ^                                                          |
 +---------------- driver complete -------------------------+
```

The body's request input observes Draw. A separate displacement device observes
the OR of the waiting states. It acknowledges the request once, then holds
completion until the request is released. The parent's final waiting stage
requires that completion, so a slower device could hold the parent there.

The explicit beat order is body, displacement device, parent. This is an
ordered observation/transition schedule, not a claim of physical simultaneity
or a transaction across structures. In the supplied trace:

| Clock pulse | Event |
| --- | --- |
| 0 | First motif accepts its request at placement 1 |
| 10 | Motif completes; parent enters Wait0 |
| 11 | Displacement completes; parent leaves Wait0 |
| 12 through 15 | Remaining waiting stages advance; parent returns to Draw |
| 16 | Next motif accepts the request at placement 2 |
| 32 | Third motif accepts a request at placement 3 |

Thus there are five waiting-stage transitions, plus the next beat needed for
the body to receive the new request. Communication latency is visible instead
of being folded into an unexplained numeric delay.

The placement device creates formal translation relations:

```text
[old placement, new placement, shared offset]
[1,2,0] [2,3,0]                 bytes 01 02 00 02 03 00
```

The motif instances bind to those exposed placements:

```text
[instance position, placement reference]
[0,1] [1,2] [2,3]               bytes 00 01 01 02 02 03
```

These are new placement references, not numeric coordinates. Offset 0 can
remain an uncalibrated displacement. A diagonal interpretation would need its
own connected directional components. We have not assumed that equal-looking
value bits establish equal horizontal and vertical distances.
Guaranteeing a spatially advancing frieze additionally requires a nonzero
translation promise. An open offset does not establish distinct geometric
positions merely because its successive placement references are distinct.

`MovingFramePattern` instead advances the placement driver on every beat while
the local rectangle body continues. The first four sampled bindings are:

| Parameter | Local phase | Exposed placement |
| --- | --- | --- |
| A | 0 | 1 |
| B | 1 | 3 |
| A | 2 | 6 |
| B | 3 | 8 |

The local A/B pattern persists, but these participations do not share one world
placement. The resulting path is a composition with moving-frame contributions;
its closure or exact shape remains unresolved. No numeric positions are needed
to expose that difference.

Both placement examples retain a small bounded construction chart. This is
demonstration history, not a mandatory permanent log. Filling that chart refuses
further construction before advancing the participating components.

## A circle and polygons without assigned values

`ContourPattern` selects a homogeneous Euclidean planar radial-path law. Every
point along an admitted arc maintains the same reach from the centre. Its
ordered parts cover one whole traversal; the actual traversal end is connected
back to the actual reference site. These are the geometric premises. A finite
collection of equal-radius endpoints alone would not supply them.

The fixed local layout starts with centre 0, reference site 1, unwrapped end 2,
radius read 3, and whole-traversal participation 4. Other local sites and part
parameters follow. The base rows are:

```text
radial attachment: [0,1,3]
whole traversal:  [1,2,4]
return binding:   [2,1]
```

The whole and radius have supplied zero/nonzero observations. Nonzero does not
mean a radius of one. The separate unwrapped end keeps a full traversal distinct
from doing nothing, even though the return binding closes at the start.

A circle can use one arc `[1,2,32]`, with no polygon at all. For a pentagon the
five consecutive parts are:

| Arc start | Arc end | Shared part reference |
| --- | --- | --- |
| 1 | 5 | 32 |
| 5 | 6 | 32 |
| 6 | 7 | 32 |
| 7 | 8 | 32 |
| 8 | 2 | 32 |

Reusing part 32 constrains equal intrinsic shares of this same whole. Five parts
therefore establish fifths without assigning degrees or using pi. With six-bit
references these rows pack as `41 01 16 06 68 1C E0 81 80 88 00 02`.
The return binding is `42 00`.

A separate straight-chord table joins `[1,5] [5,6] [6,7] [7,8] [8,1]`, packed
as `41 51 18 C6 71 20 48 00`. Notice the last chord uses reference site 1 through
the established return correspondence. It does not treat the distinct unwrapped
end 2 as the same site merely because a drawing could put them together.

The same construction with three, four, six or seven equal parts gives the
corresponding regular polygon constraints. Four alternating part references
`32,33,32,33` give a rectangle: each opposite pair of vertices is separated by
the same half of the whole. Four unrelated part references do not establish
that correspondence. Skipping markers to make a star is another construction;
the successive-chord reader deliberately refuses to label it the same polygon.

Radius zero leaves a degenerate construction; uncertain radius preserves both
possibilities. Missing observations, an open return, a wrong reference site,
and a disconnected arc chain remain distinct failures of the complete read.
The arcs and reference roles remain present in these cases.

This follows the current Gojo [circle construction contract](../../Gojo/docs/GojoCircleConstructionFixture.md)
at a smaller encoding level. It does not import its numeric carriers, discover
a physical isotropic field, or supply coordinates to a renderer.

## Branches and rejoining

The tested fork uses state sites 0=start, 1=left, 2=right, 3=join, and guard 4.
Its present row `[0,4,1]` and absent row `[0,4,2]` select complementary paths.
Unconditional rows `[1,4,3]` and `[2,4,3]` rejoin them. The activation reaches
one branch and later one join; it never appears in both branches.

If guard 4 admits absent and present, the predicted destinations are `{1,2}`.
Execution holds at 0. If two unconditional rows instead offer both destinations,
that is a conflict, not uncertainty about which known rule applies.

A completion barrier is different. A third waiting activation observes the AND
of two public completion surfaces. It proceeds only when both are present.
The AND is a calculated bitmap relation. Reading completion does not consume
either source. These tests do not claim to split one conserved material amount
into two workers, sum their amounts, or infer that geometric paths touch.

## What a spiral still needs

The existing loop can now stop on a contact or proximity read. An unknown read
holds at the check; an explicit false observation permits another body pass;
an explicit true observation selects completion. Conditions are checked at body
boundaries. Reacting in the middle of a reach would need a finer boundary or an
interrupting connection.

A shrinking spiral needs more than that controller. Its successive reaches
must be related by an actual decrease, and the contact question must name the
earlier path and the comparison basis. V=1 before and after cannot distinguish
shrinking, growing, or staying unchanged. A shrinking process also need not
ever reach a contact threshold; termination needs evidence of its own.

The next decision is what evidence should supply those relations: a refinement
ladder, relative order constraints, a participating sensor, or a finer measure.
This experiment leaves that geometric input open. The contact-controlled demo
is a controller test, not a falsely completed spiral simulation.

One candidate next primitive is a part/whole connection: the previous positive
reach consists of a retained positive part and a removed positive part, in the
same additive basis. The retained part is then strictly smaller without either
length being numbered. That would require a real partition/accounting law and
a feedback binding to the retained part; the current whole-handoff passage
cannot supply it. It would establish decrease, not automatically prove spiral
geometry, self-contact, or termination.

## Encoding and execution boundaries

The new pieces are clocked passage composition, explicit phase references,
shared parameter attachments, public request/completion bindings, and a narrow
radial traversal/partition contract. No global identifiers or shape opcode were
added. All references are local to their declared owner and role.

Six-bit references are a convenient fixture width, not a geometric resolution.
The small placement charts use eight-bit references. Existing owner-local value
maps, table sizes and cross-structure port bindings provide their context;
the displayed byte streams are not a self-describing persistence format.
Public read bindings are retained as local port connections in the fixture,
not encoded into an invented global owner-address number.

Prediction explores at most eight distinct outgoing guards at an active site.
Only a resolved, nonconflicting pulse commits. A law with no unique per-case
transition is refused by this execution foothold. Arbitrary concurrent pulses,
general scheduling, numeric distance detection, material splitting, automatic
route growth, and cross-owner atomic settlement remain outside this pass.

The design borrows reusable bodies and output-to-input feedback from
[RepeatSurface](../../Gojo/Structures/RepeatSurface.cs), and phase/control ideas
from the current pattern experiments. The repository's
[Core2 and Core4 continuity review](../../Gojo/docs/GojoCore2Core4ContinuityReview-2026-09.md)
records the older pattern-editor lineage; those numeric engines were not imported.

## Verification

The `--repetition` demonstration completes successfully. All 250 primitive tests
pass, including 45 new cases. Final full-solution verification with
`dotnet test Gojo.slnx` passes: 5,806 tests across 14 projects, no failures or
skipped tests. Tests cover actual clocked feedback, resolved and uncertain
guards, ignored-control independence, observation refresh on holds, public
request/completion handshakes, waits and frame bindings, retained reference
correspondence, and refusal of unsupported geometric reads.
