# How the path machine draws

A repeat gives each owner an ordered body of rows. A row takes one opportunity to move a pen from a bound site, in one of eight directions, along a shared extent. The receiver sees two things its neighbor publishes: the participation of its last draw, and the endpoint that may be attached. Nothing in a candidate is a pixel, an SVG command, a desired picture, a grade or a motif name.

## From bytes to an ordered segment

`(current, saved, anchor or incoming site) -> (chosen endpoint)` is the physical row. If the gate opens and the endpoint binding can settle, the row creates one ordered edge. Its stored endpoints are identity-bearing indices into the trace's site table. The projection into a drawing gives a site an X and a Y. Two sites at the same projected point are still two distinct sites unless an actual binding joins them.

| Position | Stored bits | Effect |
| --- | --- | --- |
| Program start | version byte 1, owner count | One to three independently addressed owners. |
| Each owner | row count, frame byte, clock byte, control byte | One to eight rows executed once per bay. All owners sample the preceding beat's exposed ports before updating publications together. |
| Frame | bits 0–2 select an origin lane; 3 allows attachment at the next anchor; 4 reflects vertical; 5 reflects horizontal | The lane slots read 0, B, −B, 2B, −2B, A, −A, A+B. The reflection and seam bits change the geometry accessible to the same rows. |
| Clock | Eight ordered participation bits | Select the bit at `(beat + phase) mod 8`; each beat moves to the next slot. |
| Control | bits 0–1 choose the previous owner, next owner, self, or known zero; bits 2–5 select a truth table | This table can write back the owner's vertical-frame bit. At the end, `FinalProgram` holds the resulting bytes. |
| Row byte 1 | bits 0–2 direction; 3–4 extent reference; 5–6 source reference; 7 pen participation | Directions are east, northeast, north, northwest, west, southwest, south, southeast in an upward-positive frame. Extent slots read A, B, A+B or the repeat pitch. Source slots read cursor, anchor, saved site or incoming port. |
| Row byte 2 | bits 0–3 truth; 4–5 target reference; 6 save endpoint; 7 stop after admitted request | Target slots create a fresh site, require the saved site, require the incoming site, or require the next structural anchor. |

The low four truth bits address entries by `clock + 2 × peer`; the executable lookup is compiled from Gojo's `BitRelation` over the positive-frame zero/present words. A constant law omits its unused incoming binding. Dependent laws distinguish a disconnected pulse from an ambiguous one. A blocked destination retains an event even if the permission read was fully resolved. A drawing opportunity is consumed within this owner's row. Observing the neighboring pulse or endpoint does not consume its material.

The physical calibration here is a two-axis construction with directional slots. A diagonal executes the same selected extent along both axes. Its Euclidean projection is longer than a horizontal step bound to that extent. The external experiment supplies positive A, B and pitch to realize a sketch. The program contains references to extents, never coordinate literals. Some endpoint requests succeed only under particular calibrations.

## Which connections exist

An owner's start is the anchor in its own lane at the current bay of the repeating carrier. Every bay is executed. An ordinary higher-numbered tile in a rendering is therefore backed by a new sequence of decisions. A seam-enabled owner binds a matching endpoint to its actual next anchor. The next body can then continue from that site. With the seam binding absent, an equal position gives a different identity.

A source bound to a saved or published site can send a new stroke out of a previously visited joint. An explicit target admits a merge only when the promised directional extent lands exactly on that bound site. The exported trace records borrowed-source and bound-target strokes alongside each site's ordered incidences. A branch event describes opening from a site other than the current cursor. Its resulting topological degree can still be two, or one when an isolated origin opens. A merge event can attach to a future repeat anchor before any following stroke has used it. Geometry-only crossings and overlaps remain possible outcomes.

A closed gate skips a row. A stop ends this owner's body in the current bay when admitted. Each owner's exposed location and activity refer to the preceding beat, so either owner can read the other without relying on host evaluation order. The seed pulse (zero, present or unresolved) and clock phase supply initial conditions. Protected glyphs are unnecessary.

`Cut` withdraws the foreign publication and site binding. `Frozen` keeps the initial frame against local writes. `Open` forces the stroke gate while preserving site and extent requirements. A switch is a bounded form of self-modification: a local two-input law may invert one bit in its executable frame for subsequent strokes. This does not add a new row, name a subroutine, or redesign the global evaluator. The resulting program can be used as the initial frame of a later run without its training apparatus.

## What search receives

Six compact construction priors supply stepped meanders, chevrons, diamond pairs, spurs, hooks and running frets. Their full definitions are stroke rows in `PathSearchGrammar.cs`. These authored bodies can be mutated or crossed. The kernel can also sample a free grammar with no initial foundation. A change may alter an endpoint, switch, clock, direction, gate, common extent binding or owner count. Thus a machine resembling a fret can inherit that possibility; the observed benefit must be tested against matched search, new extent inputs, and disconnected or frozen execution.
