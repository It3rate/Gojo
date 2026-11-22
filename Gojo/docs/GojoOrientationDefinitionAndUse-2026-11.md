# Orientation, definition and use

Status: **proposal for review**, 22 November 2026. Nothing here changes the
canonical prompt or code until it is accepted. It records the model agreed in
conversation, states its consequences precisely, and lists the exact prompt
edits, code changes and tests it would need.

## 1. The change in one paragraph

A domain is **defined** on an orientation and **used** by traversal, and the
two are kept apart. A directed definition lives entirely on one orientation
and runs from its lower end to its higher end, so its lower end is its natural
start. Nothing is defined backwards: the opposite direction is a definition on
the opposite orientation. A non-flowing definition, a **range**, lives on both
orientations from one shared origin and has extent but no direction and no
natural start. Traversal is a use, and a use may run along a definition or
against it. Converting between orientations is a cheap, default operation
because structures built to match share an origin by default.

This is still pre-numeric. Everything below needs only an order (`<`, `>`,
`≤`, `≥`) within one orientation and the moves between orientations.

## 2. Terms

| Term | Meaning |
| --- | --- |
| Orientation | Which way a frame calls positive: the forward face F ("positive-right") or the opposed face I ("positive-left") of a carrier. ODV's O bit |
| Origin | The site where a carrier's two orientations meet. Both read outward from it |
| Encoded value | A value as defined: an orientation and a signed amount on it, written `(F, x)` or `(I, x)`. ODV's O and D bits, with V as its magnitude at the read's resolution |
| Site | Where a value sits on the carrier, whatever orientation it is written on. Reading only the site is a fold of the encoded value |
| Definition | What a domain is: its orientation (or both) and its ends |
| Use | What is done with a definition: a traversal, a containment test, a selection. A use has a direction per step; a definition has only an orientation |

"Up", "down", "left" and "right" stay presentation words (Axiom 11). F and I
are faces; drawing I to the left is a convention of the view.

## 3. Three kinds of definition

| Kind | Lives on | Natural start | Flow | Written |
| --- | --- | --- | --- | --- |
| Directed segment | One orientation | Its lower end | The orientation's | `S(C,a,b)`, `a ≤ b` on C; `S(C*,a,b)` on the other |
| Range | Both orientations, one origin | None | None | `R(C,s,e) = P(C*,s) -> P(C,e)` |
| Loop *(later)* | One orientation, closed | None | The orientation's | A directed traversal whose end is its start site |

The loop is listed because it shows that **flow and natural start are
independent**: a loop flows but has no lowest site. In two dimensions a filled
shape's perimeter is a loop and its area is a range, two reads of one
construction. The loop is not part of this change; it is a prediction to test
when shapes arrive.

A zero-length directed segment is a point that keeps its orientation. A
zero-length range is a point with none. Both remain samples (canonical prompt
§7.1): "arrived at the store" and "at the store" differ in exactly this way.

**Strict and non-strict order carry meaning.** `<` against `≤` says whether a
landmark belongs to the domain: an open or a closed end.

## 4. Moves between orientations

Four moves act on an encoded value `(o, x)`:

| Move | Result | Site | Order | ODV word |
| --- | --- | --- | --- | --- |
| Reorient | `(¬o, −x)` | Same site | 2 | Flip O and D (`OdvWord.Reorient`, exists) |
| Negate | `(o, −x)` | Mirrored | 2 | Flip D (`OdvWord.Reverse`, exists) |
| Face flip | `(¬o, x)` | Mirrored | 2 | Flip O only (new) |
| Generator J | `(F,x) → (I,x)`, `(I,x) → (F,−x)` | Turns | 4 | Flip O from F; flip O and D from I (new) |

Reorient, negate and face flip are involutions that commute, and each is the
composition of the other two: `Reorient = Negate ∘ FaceFlip`. Together with
the identity they form a four-element group in which every element undoes
itself.

**Reorient is the default conversion.** It changes how a value is defined and
leaves where it is untouched, so it needs no calibration beyond the shared
origin. That is why it can be cheap and common: a whole store reorients with
one constant bit and one XOR of D (`Odv.Reorient(store)`, which exists), and a
reoriented *view* can do it without copying (section 7).

## 5. The generator, and why it turns four times

The generator is not one of the three involutions. From the forward face it
is a face flip; from the opposed face it is a reorientation:

```text
J(F, x) = (I, x)      face flip: same amount, other orientation
J(I, x) = (F, −x)     reorient:  same site, back on the forward face
```

