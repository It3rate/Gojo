# Gojo Equations

`Gojo.Equations` is the first foothold for relation-shaped equations. An
equation is not a symbolic formula object. It is a Gojo surface that admits
input participation, predicts output participation, and exposes residue or
error as structure.

`Equation` stays deliberately tight: it builds equation surfaces and performs a
single local read through them. `EquationInspection` is the separate layer that
applies those surfaces to ordered sample streams, compares predictions with
observations, and records fit evidence.

## Affine Surface

The first equation shape is an affine frame surface:

```text
Segment(input, inputStart, inputEnd)
  -> Segment(output, outputStart, outputEnd)
```

`inputStart` and `inputEnd` are basis endpoints, not consecutive samples.

```text
Segment(sensor,0,10) -> Segment(percent,0,100)
sensor = 3 reads as percent = 30
```

The same shape covers unit conversion, normalization, proportional gear reads,
inverse direction, offsets, and coarse percentage projections.

## Prediction

Prediction is a read through the equation surface:

```text
input sample -> predicted output + source residue
```

The data does not become part of the equation. It is admitted through the input
surface and read into the requested output carrier.

## Patterned Equations

An equation surface and an input surface can both contain structural slots:

```text
equation pattern:
  (XPort -> YPort) -> Output

input pattern:
  XValue -> YValue
```

`EquationPattern` binds those slots through ordinary relation-shaped bindings
and then runs the same local equation read. If some slots are missing, the read
still returns a prediction/residue structure and reports the unresolved slot
surfaces:

```text
bindings:
  XPort  -> concrete x port
  Output -> concrete output

unbound:
  YPort
  XValue
  YValue
```

This keeps incomplete data frames inspectable. A missing value is not a thrown
validation failure; it remains visible as an open slot in the residue or
instantiated structure.

Patterned equation maps apply the same idea to streams of binding frames:

```text
PatternMapRoot
  + index span
  + equation pattern
  + input pattern
  + sample 0
  + sample 1

PatternSampleN
  + Point(index,N)
  + supplied slot bindings
  + predicted output location
  + residue
  + unresolved slot surfaces
```

Different samples in the same stream may be complete, partial, or differently
resolved. The map records that directly instead of requiring every frame to
have the same shape before it can participate.

The first stress footholds are deliberately small:

```text
Fibonacci-like frames:
  (a,b) -> a+b
  next frame supplies (b,a+b)

Generated partial frames:
  complete frame
  missing input value
  missing equation port
  missing output surface
  repeat...
```

Missing input values tend to remain visible in prediction residue because the
input pattern itself is still carried through the read. Missing equation ports
or output surfaces are still recorded in the unresolved-slot group, but they may
not survive inside operation residue after the equation read attempts to parse
the partial operation. The unresolved-slot group is therefore the authoritative
read for frame completeness.

## Sum And Product Surfaces

Weighted sums and products use existing Gojo operation surfaces. The ports are
ordinary frame relations, and the output surface decides the operation shape:

```text
// weighted sum
((Segment(x,0,1) -> Segment(Unknown,0,2))
  -> (Segment(y,0,1) -> Segment(Unknown,0,3)))
  -> Point(output,1)

// product
((Point(width,1) -> Point(Unknown,1))
  -> (Point(height,1) -> Point(Unknown,1)))
  -> Segment(area,0,1)
```

For the sum, each operand is projected into the equation reader basis and then
added. For the product, each operand is projected and then multiplied. Biases
or constants can be represented as ordinary supplied slots, such as a bias
carrier with value `1` and a weighted port of `0..5`.

## Sequences

`EquationSequence` composes equation surfaces without turning them into a
symbolic expression tree. A sequence is an ordered Gojo group:

```text
SequenceRoot
  + equation step 0
  + equation step 1
  + equation step 2
```

