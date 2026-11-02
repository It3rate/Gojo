# A fork with two owners

Give a junction one pulse. Its sibling advertises two exits. On beat 1 both
are free and the junction places the pulse in its left offer site. On beat 2
a separately supplied obstacle occupies the left gate. The old report still
explains the choice. The actual handoff now encounters an occupied gate.
Follow the pulse and its reply in [layer 22](https://gojo.inklingai.ca/encoding/explorer/?layer=22).

## What makes the decisions

~~~text
exit gate + inbox ── vacancy read ──→ junction guard
waiting ── controlled passage ──→ left or right offer
offer ── port seam, gated by current receiver state ──→ inbox
inbox ── local passage ──→ end ── public presence read ──→ reply latch
seam before/after ── event read ──→ optional refusal meter
~~~

The two material owners have private packed state. The junction also has a
signal circuit that remembers an observed arrival. The author supplies the
wiring and the two Boolean read laws. Each law compiles to an inspectable
64-byte table of admitted input/input/output words.

* Each exit advertises availability when its gate and inbox are both empty.
* The left offer guard reads left availability. The right offer guard reads
  right availability together with left unavailability. This connection
  supplies the initial left preference when both exits are free.
* A single occupied waiting site feeds the two guarded passages. Moving into
  an offer drains waiting. This physically latches the choice. There is no
  return connection from the offer to waiting in this fixture.
* The connector pulses its packed seam rows in stored order. Each row names
  one exported material outlet and one exported intake. The intake binds the
  receiver's gate and inbox. While holding both owners, the passage reads
  source, gate and inbox, consults its 8-byte transition bitmap, then commits
  the two material bits together. Each row settles independently.
* The receiver moves an accepted packet from its inbox to its end on the
  next delivery opportunity. Presence at either end can advance a separate
  reply circuit from waiting to seen. Removing the return wire leaves a
  delivered pulse and an unanswered sender.

The material export differs from an ordinary read connection. Only an
explicitly constructed outlet/intake pair can do the joint handoff. Both
sites must still have compatible constant O/D and one V bit. A conflicting
receiver lock, incompatible migration or nonunique transition defers the
attempt with both sites unchanged. The experiment uses a single-threaded
clock. With competing rows at one inbox, row order gives the first row the
first opportunity and the occupied target holds the subsequent source.

## Clock and environmental input

Every beat runs the same delivery, seam, offer and reply phases, in that order.
Both lanes get an opportunity on every beat. The environment may move a
separate blocker from reserve to the left gate or back using an ordinary
local passage before those phases. The input tape fixes those beat numbers
before the run. The recorder supplies no clock decisions or retries.

The report read and the later intake check deliberately occur at different
times. The waiting pulse can move into an offer at the end of beat 1, after
the seam opportunity has already passed. The receiver checks its live state
on beat 2. If left stays blocked, the offer remains there even while right is
free. This exposes a limitation of the authored policy. Adaptive recovery or
new structure would need an actual return signal, local state and a connected
change law inside the system.

## Bits and connections

Site widths are one V bit with fixed positive O/D. These are whole occupancy
states. The input pulse has an unspecified magnitude.

| Owner | Bit positions, low first |
| --- | --- |
| Junction | waiting, offer L, offer R, sampled left guard, sampled right guard |
| Exit owner | busy L, busy R, inbox L, inbox R, end L, end R, blocker reserve, fixed clock |
| Reply circuit | waiting for arrival, seen, sampled arrival guard |
| Optional refusal meter | zero, one, two or more, sampled refusal guard |

| Packed table | Ordered rows | Hex bytes |
| --- | --- | --- |
| Vacancy read, 3 bits per local reference | [0,2,1] [4,6,3] | `50 E8 01` |
| Fallback read, same reference width | [1,3,5] | `59 01` |
| Junction passage, source/guard/target | [0,3,1] [0,4,2] | `58 40 01` |
| Seam rows, 1 bit per interface position | [0,0] [1,1] | `0C` |
| Junction passage law, control present | whole before/after membership | `01 02 04 00 10 20 48 80` |
| Seam law, busy control absent | whole before/after membership | `01 00 04 08 12 20 40 80` |

The calculated vacancy endpoints are 1 and 3. Endpoint 5 reads the fallback
condition. The junction imports these observations as guards. Each material
outlet maps to junction port 1 or 2. Intake 0 binds receiver ports [0,2],
and intake 1 binds [1,3]. Those public interface bindings are host handles
in this prototype. The one-byte seam table selects among them.

## A stale report, a refusal, an arrival

The default tape closes left on beat 2 and releases it on beat 4.

| Beat | Junction | Exit owner | Reply | Refusal meter | Physical result |
| ---: | --- | --- | --- | --- | --- |
| 0 | 01 | C0 | 01 | 0 | Waiting holds the pulse |
| 1 | 0A | C0 | 01 | 0 | Left offer holds it |
| 2 | 0A | 81 | 01 | 1 | Seam reads state 3 and holds state 3 |
| 3 | 0A | 81 | 01 | 2+ | A second refused attempt |
| 4 | 08 | C4 | 01 | 2+ | Seam moves state 1 to 4. Left inbox now holds the pulse |
| 5 | 08 | D0 | 06 | 2+ | Left end holds the pulse. The reply latch changes |

Here the seam state number is S + 2C + 4T. Only one of waiting, the two
offers, the two inboxes and the two ends is occupied at any snapshot. The
blocker moves separately. A read of its position consumes none of it.

## Separating an instrument from a controller

The small refusal meter has its own three state sites. Once per measured beat
it reads the before/after pair from each connected seam. An eight-byte mask
`00 02 00 08 00 20 00 80` admits the four diagonal transitions that held a
present source. A presence read combines the results. Two guarded passages
advance the meter from zero to one and then to two or more. There is no
outgoing wire from the meter into the material owners. With a single input
pulse, at most one connected seam carries an offer per beat.

The exact counts beneath the diagram are calculated afterward from detached
frame copies. Offers count resolved seam attempts with a present source.
Refusals count those that left it held. Arrivals count a newly occupied end.
Multiple attempts on one pulse can therefore produce several refusals and a
single arrival. A disconnected offer can stay held without producing any
seam event. A deferred, unreadable transaction supplies no event sample.

| Five beats of input | Tested offers | Refusals | Acceptances | Arrivals |
| --- | ---: | ---: | ---: | ---: |
| Close left on 2, release on 4 | 3 | 2 | 1 | 1 |
| Close left on 2 and hold it | 4 | 4 | 0 | 0 |
| Left busy initially, right free | 1 | 0 | 1 | 1 |
| Both busy throughout | 0 | 0 | 0 | 0 |

Removing the left report wire leaves its local guard at its initialized
zero. Removing the left material seam leaves a stranded left offer and an
instrument reading of zero. Removing the arrival reply leaves material at
the end while the reply stays waiting. Independent replays with the meter
unplugged and no intermediate frame capture produce identical junction, exit
and reply bytes at every beat.

These results exercise a fixed, authored coordination rule. A later
experiment could wire refusal evidence into a local change mechanism and
measure its utility afterward against the same tapes. Here neither the meter
nor an external interpretation of failures or fragments alters a route.

~~~powershell
dotnet run --project experiments/PrimitiveMeasure -c Release -- --fork
dotnet test Tests/PrimitiveMeasure.Tests -c Release --filter FullyQualifiedName~ForkFixtureTests
~~~
