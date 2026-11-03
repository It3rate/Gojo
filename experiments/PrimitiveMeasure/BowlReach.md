# Push a bowl outward

Pin the top and bottom of a D's stem. Imagine a short stick extending at right angles from the middle of the stem to the farthest point of the bow. Push that tip farther out. Which constructions allow the stick to lengthen while the stem stays put? [Try layer 28](https://gojo.inklingai.ca/encoding/explorer/?layer=28)

## Try one push

* **D · overlay.** At rest the solid and dashed outlines coincide. Click **Push outward**. The solid bow widens. The dashed bow is half a circle whose diameter is the pinned stem. Its radius already fixes its reach, so the request waits at its intake. Switch **Inspect encoding** to see where the request settled in either owner. **Reset** restores both.
* **B · compare.** Each B starts with the same drawing, with a wider lower bowl. Push the upper tip. On the left only the upper bowl widens. On the right both widen. The right-hand bowls share the change of setting while keeping their own starting widths. The dotted outlines show where the tips started.
* **D · ends only.** The two perpendicular end headings and the stem are available. Try the same push. There is a waiting request, but no published tip to compare before and after. “We saw the tip stay” and “we couldn't see a tip” produce different reads.

## What connects to what

Each owner supplies one request in site 0. Its first passage moves that request into waiting site 1. Its second passage reads a guard. An empty guard passes the request into the selected dial. An occupied guard leaves it waiting. A tip row reads its dial and publishes either the resting or the farther landmark. Two tip rows can read one dial without consuming its contents twice.

The owner assigns local site 2 to the open guard, 3 to the pin guard and 4 to the half-span dial. The stem pin connects a straight stem span, site 4 and guard 3. Independent dials follow at sites 5 and 6. The offer row is always `[0,2,1]`. All addresses below occupy three bits per field. In a tip row the fields are **ink span, dial, resting tip, farther tip**. In a width passage they are **waiting site, guard, destination**.

| Owner | Tip rows | Width passage | Owner byte before → after | Observed tips |
|---|---|---|---|---|
| Adjustable D | `[1,5,3,1]` | `[1,2,5]` | `09 → 28` | Upper moves out |
| Radial D | `[1,4,3,1]` | `[1,3,4]` | `09 → 0A` | Upper stays |
| D ends only | None | None | `09 → 0A` | Upper unreadable |
| Independent B | `[2,5,3,1]`, `[3,6,4,2]` | `[1,2,5]` | `09 → 28` | Upper moves alone |
| Linked B | `[2,5,3,1]`, `[3,5,4,2]` | `[1,2,5]` | `09 → 28` | Both move |

Read the bytes in hex, with site 0 in the low bit. For example, `28` has sites 3 and 5 occupied. The two B owners have the same live byte after the push. The lower tip's connection to dial 6 or dial 5 explains the different results.

The outward ray stores the landmarks in physical order `[0,3,1,4,2]`. Starting at the stem, these mean *stem, upper rest, upper farther, lower rest, lower farther*. Thus a tip moving from landmark 3 to landmark 1 moved outward. No subtraction of landmark names or measurement of the distance is involved. The drawing chooses spacings for illustration. Here the B bowls have equal heights and the lower one has greater reach. This particular construction says nothing about their relative areas in another hand.

## What a second structure can tell

The motion reader sees the published tips before and after. Its initial possibilities come from running four supplied examples through the same push. It combines the outcomes of examples compatible with the visible starting tips. This is a small test of what an intervention reveals, with its repertoire supplied in advance.

| Visible sample | Before pushing | After pushing |
|---|---|---|
| Either complete D | `03` stay or widen | `02` widens, or `01` stays |
| D ends only | `03` stay or widen | `03` still unresolved |
| Either B | `0A` upper alone or both | `02` upper alone, or `08` both |

The movement mask gives one bit to each joint outcome. From the low end they are *neither*, *upper alone*, *lower alone*, *both*. On this particular push the two complete D outlines and the two B drawings become distinguishable. Observing two tips move together once leaves open whether they would move together under another intervention.

## Where momentum could enter

The perpendicular from the stem to the tip measures the bow's **reach**. A straight initial run along the departure heading, a rate of turning during travel, and a delay before turning could each affect that reach. They would make different predictions about the ink between the endpoints. The present trial takes the outward request as given and encodes only the ordering of tip positions. A later driven pen could generate such a request, then use these same tips to check its consequence.

Run the same trial from the repository with `dotnet run --project experiments/PrimitiveMeasure -c Release -- --bowl-reach`. For the earlier test of what the two end headings alone allow, see [Across a glyph gap](GlyphCompletion.md).
