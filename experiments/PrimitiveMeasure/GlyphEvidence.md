# Glyph clues and junctions

Take a **b**. Keep its bowl on the right and hide whether the straight tail rises or drops. Both **b** and **p** fit. Now take a **Y**. Keep every stroke in place and change which arm introduces flow. The same joint can collect two approaches into the stem or let one arrival choose a branch. [Open layer 29](https://gojo.inklingai.ca/encoding/explorer/?layer=29)

## What the reader can use

The straight atlas already exposes ordered spans, joints, shared ink sites, projected crossings, connected components, independent loops with walk witnesses, and travel in a local upright frame. It records a separate writing order with lifts and reversed uses. The curve experiments add departure and arrival headings, turning during a span, corners between spans, radial sources and closure goals. A retained return across a C gap and the change in reach of a pushed D are further questions one can ask particular owners. These observations have different scopes. A closed walk alone does not establish the filled interior, the pen's route, or the identity of a letter. [Straight capitals](StraightCapitals.md) · [Curves in capitals](CurvedCapitals.md) · [Across a glyph gap](GlyphCompletion.md) · [Push a bowl outward](BowlReach.md)

This reader asks five questions at once. How many independent loops and free ends are there? Does the single free tail turn during its end span? Is the marked side of the bowl to the right of the tail joint? Is the free tip above that joint? A dotted or disconnected mark, for example, would need additional observations. The scope here is eleven supplied forms in one upright hand.

| Sample | Loops | Free ends | Tail at the free end | Bowl relative to joint | Tail tip relative to joint |
|---|---:|---:|---|---|---|
| b | 1 | 1 | straight | right | above |
| d | 1 | 1 | straight | left | above |
| p | 1 | 1 | straight | right | below |
| q | 1 | 1 | straight | left | below |
| 6 | 1 | 1 | curved | right | above |
| 9 | 1 | 1 | curved | left | below |
| o, 0, O | 1 | 0 | — | — | — |
| 8 | 2 | 0 | — | — | — |
| c | 0 | 2 | — | — | — |

For **b**, the ink spans are `[1,0]` up the shared straight side, `[0,3]` and `[3,1]` around the bowl, and `[0,2]` up to the free tip. There is a closed walk through the first three. Only site 2 occurs once among the span ends. Ordered horizontal sites `[0,3]` put the marked bowl site to the right of the joint. Ordered vertical sites `[0,2]` put the free tip above it. For **p** the tail attaches at site 1, and its vertical order is `[2,1]`. These are order comparisons without distances. A renderer chooses one spacing for a proposed completion.

### Try withdrawing evidence

Select **b**, change **Evidence**, then use **Try completion** to draw each admitted construction. The left nodes are observations and the drawing is a proposal. Changing the proposal never contributes new evidence to the left.

| Evidence withdrawn from b | Surviving labels in this hand |
|---|---|
| None | b |
| Vertical extension | b, p |
| Bowl side | b, d |
| Both axis reads | b, d, p, q |
| Whether the tail bends | b, 6 |
| Closure and vertical extension | b, p |
| All except closure and free ends | b, d, p, q, 6, 9 |
| All except closure | b, d, p, q, 6, 9, o, 0, O |
| All five | All eleven |

Choose **Hide loop & extension**. The reader sees one free end, a straight tail and a right-hand bowl landmark. Both surviving constructions **predict one loop**, even though the loop count was withheld. They disagree about whether to extend the tail up or down. The loop prediction appears underneath the drawing and its observation stays unreported. In the round family the trial deliberately supplies one radial construction for o, zero and capital O. Reading all five clues still leaves all three names. A shared writing line, relative size, a slashed variant, or surrounding characters would supply a different basis. Size alone would still leave identical forms possible.

### A plausible completion that fails

Select **Two bowls + tail** under **Observation**. This held-out construction adds a second curved return on the opposite side of the same stem. It still has one straight free tail and the same marked site on the right. Hide the loop count and extension. The reader again proposes b and p, both with one loop. Restore all five clues. The observed count is two and **no catalogue entry fits**.

There are two different strengths of prediction here. A complete nonempty finite ink graph with exactly one free end must contain at least one cycle. Every finite tree with an edge has at least two leaves. The stronger claim of *exactly* one loop came from the available examples and failed on the extra return. Seeing one tip through a crop would not establish the complete free-end count used in this argument.

## A route through a joint

The three local arm ports are **left, right, stem**. Each stores a reference to an ink run, a span slot on that run, and a reversal bit for travel **toward** the joint. An arrival selects a start site and an oriented approach. An output follows its bound span away from the joint. A whole glyph can admit several starts. Writing it with one pen would also require choosing the order of those starts and any lifts.

| Incidence | Left inlet | Right inlet | Stem inlet | Packed arm bindings |
|---|---|---|---|---|
| Y, three run ends meet | `[0,0,0]` | `[1,0,0]` | `[2,0,1]` | `40 20 01` |
| T, the bar continues through the joint | `[0,0,0]` | `[0,1,1]` | `[1,0,1]` | `00 15 01` |

There are two bits per field, low bit first. Actual transitions use the existing one-bit passage store. Its four sites in order are the left arm, right arm, stem and gate. The examples below use hex for whole store bytes. When both inlets need the same empty slot, the first passage row gets the first opportunity.

| Reading of the joint | Passage rows [source, gate, destination] | Actual trace |
|---|---|---|
| Y as merge | Open `[0,3,2]`, then open `[1,3,2]` | Left alone `01 → 04`; right alone `02 → 04`. Both offered `03 → 06` leaves the second waiting |
| Y as branch | If clear `[2,3,0]`, if set `[2,3,1]` | From the stem, clear `04 → 01` or set `0C → 0A` |
| T as two corners | Open `[0,3,2]`, then open `[1,3,2]` | Arrival from either end of the bar turns down the stem |
| T as blockage from below | Open `[0,3,1]` | From the stem `04 → 04` stays held. From the left `01 → 02` passes across |
| T as an optional turn | If clear `[0,3,1]`, if set `[0,3,2]` | From the left, clear `01 → 02` travels across or set `09 → 0C` turns down |

Withhold the gate on the optional T. The reader predicts the right arm **or** the stem. Those two possible futures stay separate. Set the gate to run one actual trace. At this resolution an arriving pulse occupies an arm slot. The illustration draws its oriented use of an existing span. It does not integrate motion along a stroke or automatically color every branch.

The merge-Y and corner-T share the very same passage rows. The T's two upper arms belong to successive slots of one straight run. The Y's belong to two different diagonal runs. One reader can therefore see the same collection behavior and another can see different shapes. This is a reusable example of a concept above the individual mark.

## Which constructions would test the limits next?

The shared-line i/j and the open/closed 4 now have executable trials. [Try the new constructions](WritingLine.md)

* **i and j** would test attaching a separate dot to a stem in the writing frame without creating an ink bridge. A shared baseline and a mid-height landmark would let two glyphs compare ascenders and descenders across their separate local origins.
* **a, g, 4 and 7** would test multiple hands per label. A single-storey and double-storey construction or an open and closed 4 can have different loop counts. A crossed and uncrossed 7 can have different segment evidence.
* An unmarked gap in a handwritten bowl would test choosing between an observed gap, a possible closing stroke and a deliberately open letter. The experiment offers complete comparison forms. It does not extract missing spans from a photograph, learn an alphabet or prove which hand produced a mark.
