# GojoCAD Electronics Component Encodings

This folder is the first experimental component-encoding area for electronics
inside GojoCAD. It is not a SPICE model library, schematic-symbol catalog,
footprint library, or BOM schema. Its job is to describe how simple electronic
components can be represented as inspectable Gojo structure.

The working rule is the same as the rest of GojoCAD:

```text
component meaning lives in connected carriers, relations, unit shapes,
read witnesses, residue, tension, and timelines
```

A component helper may build or inspect these structures, but the helper should
not own the component meaning. If a helper disappeared, the terminals,
parameters, capacities, transforms, unresolved ports, and read witnesses should
still be locally traversable.

## Component Surface

A component should start as connected participation rather than a code-side
type. The lowest useful component surface is:

```text
component instance
  terminal / pin / port participations
  internal relation surface between terminals
  domain parameter surfaces
  capacity / rating surfaces
  transform or flow surfaces
  source witnesses and lexical labels
  residue / tension
```

The same component can then support several reads. A resistor may be read as a
resistance relation, a pull-up, a load, a current limiter, a BOM line, a
footprint instance, or a documentation claim. Those reads should share lower
structure instead of becoming unrelated tags.

## Values Are Reads Over Resolution

Component values should not smuggle in precision that the source did not give.
A label such as `10K 5%` is not an exact hidden interval with exact endpoints.
It is a nominal resistor-ladder read plus a relative resolution read. A later
meter reading can refine or contradict that coarse source, but the nominal
source itself should remain lower resolution.

The general shape is:

```text
selected component parameter
  read through a domain basis
  produce a nominal / capacity / transform / measured result
  preserve residue where the source is coarse, incomplete, or incompatible
```

Use exact integer/rational structure when the source actually supplies it. Use
resolution ladders, repeated transforms, and residue-bearing reads when the
source is conventional, approximate, or only precise inside a domain scale.

## Component Families

Initial component families should stay small:

```text
resistors
  resistance relation, preferred-value transforms, tolerance resolution,
  power/voltage capacity, temperature coefficient, zero-ohm bridge read

capacitors
  charge/voltage storage relation, capacitance value ladder, tolerance,
  voltage capacity, dielectric and temperature residue

inductors
  current/field storage relation, inductance value ladder, current capacity,
  saturation and resistance residue

transistors
  controlled route, control terminal, channel relation, rating/capacity reads,
  unresolved model family residue
```

These names are folder and documentation names, not final ontological tags. The
goal is to grow a set of local surfaces that a schematic reader, board reader,
language reader, or solver adapter can inspect.

## Solver Boundary

A circuit solver should be an output/read surface over selected structure, not
the canonical component model. A solver may project an E-series resistor into a
larger internal carrier, equation basis, or sampled numeric approximation, but
that projection should record its basis and residue.

```text
selected circuit structure
read through solver basis
produce solver-local values or equations
preserve value-resolution, missing model, and unsupported-feature residue
```

This keeps the Gojo component structure honest. The source value can remain an
E6 transform position such as "22 E6 steps", while a specific solver read can
derive whatever representation it needs.

