# Saturation branches and pre numeric rectangles

Round 9 extends the examples in [ConditionalExperiments.md](ConditionalExperiments.md).
There are two executable parts: routing blocked contents through existing passage
rows, and reading a rectangle from shared unknown reaches in an explicitly
perpendicular frame. The latter is a small geometry constraint experiment;
it does not simulate radiation or derive perpendicularity from a blocked bit.

The useful result is that a connection can preserve **what must correspond**
without specifying how much is there. A blocked passage also preserves what
has not moved. Those two capabilities give us a base for exploring new routes
without pretending we already have a law that grows them.

```powershell
dotnet run --project experiments/PrimitiveMeasure -- --saturation
dotnet test Tests/PrimitiveMeasure.Tests/PrimitiveMeasure.Tests.csproj
```

## The simple picture

Imagine contents waiting at a junction. An ordinary passage tries to hand them
to the next site. If that site cannot accept them, a second passage can hand them
to a side site. If neither accepts, the contents stay where they are.

The junction has no extra amount of its own. It consists of existing value sites
and connection rows referring to them. A row is a stored relation, rather than
a third material store or a coincidence inferred from positions in memory.

An **eight-byte transition law** is a bitmap of allowed complete before/after
patterns for three roles: source, control, and target. The existing law allows
a whole handoff into an empty target. It cannot combine two occupied stores,
so occupancy acts as capacity *for this law*. Nonzero alone never means full.

A branch from one site and an extension of a whole line need different
correspondence. A branch only identifies its departure site. A parallel line
requires a shared offset across the original line. The rectangle experiment
supplies that stronger relation explicitly.

## Situations and what they establish

| Situation | Structural requirement | Result and limit |
| --- | --- | --- |
| Continue beyond a landmark | A passage connects the pending site to a site beyond it | Executed. A threshold observation need not force a turn. |
| Escape when forward is blocked | Forward attempt followed by a side passage controlled by forward occupancy | Executed for all eight states. |
| Allow only one side | Only that side has a passage | Executed routing mechanism; an L drawing additionally needs an angle relation. |
| Allow both sides | Two rows share one source | Executed with explicit priority. One handoff does not fill both branches. |
| First side is occupied | Second passage remains available | Executed. Priority can fall through to the next accepting outlet. |
| Both sides are occupied | Neither passage can accept | Executed. Pending contents remain. |
| Forward capacity is uncertain | Keep both complete possible states | Executed prediction. Neither alternative is secretly selected for mutation. |
| No escape route exists | Keep the blocked source and its existing relations | Executed repeatedly. Waiting does not invent an outlet or a larger amount. |
| Add an escape later | Explicitly attach another ordinary passage row | Tested. The previously held contents can then move. Attachment is supplied, not learned. |
| End branch versus whole-line radiation | Single-site attachment versus correspondence over a span | Different requirements. Current routing alone cannot distinguish their geometry. |
| Parallel boundary on one side | A common offset and parallel correspondence | Executed as rectangle boundary constraints, with unknown positive reaches. Radiation dynamics remain open. |
| Parallel boundaries on both sides | Two offset constructions relative to the same source span | Proposed composition. Each may have a different reach; symmetry requires shared reach. |
| Nonuniform radiation | Offsets may differ along the source | May form a bent or irregular boundary. A parallel-line read is unjustified. |
| Radiation without hardening | Transient participation remains accounted for without committing a reusable route | Proposed dynamics. Absence of a retained route is not disappearance of material. |
| Encounter another structure | An exposed receiving surface and compatible acceptance/coupling law | Existing passages model acceptance once attached. Discovery, attachment and hardening are not automatic. |
| Rectangle by path or parallel pairs | Same joined corners, frame, and reused reaches | Both assembly orders produce the same boundary bytes. Process history and interior fill are separate. |
| Circle from a point | A radial rule plus equal-radius correspondence across directions | Not established by the current four-lane fixture. Radiation alone is insufficient. |

An L, T or Y therefore needs both routing and geometric constraints. The same
branching graph can be drawn as a T or Y. A bitmap that chooses an outlet does
not, by itself, establish the angle of that outlet.

## Exact overflow encoding

All three value sites live in one owner with constant O=0, D=0 and one V bit.
Ports and bindings are initially in the same order as the packed value bits.
The connector follows the existing local port mapping; this experiment does
not change the addressing scheme.

| Local port | Value bit | Meaning in this example |
| --- | --- | --- |
| 0 | 0 | P, pending contents |
| 1 | 1 | F, forward receiver |
| 2 | 2 | L, side receiver |

The value byte is `P + 2F + 4L`. The upper five bits are unused.
Each connection reference uses two bits, low reference first, low bit first.

| Visit order | Table law | Row roles as local ports | Row byte | Law bytes |
| --- | --- | --- | --- | --- |
| First | Unconditional handoff | `[P,L,F] = [0,2,1]` | `18` | `01 00 04 00 12 20 48 80` |
| Second | Handoff when control is present | `[P,F,L] = [0,1,2]` | `24` | `01 02 04 00 10 20 48 80` |

