# Two reaches, one choice

Slide two strips out from the same edge. Each tip can stop at **Near** or **Far**. Suppose exactly one goes to Far. Cover the tips. You can still mark where the overlap ends. Both strips cover Near, and only one reaches Far. Which strip is longer stays open. [Try layer 31](https://gojo.inklingai.ca/encoding/explorer/?layer=31)

~~~text
                   Origin       Near               Far
Possibility 1      |============|==================|   left
                   |============|                      right

Possibility 2      |============|                      left
                   |============|==================|   right
~~~

The spaces on the page are illustrative. The gauge supplies a common outward rail and two occupied spans that run from its first stop through their respective tips. Near and Far name distinct ordered stops. The experiment supplies no distance, unit or spacing.

## What crosses the boundary

A public read keeps one bit for each **complete possibility**. In low-bit order these are *neither, left only, right only, both*. The mask `06` admits the two middle cases. It takes four bits to describe the alternatives for these two V roles. Each element still has its original three-bit O/D/V encoding.

| Set the experiment to | Public answer at Far | Can the neighbor say exactly one reaches Far? | Where can the overlap end? |
| --- | --- | --- | --- |
| Exactly one | Left only or right only · `06` | Yes | Near |
| Independent | Any of the four pairs · `0F` | Open | Near or Far |
| Together | Neither or both · `09` | No | Near or Far. The two tips agree in this gauge. |
| Exactly one, left observed off | Right only · `04` | Yes | Near. The right strip is longer. |
| Attached left observer supplies no reading | No supported pair · `00` | No reading | No reading |

*Independent* allows all combinations. It supplies no probabilities. With any of the first three settings an observer asking only about the left tip gets “maybe.” The right tip gives the same individual answer. Splitting the joint read into two such answers loses precisely what distinguishes these constructions.

## Encoding chart

| What is connected | Stored or calculated bits | How to read them |
| --- | --- | --- |
| Outward rail | Site rows `[2] [0] [3] [1]` → `72` | Origin, Near, Far, Beyond in physical storage order. The site addresses themselves have no length ordering. |
| Left and right tip dials | Rows `[0,3] [0,3]` → `CC` | Each dial selects its resting or extended stop. Both spans start at the rail origin. |
| Pair of dial settings | Allowed mask `06` | The low V bit belongs to the left role, the next to the right. Setting pair index `1` or `2` is admitted. |
| Read at one stop | Four two-bit output fields | Each field says which strips reach that stop under one complete setting. At Near all fields say both (`FF`). At Far the map preserves the input pair (`E4`). Beyond both tips all fields say neither (`00`). |
| Compare tips | Four fields → `E7` | The two output questions are “left reaches at least as far as right” and its converse. Both true means equal projected reach. |
| Neighbor asks “exactly one?” | Truth mask `06` | Test the entire public set against the question. When every admitted pair answers yes the output support is `02`, the ordinary resolved positive presence word. |

A zero **map** at Beyond gives one supported answer, *neither*, when a valid setting exists. A zero **support mask** admits no answer. A missing observation, an incompatible O/D observation, or evidence contradicted by the authored setting law can each produce that coarse result. Their source bindings and premises differ. An unwired observation imposes no extra constraint. A probe from a different rail requires an explicit correspondence before its address means anything here.

## One loose dot

Switch the construction to **One loose dot**. Rows `[4,0] [4,2]` (bytes `04 05`, three bits per site) connect the same mark to two candidate stem heads. The two participation roles follow row order. With mask `06` exactly one link is active. A neighbor can act on “the mark belongs to one head” while either individual owner remains plausible. Witnessing that the left link is absent settles the right. Reversing the query to look for a link from a head back to the mark gives a different ordered relation. This is an authored membership experiment with separate ink components.

## Can the read do work?

Choose **Far** and **Try handoff**. A peer connects the published relation to a guard on its own waiting pulse. Exactly-one support opens the passage to an offer. A second owner receives that whole pulse through its inbox. Reading the gauge again leaves the same possibilities, and a second request cannot deliver the pulse twice. Switch to independent settings and the unresolved guard leaves the pulse waiting. The tests also rewire the guard to ask “left reaches Far?” and exercise that same hold. [Material handoff and held samples](WritingLine.md#reading-remembering-and-drawing-from-the-stem)

A separate test moves two independent local dials through their own passages. A single live comparison port reports *left farther*, then *right farther*, then *equal projected reach*. Each change follows from where a tip lands on the rail. No comparison of uncalibrated V magnitudes occurs.

This bounded construction needs two dial settings per role and a supplied ordered rail. Arbitrary origins, partly ordered rails and discovery of the exactly-one law remain open. To run the behavior checks use `dotnet test Tests/PrimitiveMeasure.Tests --filter FullyQualifiedName~PairedReachTests`.
