# What tension can change while the pen is moving

Consider a pen that has drawn east and is about to draw west directly over its own stroke. Its contact read adds a signed turn request to its local carrier. Before the new mark is committed, a connected actuator can spend one unit to use an available heading. When two diagonal lines cross once, the contact read stays quiet. A junction requires a shared endpoint in either case. These claims can be checked on a two-row tape without an aesthetic evaluator.

An input supplies extents A and B, the pitch of a bay, a clock and a number of vertical slots. At a budget of S slots the legal end of a stroke lies between −S×B and +S×B. The same limit applies under every intervention. A blocked attempt leaves its native event. The owners use the existing path interpreter, gate table, site identity, saved sites, shared peer ports, ordered vector edges and repeat anchors. A coupled stroke may change direction or extent, explicitly join an offered anchor, or remain blocked. A bound target continues to require the exact physical site and extent promised by the row. Other pens in the same beat cannot sense an as-yet-unpublished stroke.

## The connection and its units

The experiment instantiates a star of one to three pens attached to a machine. The machine has its own connection to a frieze owner. Each attachment contains an ordered link in either direction for each of three signed bases: extent, turn, and end binding. A positive extent request asks for a longer reachable step, and a negative request for a shorter one. The sign of turn tension selects a relative steering hand. A positive end-binding request favors the next seam, while a negative one favors closing at the home anchor. Only an eligible end row can act on an end-binding request.

Each owner measures a small signed mismatch and injects its *change* into its own carrier. If the measured need persists after the earlier local charge is used, a reading can reissue a unit. Projection onto a route alone does not spend that charge. On an opportunity, an owner puts at most one signed unit onto one wire. A pen keeps its last local unit for its own actuator. Selection uses the magnitude available and the route's stored strength. Ties rotate. The stock leaves the sender, then the recipient accepts it on the next beat if the passage and recipient have room. Excess and disconnection return the same unit to its source. A target base accepts at most one incoming unit per beat and cannot accept a unit that would bring its magnitude above twelve. Local sensing is separately accounted. Opposed units meeting in a base cancel.

For the unsigned transfer counts, **offered = accepted + blocked + still in flight**. Signed deliveries, their polarities, before/after quantities, local injection and spending are separately inspectable in the timeline. Remainder counts in-flight units at the final sample, and each tier's debt sums the absolute outstanding local carriers. Observed information, including receipt acknowledgments, can be published without multiplying those units. Charging a pen's permissible stroke uses one local action opportunity. This experiment accounts for corrective impulses. It does not impose a common ink or energy reservoir.

## What each perspective senses

| Surface | What is exposed to it | What might happen next |
| --- | --- | --- |
| Pen | Its proposed path's overlap with existing material from earlier beats, the upper and lower slot limit, an excess of turning to one side, and its own accumulated step lengths at the end of a bay. | Steer onto another admissible direction, choose the nearest longer/shorter extent, or leave an unresolved request. An eight-slot direction memory gains one step for an actually drawn clear stroke and loses one for retracing. Equal-strength alternatives retain policy order. |
| Machine | Participating pens expose closure at their own home identity, whether the seam endpoint is the next actual anchor, whether a terminal stop was intentional, and their bay direction word. | A shortfall can request a seam or home binding through one pen port. An unresolvable target stays open. |
| Frieze | One combined word and its reflection from the current bay, plus the machine's seam shortfall and aggregate rise. It compares one earlier bay word. | A tall repetitive band can ask for a change. A missed mirrored echo can ask for opposite steering. It has no access to a pen's list of material sites. |

Pen contact is a finite tactile sample along the candidate stroke. The implementation scans earlier segments to answer the local contact question. Positive-length collinear intersection triggers the overlap read. An endpoint meeting or a transverse crossing does not. Closure in the internal machine read has the narrow contract of returning to the pen's own home site. The external jury can assess more complicated closure, continuity or motif potential without injecting its computed score.

A route can gain or lose strength only after a unit it carried actually reaches an actuator, changes an admitted stroke or binding, and its originating owner takes another read of the same basis. If the mismatch's magnitude falls, the route's preference moves up one position. If it fails to fall it moves down, between zero and four. A relayed unit keeps its return path, so the original owner can receive an acknowledgment through an intermediate machine. Cancellation before use cannot earn credit. Each route retains at most one unconfirmed credit ticket. This is local temporal evidence. The matched fixed-routing control tests whether these updates change later output. Every run starts with fresh route strengths.

## The policy tape

The path bytes retain the named engine. A second eight-byte tape changes which connected mechanisms can act. These bytes travel with the program. A training judge and a reference picture are unnecessary at runtime.

| Byte | Participating positions |
| --- | --- |
| 0 | Bits 0–4 expose overlap, density, turn balance, machine end binding and frieze repetition respectively. Bits 5–7 attach the extent, steering and end-binding actuators. Slot constraints apply independently of this byte. |
| 1 | Bit 0 chooses the initial turning hand. Bits 1–2 choose the first angular offset to try. Other admissible offsets can win on the pen's direction history. |
| 2 | Bit 0 requests a home closure. Bit 1 asks the frieze for a mirrored echo. Bit 2 exempts the explicitly stopped terminal from seam repair. |
| 3 | Bits 0–1 set the turn tolerance. Bits 2–3 adjust the pen's local desired length by a multiple of B. |
| 4 | Positional bits give fixed starting preferences to upward and downward links. Achieved relief or missed credit may revise each link separately. |
| 5 | Bits 0–1 set the actuation threshold from one to four units. Bit 2 shifts priority between the three admitted lanes. |
| 6 | A four-entry native Boolean table reads sufficiently strong tension and the pen clock. When it admits the response, a unit is spent only after a stroke actually settles. |
| 7 | An eight-beat clock and the initial rotation for tie opportunities. |

Quiet keeps sensing, contacts, slots and transfers while disabling pressure-driven acts. Fixed keeps acts and receipts while fixing link strengths. CutUp and CutDown block material offers only in the selected direction. The other exposed observations still exist. Misrouted delivers the same charged unit to the succeeding receiver lane cyclically. These interventions ask different causal questions.

## A portable surface for a series of drawings

The frieze's final public position is one byte. Bits 0–3 indicate horizontal, vertical, rising diagonal and falling diagonal support in the last bay. Bits 4 and 5 mark agreement with the preceding aggregate direction/extent word or its vertical reflection. Both stay off in the first bay. Bit 6 reports a participating, non-exempt pen off its expected seam. Bit 7 reports material closure at a home identity. Direction/extent words use finite rolling signatures and can alias. The byte omits distances, endpoint IDs, exact positions and private sites. Stretching a line or moving a junction can leave the byte unchanged. A subsequent owner can inspect that public byte and return a bounded pulse through the frieze input without reaching into the drawing.

The runtime fixtures include a source at the frieze and a pen with eight eastward strokes. An injected +3 extent request reaches the pen and lengthens a stroke. Reversing the sign shortens it, and cutting the downward passage keeps the original extent. The fixed and learned route controls can produce different later strokes on the same hook foundation. Those fixtures prove the wiring can matter. The held-out competition measures when it actually helps.
