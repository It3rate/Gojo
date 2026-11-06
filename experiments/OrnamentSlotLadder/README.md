# Drawing from an extra slot

Imagine giving a spiral pen a wider strip. If its body already fits, nothing makes it draw anything more. Here a pen can *read* the available height at a local connection. An extra slot opens another construction engine. We compared that mechanism with simply letting all the pens attempt their strokes and stopping any stroke that crosses the physical boundary. [See all three compounds at three widths](../../artifacts/local-tension-ornament/slot-ladder/Gallery.html).

The compounds are named, editable, manufactured examples. The key has two streams that can attach limbs to the running spine's published endpoints. The spiral band keeps a straight through-rule and runs a pair of discrete motifs, each ending inside its own coil. The crest band keeps a straight through-rule and uses actual writable vertical frames. A known clock phase causes a crest body to invert on the following bay. An additional pen in the opposite initial frame can fill out the complementary half. The three programs use the same ordinary one-to-three-pen, eight-rows-per-owner grammar as the tension study. No learned finalist or post-hoc human ranking selected these constructions.

## What is encoded

The byte stream begins with version 1 and three owner-local reference bytes. The low nibble chooses one position in a unary height stream; the high nibble is a one-input Boolean law. Here the local laws pass the selected input through. The three chosen positions are the first, fourth and eighth height slots. The remaining bytes are the ordinary packed Gojo path program. At each opportunity, the offer's gate reads its own address. A separate physical capacity check admits an endpoint only within ±slots×B. The same input path bytes and extents are used throughout each narrow/mid/wide comparison.

All six A/B/Pitch triples were checked against the forty normalized physical bases of the preceding tension tournament. They were newly enrolled before any drawing. Pitch equals A+2B. The choice is constructive: all three through-spines can close their seam at that pitch. Different periods may destroy that relation. The key's upper and lower limbs are attached through the actual prior-beat stem port; the spiral pen remains a separate participant. A geometrically symmetric pair of spirals need not share material.

## What happened

Across 216 native runs all three ladder machines drew with one active owner at two slots, two owners at five, and three owners at nine. Their spines continued through five native anchor handoffs in six actually executed bays. At intermediate width, the capacity-only control already admitted third-owner ink in *all* 18 engine/basis combinations. This separates a deliberately open higher rung from a pen simply having enough room to draw.

| Ladder engine | Optional strokes, 2 / 5 / 9 slots | Symmetry and joints |
| --- | --- | --- |
| Key with branches | 0 / 18 / 36 | Real directed fork sites rose from 0 to 6 to 12. Whole-motif mirror agreement fell from .107 to .076 to .059. More available space produced more branching, without strengthening that symmetry. |
| Opposed inward spirals | 0 / 42 / 84 | At five slots the one-sided band had .115 mirror agreement. At nine slots the reflected upper and lower coils achieved 1.000. Every wide drawing had twelve explicit interior terminals. |
| Alternating mirrored crests | 0 / 30 / 60 | At five slots, average within-bay mirror agreement was .233, whereas a reflected neighbor matched exactly (1.000); simple translation gave .233. At nine slots, both within-bay reflection and adjacent-repeat reflection reached 1.000. |

All 18 wide engine/basis combinations avoided positive-length collinear overdraw. The always-on arm withdrew the physical width boundary and published a positive bit to all ports; it is a counterfactual ceiling. Its plots are invariant across nominal widths. The spine-only plots were also invariant. These saturation and withdrawal controls are deliberately incomparable to a claim that an organism learned how to exploit its width.

The published [protocol](Protocol.md), [individual outcomes](../../artifacts/local-tension-ornament/slot-ladder/cases.json) and [verdicts](../../artifacts/local-tension-ornament/slot-ladder/verdicts.json) separate intended effects from measured behavior. A fresh process reproduced the exact aggregate, exhibit and verdict bytes. The gallery shows all four connection modes; the [contact sheet](../../artifacts/local-tension-ornament/slot-ladder/Compare.png) shows the ladder alone.

~~~powershell
dotnet build experiments/OrnamentSlotLadder -c Release
$runner='experiments/OrnamentSlotLadder/bin/Release/net10.0/OrnamentSlotLadder.dll'
dotnet $runner freeze NEW_RUN
dotnet $runner run NEW_RUN
python experiments/OrnamentSlotLadder/Gallery.py NEW_RUN
~~~

Freezing uses the previous cohort manifest from the recorded tension campaign. Rebuilding the illustration requires Pillow. Opening the gallery requires a browser.