The first row uses L in its required control position, but its law ignores that
position's value. It preserves L. This reuses the three-role format without
creating a dummy value. The second row uses F as an actual control read.
Both visits are one atomic local step: there is no externally visible partial
update between them.

| Before `[P,F,L]` | Value byte before | Value byte after | Explanation |
| --- | --- | --- | --- |
| `[0,0,0]` | `00` | `00` | Empty remains empty |
| `[1,0,0]` | `01` | `02` | Forward accepts |
| `[0,1,0]` | `02` | `02` | No pending contents |
| `[1,1,0]` | `03` | `06` | Forward cannot accept; side accepts |
| `[0,0,1]` | `04` | `04` | Existing side contents remain |
| `[1,0,1]` | `05` | `06` | Forward accepts despite occupied side |
| `[0,1,1]` | `06` | `06` | No pending contents |
| `[1,1,1]` | `07` | `07` | Both receivers block; pending contents remain |

The last row is the important saturation case. Repeating it does not increase
the encoded amount, emit a field, or create a new row. Continuing input would
need an explicit storage or merge law; this passage cannot consume it. A coarse
V could stay 1 while an actual physical amount grows, but that growth is not
demonstrated by this fixture. The bit cannot report how much more arrived or
how long it has waited.

With P present, L absent, and F unobserved, the possible starting value bytes
are `{01,03}`. The possible final bytes are `{02,06}`. Their support masks are
`0A -> 44`: **bit n in a support mask means value pattern n is possible**.
The mask is a prediction, not another material store. `TryStep` refuses this
unresolved input and leaves the actual stored byte unchanged.

The tests check all 256 possible starting support masks against the union of
their resolved cases. They also check conserved occupied stores for all eight
actual states. That verifies whole-handoff bookkeeping at this resolution;
it does not measure hidden physical amounts.

## Two available outlets

For `[P,C,L,R]`, two unconditional rows `[0,1,2]` then `[0,1,3]` pack together as
`24 0D`. Reversing their order packs as `34 09`. The law is unchanged.

| Starting byte | Left first | Right first | Meaning |
| --- | --- | --- | --- |
| `01` | `04` | `08` | One source chooses one empty outlet |
| `05` | `0C` | `0C` | L already occupied; R receives the new contents |
| `0D` | `0D` | `0D` | Both occupied; source retained |

C is preserved and ignored by these unconditional rows. Visiting both rows
does not duplicate the source, because the first successful handoff empties it.
This is explicit priority, not a symmetric split. A symmetric unresolved choice
would preserve candidate routes; an actual split needs a law accounting for
the portions. Merely turning both output bits on would lose that distinction.

## A rectangle with no assigned lengths

The new `OrthogonalPathFixture` is an observation-only geometry contract.
It has four connection arrays. Their positions mean straight travel in
`+u`, `+v`, `-u`, `-v`, where u and v are perpendicular unit directions in one
plane. Absolute orientation is unspecified. This fixed interpretation is the
experiment's geometry rulebook; it is an additional premise, not a deduction
from O, D, blocked flow, or the names of the arrays.

Local references 0 through 3 are corner roles. References 4 and 5 are exposed
reads of two shared nonnegative reaches, A and B. Each currently reads nonzero.
The same reach reference means the same unknown length for this simultaneous
read. Equal bits at different reach references do not mean equal lengths.
The fixture assumes those supplied observations stay stable during the read;
it is not an atomic snapshot service across independently changing structures.

```text
       3 <----- A ----- 2
       |               ^
       B               B
       v               |
       0 ----- A ----> 1
```

Every row is `[start corner, end corner, reach reference]`. References use
three bits. Each directional array starts its own packed stream.

| Directional array | Row | Packed bytes | Meaning |
| --- | --- | --- | --- |
| 0, +u | `[0,1,4]` | `08 01` | Corner 0 to 1, reach A |
| 1, +v | `[1,2,5]` | `51 01` | Corner 1 to 2, reach B |
| 2, -u | `[2,3,4]` | `1A 01` | Corner 2 to 3, the same A |
| 3, -v | `[3,0,5]` | `43 01` | Corner 3 to 0, the same B |

Each array has nine meaningful bits and seven final padding bits. These are
four separate arrays, so their bytes differ from packing four rows consecutively
in a single array. Two constant-context reach values require two V bits. The
corner references carry correspondence, not coordinates or extra magnitudes.
Existing owner-local read mappings and array lengths still provide the context;
these payload bytes are not a self-describing file format.

The connected rows tell us more than four nonzero values could:

* The end of each edge is the actual start reference of the next.
* The opposite edges use the same reach, in opposite directions.
* The table positions impose perpendicular adjacent directions.
* A and B are nonzero in the **reach** interpretation, so neither dimension collapses.

Consequently the boundary is a rectangle of unknown size and aspect ratio.
Nothing here chooses a universal unit or reads either length. Supplying a
different nonzero length later remains consistent with this coarse read.
Sharing A across all four edges would further constrain the boundary to a square.