A single read through the sequence feeds each step's predicted output into the
next step. The final read returns the last predicted output, while the residue
is an ordered group of the step residues. The sequence itself is the structure;
the step reads are witnesses of one particular input moving through it.

This is the first foothold for multi-step equations such as:

```text
sensor -> normalized sensor -> squared sensor -> score
width,height -> area -> normalized area
```

## Fanout

`EquationFanout` is the paired branching shape for sequence composition. It
anchors an ordered branch group at a fanout origin:

```text
Point(fanoutRoot,0)
  -> BranchGroup
       + equation branch 0
       + equation branch 1
```

Each branch reads the same supplied input. The prediction output is a
tuple-shaped relation, not an ordered holder group, so it can feed existing
operation surfaces:

```text
input -> (branch0 output, branch1 output, branch2 output)
(branch outputs) -> Sum/Product/etc.
```

This gives the equation layer the first explicit `one input, many derived
participations` structure. It is enough to model shapes like `x -> (x, 2x) ->
sum`, and it points toward polynomial terms where later branches can derive
`x^2`, `x^3`, or lower-resolution witnesses before a merge surface reads them.

## Ordered Map

Mapping a stream is inspection structure built by `EquationInspection`:

```text
MapRoot
  + index span
  + equation surface
  + sample 0
  + sample 1
  + sample 2

SampleN
  + Point(index,N)
  + input location or relation-shaped input
  + predicted output location
  + source residue location or relation-shaped residue
```

The current helper still executes the per-sample loop in code, but the result
records the index sites and per-sample prediction groups. This keeps the
direction compatible with later time-step and SIMD-style execution without
making SIMD the source of meaning.

## Comparison

Comparison is also inspection structure:

```text
CompareRoot
  + index span
  + comparison sample 0
  + comparison sample 1

ComparisonSampleN
  + Point(index,N)
  + predicted output
  + observed output
  + error
  + equality claim surface
```

The comparison helper now builds a small equality claim:

```text
predicted -> observed
  + readable residual
  + one-sided/equality violation
  + residual magnitude
  + violation magnitude
  + residue
```

For same-face scalar samples the readable residual is still `observed -
predicted` on the supplied error carrier. Negative residuals remain negative
locations on that carrier; they are not converted into inverse-flow
participation. If the compared samples use opposed carrier faces, the claim
keeps a fused residual so a later surface can decide whether to fold, route, or
settle that opposed participation.

## Difference And Fit

The difference helper is not a symbolic subtraction node. It is a weighted sum
surface, so it belongs with `Equation`:

```text
(predicted port: 0..1 -> 0..-1)
+(observed port:  0..1 -> 0..1)
  -> Point(error,1)
```

Given relation-shaped input `(predicted, observed)`, it reads `observed -
predicted` onto the error carrier.

A fit summary is separate inspection structure. It gathers a stream of error
samples:

```text
FitRoot
  + index span
  + total signed error
  + total error magnitude
  + fit sample 0
  + fit sample 1

FitSampleN
  + Point(index,N)
  + signed error
  + error magnitude
```

The magnitude value is now read from the equality claim. It is still a small
inspection foothold, but the mismatch that produced it remains attached as
claim/residual structure so later fit, thresholds, and "close enough" reads can
settle from the same surface rather than from a hidden classifier.

## Next Targets

The intended sequence is:

```text
affine frame             y = ax + b
weighted sum             y = a*x + b*z + c
difference/error         observed - predicted
product/bilinear         x*z
sequence/composition     f(g(x))
fanout/merge             x -> (f(x), g(x)) -> h(...)
fit summary              samples -> signed/magnitude residuals
piecewise/clamp/gate     bounded local equations
polynomial               c0 + c1*x + c2*x^2 ...
state transition         state[t+1] = f(state[t], input[t])
exponential/log/trig     higher equation families
candidate fitting        samples -> parameters + residual
```

Each target should first be a clear Gojo structure, then an executable helper,
and only later a compiled/backend representation.
