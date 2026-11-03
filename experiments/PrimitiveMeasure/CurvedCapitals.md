# Curves in capitals

Start with D. Switch **Probe evidence** to **Ends + ink topology**, then step
right to **D · corners**. Both returns leave the stem going right and arrive
going left. Restore the interior law to see where the turn happens. Next try
**C · gap in O** and switch **Drawing** to **Ink + construction**. The thin
dashed arc completes the source circle while the ink still has two ends.
[Open the experiment](https://gojo.inklingai.ca/encoding/explorer/?layer=26)

## How to build each letter

- **D** Draw the upright. From its top, move right and keep turning clockwise as you advance until you arrive at its foot heading left.
- **D · corners** Keep the same upright. From its top go right, turn down, then turn left to meet its foot. Both joins to the stem are still right angles.
- **D · half of O** Divide a radial circle into two equal arcs. Ink its right half and join the two cuts with a straight stem. The other half remains available as construction.
- **D · ends only** Join the return to the top and foot of the upright. It must leave heading right and arrive heading left. The interior is unspecified.
- **C** Begin at the upper lip. Travel up-left and turn around the left side, arriving at the lower lip heading up-right. Leave the two tips apart.
- **C · gap in O** Cut a circle at two sites on its right. Ink the long way between them. Keep the remaining arc in the source so the whole O is still readable.
- **C · closure sought** Write the open C. Ask its last span to arrive at the first site. Its actual arrival remains at the other lip, leaving an unmet request.
- **O** Start with a centre and one participating reach in every direction of the plane. Take the boundary once around to its starting site.
- **B** Draw an upright with an interior joint. Curve from the top to that joint on the right. Curve out again from the joint to the foot, making a second bowl.
- **G** Draw an open C. From its lower lip go straight up, then turn left to make the inward bar. Leave the upper lip free.
- **J** Bring the stem straight down. Sweep round to the left at the bottom and finish facing up.
- **P** Draw an upright. From its top bow out to the right and return partway down the upright. Leave the lower part of the stem free.
- **Q** Make the radial O. From a shared site on its lower-right boundary, extend a diagonal tail down-right.
- **R** Build P. From the joint where its bowl returns to the stem, add a straight leg down-right.
- **S** From the upper right, turn around the upper bowl. Pass through the waist heading down-right and change the sense of turn to make the lower bowl. Leave both ends free.
- **U** Draw down on the left. Keep advancing as you turn around the bottom. Finish straight up on the right.
- **Straight · closure sought** Travel right on one straight stroke and request a return to the starting site. The endpoint stays distinct. The closure request alone supplies no bend.

Together with the [straight capital atlas](StraightCapitals.md), the examples
cover the 26 uppercase Latin letters in one selected block hand. The Q uses
a tail joined to the rim. A stroke crossing the rim from inside would be
another construction. The grid selects authored inputs. The comparisons below
work on their encodings without consulting their names or their pictures.

## What the two readers can see

The coarse reader sees the number of independent ink loops and the departure
and arrival headings of a selected continuous stretch of ink (the **probe**).
The finer reader can also inspect how many joins inside the probe change
heading and whether steering participates during travel. The full numeric
panel remains available to the person inspecting either read. A third setting,
**Include source and goal**, also compares the recorded radial part count,
rung presence and closure request outcome in these fully supplied examples.
It separates C made by a gap from C with closure sought. Written C still
fits G's open sweep because this probe never asks about G's extra spur.

| Construction | Ink loops | Probe ends | Turning inside a span | Evidence still available |
| --- | ---: | --- | --- | --- |
| Rounded D | 1 | right → left | present | One clockwise drive on the return |
| Cornered D | 1 | right → left | absent | Two changes of heading at joins inside the return |
| Half-circle D | 1 | right → left | present | Two equal shares of one whole and a retained other half |
| D with only its end conditions | 1 | right → left | unresolved | Two right-angle attachments to the same straight stem |
| Written C | 0 | up-left → up-right | present | Open turn with two distinct tips |
| C from O | 0 | up-left → up-right | present | Closed source, open ink, and correspondence between the two reads |
| C with closure sought | 0 | up-left → up-right | present | Desired site and a different actual arrival |
| Radial O | 1 | right → right | present | Centre, common reach, whole traversal and an explicit return to the start |

The whole D returns to its starting site. On the bowl alone the endpoints
have the same horizontal projection and different heights. Their headings
are opposed. A single forward straight stroke cannot satisfy those end
conditions. Distinct straight spans with corners can. With interior evidence
withdrawn the concrete matches in this roster are the three D constructions,
P and R. The last two share a compatible upper bowl. Testing for an extra
leg is a separate question. A local change of tangent concerns the path.
Vector-field curl would require a field and a chosen neighbourhood.

The ungapped radial O uses one source arc, irrespective of display size.
The radial contract supplies a common nonzero reach throughout a whole planar
turn. Its centre and reach are construction. The boundary is ink. This fixture
starts with that retained boundary. Formation of a durable boundary from an
evolving field of tension remains an experiment to do.

## Encoding chart

One owner stores ordered pairs of site references per span and signed span
uses in writing order. A change of site between uses requires a pen lift.
Windows are half-open intervals in this use array. Window 0 selects ink and
window 1 the continuous probe. An optional window 2 keeps a whole source.
An additional rung row [2,1] preserves which source supplied the part.
The source arc slots bind directly to ink spans at corresponding end sites.
At the source seam a three-bit outward heading grounds the ray in the letter
frame. Compare it with the departure tangent. The half-circle D goes clockwise
from its upward ray. The gapped O goes counterclockwise from its up-right ray,
retaining the long arc between the two lips in this realization.

| Physical row | Example | Meaning |
| --- | --- | --- |
| Ink endpoints | D has [1,0] [0,1], packed `09` | The stem and return meet at both physical sites |
| Headings | Return [0,4] | Right on departure, left on arrival in the upright frame |
| Drive connection | D has [0,0] [1,1], packed `50` | Stem holds its heading. Return advances with clockwise steering |
| Unbound return | D with ends only has the single drive row [0,0], byte `00` | The stem has a zero steering word. The return has no steering connection |
| Radial C endpoints | [0,1] [1,0], packed `06` | The retained and omitted arcs meet at the same two cuts |
| Radial C windows | [0,1] [0,1] [0,2] | Ink and probe see the retained arc. The source sees both |
| Closure request | Written C adds [0,0] | Span 0 actually arrives at site 1. The desired site is 0 |

Heading slots go clockwise from right in the selected eight-heading frame.
The examples use this as a small directional vocabulary, without measuring
distances. Steering has a fixed orientation. ODV words 000, 001 and 011
hold the heading, turn clockwise or turn counterclockwise during travel.
Rate and speed remain open. The model declares this coupling over the span.
Two readings of the tips alone cannot establish continuous steering.
The resulting presence supports 01, 02 and 03 are hexadecimal masks for
absent, present, or either. The circumference uses the existing
[radial contract](RepetitionExperiments.md) and needs no steering samples.

The reader counts independent ink cycles by edges − sites + components.
Nonzero enclosed area requires more geometric evidence than this count.
The proposed and actual closure sites remain separate. A failed claim does
not apply a repair force. The handwriting order also supplies no rule for
subsequent material flow through joints. Coordinates and drawing curves in
the viewer illustrate one allowed realization and never enter the readers.

~~~powershell
dotnet run --project experiments/PrimitiveMeasure -c Release -- --curved-capitals
~~~