The same boundary can be assembled around the perimeter, or by attaching the
two opposed A edges and then the two opposed B edges. Tests verify identical
final bytes. This establishes equality of this boundary read, not equality of
the two construction processes or their future capabilities.

The reader deliberately refuses stronger conclusions in these cases:

| Changed structure or observation | Read |
| --- | --- |
| Four separate nonzero reach references | Joined cycle, but opposite lengths are unresolved; no guaranteed rectangle |
| Last edge ends at corner 1 instead of corner 0 | No four-corner cycle |
| Missing or extra edge in a directional array | Outside this narrow four-edge read |
| A or B has true zero reach | Degenerate boundary; no nondegenerate rectangle |
| A may be zero or nonzero | Both degenerate and nondegenerate outcomes remain possible |
| Reach has no admitted observation | No admitted result, not a measured zero |
| An O/D change is offered as a quarter-turn | Rejected: perspective bits are not geometric angle codes |

A zero reach does not delete a corner or its edge row. The potential construction
survives even though the present rectangle read fails. Nonzero *material* at a
joint would not establish nonzero width; that conclusion requires the explicit
reach interpretation used here.

A connected cycle with independent reaches is a closure claim, not proof that
those reaches satisfy it. The conservative reader leaves that case unresolved
instead of silently adjusting unknown lengths until the claim becomes true.

## Other construction families to compare next

These are geometric constraints to encode next, not additional implemented
constructors. They identify what the connections would have to preserve.

| Construction | Required correspondence | What would follow |
| --- | --- | --- |
| Repeat a path twice | Move A, quarter-turn, move B, quarter-turn; reuse A and B on the second pass | The rectangle boundary used above. The second turn inside the repeated block matters. |
| Two pairs of parallels | Each family has common direction; the two families are perpendicular; both separations are nonzero | A nondegenerate rectangle. Without perpendicularity the family is a parallelogram. |
| Four points on a circle | Two distinct diameters of one positive-radius circle; connect their endpoints in cyclic order | A rectangle. Four arbitrary circle points do not suffice. |
| Even quarter spacing on that circle | Adjacent directions are quarter-turns and all radii correspond | A square. |
| Flow away from one point | No selected axis; a radial continuation is permitted | Radial potential. A circular contour additionally needs common-radius correspondence. |

For the circle construction, the two diameters must remain distinct: coincident
diameters collapse the four-point boundary. Again, nonzero radius alone does
not supply every nondegeneracy condition. This is a useful counterexample to
using one participation bit as a universal shape-validity bit.

## What multiplication would mean

Following a path concatenates spans. In a calibrated reading, its total length
adds the traversed lengths, including around a corner. That total differs from
the displacement between its endpoints.

Extending every position of a span through a common transverse interval gives
a product of two spans. With perpendicular directions and suitable coverage,
its calibrated area is length times width. The far boundary is a parallel line;
the region between them is the swept strip. A parallel line by itself does not
establish that its interior has been swept or filled.

The rectangle fixture establishes only the boundary constraints. It does not
create area material by multiplying a bit, and it does not distribute one
source amount across all the points of a line. A future sweep connection needs
to state whether it transfers material, observes a potential, or couples sites,
and must account for any accepted, blocked and remaining participation.

## Retaining material and retaining a route

These are two distinct forms of persistence. A closed passage already retains
pending contents. Retaining a successful connection would preserve a route for
later use, even after those contents have left. Both can exist independently.

A useful next structural experiment would start with an attachment to the
source and an open destination. Local constraints would admit possible
continuations. A compatible receiving surface could complete one of those
connections; an explicit retention rule could keep that ordinary row for reuse.
Without retention, transient participation must still remain represented or
leave through an accounted-for transfer. No route may be silently created from
a repeated failed passage.

Current packed endpoint rows require concrete references. Growth toward a
not-yet-selected receiver therefore needs an explicitly open end, distinct
from an attached site whose value reads zero. Address 0 already names a real
local endpoint; using it as an unknown destination would create a false
connection. Another valid experiment could allocate a fresh local endpoint and
connect to it immediately, without claiming a known geometric position. These
are different cases: an existing endpoint with unresolved location, an absent
endpoint, and a prewired passage that is merely closed.

For a single-site branch, the source attachment is one site. For a whole-line
extension, it must include correspondence along the span and a shared offset
law. That extra relation is what can justify a parallel contour. Uniformity,
choice of side, attraction to another surface, splitting, capture, and the cost
of retaining a route still need explicit laws. Opposite signs alone do not
prove that two structures can merge usefully.

This keeps the proposed learning loop concrete: expose unmet participation,
try a locally admissible connection, observe what it accepts and what remains,
and optionally retain the connection. The current examples establish routing
and some geometric constraints; automatic discovery and hardening remain the
next experimental question.

## Verification

The `--saturation` demonstration completes successfully. All 205 primitive
tests pass, including 26 new cases. Full solution verification with
`dotnet test Gojo.slnx` passes: 5,761 tests across 14 projects, no failures or
skipped tests. The Gojo kernel and existing primitive laws are unchanged.