So `J² = Reorient ∘ FaceFlip = Negate`, and `J⁴` is the identity. The cycle
`1 → i → −1 → −i → 1` is `(F,+) → (I,+) → (F,−) → (I,−) → (F,+)`, which is
the flow codex's `PF -> PI -> NF -> NI -> PF` (Axiom 10) with each step now
named: **moving the encoded value onto the other kind of flow, then reading
the same site back**. Your sense that the multiplications were flipping
orientation and direction is right, and this is the precise form of it. The
order of four comes from the generator acting differently on each face; no
involution alone can produce it.

Read only the site, and `J(F,1) = (I,1)` lands on the site of `(F,−1)`.
That is not a collapse of `i` into `−1`. The site read is the lossy scalar
fold Axiom 10 already allows. The algebra acts on the encoded value, its
orientation of definition and its amount, and the fold forgets the
orientation. The repository's opposed-face construction, where the imaginary
face is not the negative of the real one, is this same statement: `(I,1)` and
`(F,−1)` are different definitions of one site.

A range is the two-coefficient value: start on I, end on F, from one origin.
Its site fold is an interval. This is where "complex arithmetic on a segment"
lives.

This section is **exploratory**: the table is exact, but whether multiplying
ranges by J gives the geometric behavior wanted later (rotation of a shape,
anti-participation after two turns) is to be checked, not assumed.

## 6. Conversions a traversal needs

All of these are reads; none changes the definition they start from.

| From | To | How |
| --- | --- | --- |
| A backward use of a directed segment on F | A forward use of its reorientation on I | Reorient every site; the steps are unchanged |
| A range `R(C,s,e)` | A directed segment on F | Start `Reorient(I,s) = (F,−s)`, end `(F,e)` |
| A range `R(C,s,e)` | A directed segment on I | Start `Reorient(F,e) = (I,−e)`, end `(I,s)` |
| A directed segment `S(C,a,b)` | A range | `s = Reorient` of the lower end onto I, `e = b` |
| A range whose start is past its end | Nothing directed | The bounds are reported, not repaired: an empty range is a contradiction, or anti-participation if a structure reads it so |

**Same origin by default.** Structures built to match one another share an
origin unless something declares otherwise. A different origin is an explicit
calibration (canonical prompt §4.6), never an implicit offset.

**A use records its direction relative to the definition**, step by step.
"Turned back" is a step whose direction is negative relative to the domain's
orientation. The same use read on the reoriented definition has positive
steps. Which one was written is retained, so a conversion never silently
changes the evidence.

## 7. Exact canonical prompt edits

### §2.4 Relation (constructors)

Replace:

```text
A directed segment may preserve opposed-start and forward-end participation:

DS(C,a,b) = P(C*,a) -> P(C,b)
```

with:

```text
A directed segment is defined on one orientation and runs from its lower end
to its higher end; the lower end is its natural start. The opposite direction
is a definition on the opposite orientation, not the same ends written in the
other order:

S(C,a,b)  = P(C,a) -> P(C,b)     a ≤ b on C's orientation
S(C*,a,b)                        the same, on the opposed orientation

A range is defined on both orientations from C's origin. It has extent and no
direction or natural start:

R(C,s,e) = P(C*,s) -> P(C,e)

Structures built to match share an origin by default.
```

### Axiom 3: Order Is Primitive

Replace the body with:

```text
Relation order is meaningful before an algebraic label is applied. A
definition's direction is its orientation: a directed segment runs from lower
to higher on the orientation it is defined on.

S(C,a,b) != S(C*,a,b) as definitions

Traversal is a use. A use may run along a definition or against it; the
direction of each step is read relative to the definition's orientation and
kept as evidence. A backward use of a definition and a forward use of its
reorientation cover the same sites; which was written is retained. Forward
frame, reverse traversal, wrap, clamp, projection and routing behavior must
emerge from visible ordered sites and local context. Do not sort away the
direction of a use when it participates in the read.
```

### Axiom 4: Duality, Reversal, And Contextual Inversion Are Distinct

Replace the two code blocks and the closing sentence with:

```text
Dual(element)       element-local mirror
Reorient(read)      the same sites read on the opposite orientation
Reverse(use)        the same surface traversed in the opposite order
Invert(read/use)    contextual use of Dual, Reorient, Reverse, or several

Dual(C)          = C*          opposed-flow carrier face
Dual(L(C,n))     = L(C,-n)     scalar negation on the same carrier
Reorient(L(C,n)) = L(C*,-n)    same site, other orientation
Reverse(A->B)    = B->A        reversed traversal

Do not collapse negative scalar location, opposed face, reorientation,
reversed traversal, unit denominator, subtraction, or division into one flag.
Reorientation is the default conversion between orientations under a shared
origin; it changes how a value is defined and not where it is.
```

