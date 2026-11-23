# Bit polarity: what 1 and 0 mean

Status: **decided**, 23 November 2026. The rule is now canonical prompt
§9.7: 1 is presence or the dominant member, 0 absence or the recessive one.
EncodingC is changed to it: supports set 1 for a value still possible, with
planes that fill new positions with 1; ODV D sets 1 for positive; ODV O sets 1
for the forward face (positive-right). The inventory below is the audit that
led here. Since the audit, `Paths.Direction` is retired: path steps are ODV
step words, so forward is D 1 like every other positive.

**The older encodings keep their historical polarity and will not be
changed.** `Gojo.Encoding`, including its promoted core, is historical
evidence: EncodingC has no reference to it and shares no code with it, and
the experiments and visualizations that use it record results under its
original conventions. A difference in polarity between the two is not
unfinished work. When EncodingC needs an idea that exists only there, the
idea is rewritten in EncodingC under §9.7, never copied or backported. The
kernel's `Carrier.IsInverse` is a named Boolean rather than a stored bit, and
reads correctly as it is.

## The two rules in use

The code mostly follows one of two coherent rules, and uses each for
different kinds of bit:

- **Presence: 1 is the thing being there.** More participation reads as more
  set bits. This is the user's preferred rule: dominant, true or positive is
  1, recessive is 0.
- **Markedness: 0 is the default, 1 is the exception.** A fresh, zeroed store
  means "the usual case", and a set bit marks the less usual alternative. This
  is the linguist's notion of the unmarked and marked members of a pair.

The two agree for presence-like bits and disagree for sign, orientation and
face, where the code treats positive, right and forward as the unmarked
default 0.

## Inventory

| Bit | 1 means | 0 means | Rule | Agrees with "dominant is 1"? |
| --- | --- | --- | --- | --- |
| ODV V | Detectable at this resolution | Not detectable | Presence | Yes |
| ODV D | **Negative** | Positive | Markedness | **No** |
| ODV O | **Positive-left** (opposed face) | Positive-right (forward face) | Markedness | **No**, if forward is dominant |
| Kernel `Carrier.IsInverse` | **Opposed face** | Forward face | Markedness | **No** |
| Promoted core `negative`, `positiveLeft` | Same as ODV D and O | | Markedness | **No** |
| Unary count, bit j | More than j | Not more than j | Presence | Yes |
| Store `Live` | In use | Spare or retired | Presence | Yes |
| Occupancy, passage S and T | A unit is there | Empty | Presence | Yes |
| Passage gate C | Open, flow passes | Shut | Presence | Yes |
| Law table bit | Tuple **admitted** | Not admitted | Presence | Yes |
| Supports plane | Value **ruled out** | Still possible | Markedness | **Mixed**: the opposite of a law table |
| Learned table `Admitted` plane | Admitted | Not | Presence | Yes |
| Row evidence planes (changed, unread, …) | That happened | It didn't | Presence | Yes |
| Workspace held and selection keep | Held, kept | Not | Presence | Yes |
| `Paths.Direction` values | Forward = 2, stay = 1, back = 0 | | Presence (more forward is larger) | Yes, but the **opposite** of ODV D |

## Where meanings do flip

1. **Sign and face are marked, not dominant.** ODV D, ODV O, the promoted
   core's sign and orientation, and the kernel's `IsInverse` all set 1 for
   negative, left or opposed. They agree with one another, so nothing flips
   between them. But they are the inverse of "positive is 1".
2. **Supports and law tables disagree inside EncodingC.** A law table sets 1
   for what is admitted; a support sets 1 for what is ruled out. Yet a support
   is a one-role relation, the same kind of thing as a table. This is the one
   place where the same idea, possibility, takes opposite bits in two
   structures that meet constantly. The older packed core uses admit = 1,
   which the cloud handoff already flags as a mapping hazard.
3. **Step direction has two encodings.** `Paths.Direction` makes forward the
   largest value. An ODV step word makes forward D = 0. The planned move of
   steps onto ODV words would carry the inversion into paths.

There is also one limit that is not a flip. ODV is sign and magnitude, so no
assignment of the two bits D and V makes the 2-bit code monotone across the
sign: "−? < ±0 < +?" cannot be "smaller code, smaller amount" while V keeps one
meaning. Within the positive side, larger stays larger. Order across signs is
a read (`OdvWord.Compare`), not the code's numeric order. That is inherent,
not a mistake.

## Why markedness was used

The code's own rationale for supports is that "a fresh position is therefore
open with no extra step, and each bit set is one thing learned". Markedness
makes a zeroed allocation mean the default, and makes narrowing only ever set
bits. The same reasoning made positive, right and forward the zero defaults.
These are real conveniences, but they are conveniences of allocation, which
canonical prompt §9.6 says must not shape the concept.

## Recommendation

Adopt one stated rule, **1 is presence, dominance or the positive member; 0
is absence or the recessive member**, and add it to the canonical prompt
beside "larger things read as larger numbers". Then, as separate, tested
changes:

1. **Supports: possible = 1.** This matches law tables, and makes "nothing
   possible", a contradiction such as the Main Street case, read as all zeros,
   which is absence. The cost is that a fresh position must be filled with
   ones to be open. A store can do that once with a constant plane, so it is
   not a per-bit cost. Narrowing would clear bits instead of setting them;
   it stays monotone either way.
2. **ODV D: positive = 1.** Then forward steps and positive sides are 1, and
   `Paths.Direction` agrees with ODV. The packed bytes `88 C6 FA` change, and
   the tests that pin them must be updated knowingly.
3. **ODV O and the kernel face: forward = 1**, only if forward is agreed to be
   the dominant face. Orientation is the case where dominance is least clear:
   positive-right is a presentation choice (axiom 11), while forward against
   opposed is structural. If forward is dominant, the kernel's `IsInverse`
   becomes the recessive 0 and reads correctly as it stands; only ODV O and the
   promoted core's `positiveLeft` would flip.

Until those are decided, new code should read bits through the named
accessors (`Detectable`, `Negative`, `PositiveLeft`, the supports API) rather
than raw bit positions. That way a later flip changes one codec, not every
law built on it.