### Axiom 10: Unit Algebra Remains Structural

After "In either case preserve the two coefficient roles and the generator's
action…", add:

```text
In the opposed-face construction, the generator moves a forward-face value
onto the opposed face with the same amount, and reorients an opposed-face
value back onto the forward face: J(L(C,n)) = L(C*,n) and
J(L(C*,n)) = L(C,-n). Twice is negation and four times the identity. A read
of the site alone folds J(1) and -1 together; that fold is the lossy scalar
read, and the algebra acts on the definition.
```

### §8.2 or Axiom 11 (exploratory note)

```text
A filled shape's perimeter is an oriented loop, with flow and no natural
start; its area is a range across both orientations. Area and perimeter are
two reads of one construction.
```

### §12 Known Maturity Boundaries

Add: "loops as oriented definitions without a natural start, and fill as a
range in two dimensions". Remove nothing.

### Design notes

Add a section "Orientation, Definition And Use, November 2026" explaining the
reason: it moves direction out of definitions and into uses, gives reversal a
single home, and names the generator's two moves.

## 8. Code changes

### EncodingC (this change)

1. **`OdvWord.FaceFlip()`** (flip O only) and **`OdvWord.Turn()`** (the
   generator: flip O on F, flip O and D on I). Document the four moves as the
   table in section 4.
2. **A reoriented view.** A surface over a store that shows its orientation
   flipped without copying: it reads and writes O and D inverted, and laws run
   on it unchanged. Reorientation then costs nothing to set up and stays live,
   as a workspace does. A range is a store read through the view for its start
   and directly for its end, so its shared origin is structural, not declared.
3. **Order with a magnitude.** ODV's V is a one-bit unary magnitude. For the
   sentence, extend it to a short unary count: an encoded value is O (the
   store's), D and a unary magnitude. Order within one orientation is a
   relation table over (D, magnitude), with `−2 < −1 < −0 ≤ +0 < +1`; the
   detection contract's treatment of zero is kept. Values on different
   orientations are compared only after a reorientation.
4. **Rework the sentence fixture.** The goal defines the path on F, home lower
   and the store higher. Each moment holds the mover's site on it. The
   approach is a use with positive steps; "turned back" is the same definition
   used with negative steps. Reaching the store is the ≤ case at the store's
   site. The lane still carries only the turning moment.

### Kernel (not in this change)

The kernel's `Segment(C,a,b)` is an ordered relation that allows `a > b`,
projection reads reversed target frames from it, and `DS` is the name in the
prompt. Under this proposal those become a definition on `C*` or a backward
use. Changing them is a separate migration; until then the kernel's reversed
frames are read as "a definition on the opposed orientation, written as an
ordered relation", which is behavior-preserving. This is stated so nobody
mistakes the prompt change for an implemented kernel change.

## 9. Tests

- The three involutions commute, each undoes itself, and each is the
  composition of the other two, for all eight ODV words.
- `Turn` has order four, `Turn²` is `Reverse`, and `Turn` is not any of the
  three involutions.
- `Turn(+1)` and `−1` have the same site and different words.
- A store read through the reoriented view runs a law exactly as the store
  reoriented in place does, and the store's own bits never change.
- Sentence: the approach has positive steps on the goal's definition, the
  return negative steps on the same definition; read on the reoriented
  definition the signs swap and the sites are unchanged.
- Sentence: a new goal on the opposite orientation reads the return as
  positive without changing a recorded site.
- A range over the home and the store covers the same sites as the directed
  segment between them, is unchanged when its bounds are swapped, and has no
  natural start; converting it to a directed segment on either orientation
  gives the lower end as the start.
- A range whose start is past its end is reported, not repaired.
- A directed definition written with its ends high to low is refused (see 10).

## 10. Open questions

1. **Writing ends high to low.** Should `S(C,b,a)` with `b > a` be refused, or
   read as the reoriented definition? I recommend refusing: a silent
   reorientation is exactly the conversion this change wants to keep visible.
2. **Where the sentence's origin sits.** Home is the natural origin, so home is
   the lower end on F. If the mover starts somewhere else, is that site the
   origin, or a landmark on a home-origin path?
3. **Order without a magnitude.** The fixture uses a short unary magnitude as an
   ordinal position. A strictly pre-numeric alternative is a chain of sites
   with a successor relation, where `<` is reachability. The unary form is
   cheaper and still only ordinal; the chain form avoids any count. Which
   should come first?
4. **J on ranges.** Section 5 predicts that turning a range moves its bounds
   between the faces and that two turns give its negation. Whether that is the
   wanted geometric reading needs a test before it is relied on.
