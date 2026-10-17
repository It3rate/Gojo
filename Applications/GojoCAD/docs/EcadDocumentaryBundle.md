# Gojo ECAD Documentary Bundle

This note captures the current direction for representing PCB projects in
Gojo. It is deliberately not a KiCad, EasyEDA, Eagle, Gerber, or G-code clone.
Those formats are important source witnesses and output lenses, but the Gojo
format should be a documentary bundle for the board project itself.

The bundle should hold tight information and loose information together:

```text
raw ECAD artifacts
  -> dialect reads
  -> schematic, PCB, manufacturing, and documentation witnesses
  -> Gojo relation structure
  -> purpose-specific readouts
  -> residue, unresolved claims, and tension
```

The point is not to force every source into one clean tree. PCB projects are
not clean trees. A board has geometry, electrical intent, manufacturing
constraints, documentation claims, component data, firmware expectations,
connector ecosystems, assembly procedures, and verification status. Those facts
cross-link heavily. A good Gojo encoding should let a read surface pull the
pieces relevant to its purpose without pretending that unrelated pieces do not
exist.

## Intent

A Gojo ECAD bundle should let different readers ask different questions:

- A milling or G-code reader asks for copper to remove, holes to drill,
  board-side orientation, tool diameter, isolation rules, and safe machine
  motion.
- A schematic reader asks for components, pins, nets, power domains,
  interfaces, and unresolved or implied design intent.
- A manufacturing reader asks for stackup, clearances, paste, mask, BOM,
  pick-and-place, assembly limits, and which outputs have actually been
  generated.
- A conceptual reader asks why the board exists, which functional blocks were
  chosen, what compromises remain, and what evidence supports those claims.
- An ML reader may ask for a low-resolution project summary, a feature ladder,
  a local training example, a source-grounded explanation, or a high-resolution
  graph of pads, traces, and constraints.

The same bundle should support all of those reads without moving meaning into a
code-side wrapper model. The source of meaning remains ordinary Gojo structure:
carriers, locations, relations, unit shapes, read witnesses, residue, tension,
timelines, and crystallized settlement surfaces.

## Core Shape

The bundle can be read as an assembly of surfaces rather than a single schema.
Names below are working names, not a request for wrapper classes.

```text
Project surface
  raw artifact surface
  dialect read surface
  board geometry surface
  schematic intent surface
  component and capability surface
  net, pad, trace, via, zone, and area surfaces
  documentation claim surface
  external evidence surface
  manufacturing and validation surface
  operation and assembly surface
  correspondence and variant surface
  resolution ladder
  timeline and provenance
  residue and tension
```

Each surface should be locally traversable. A claim about a pull-up resistor
should attach near the resistor, bus, and source documentation. A firmware link
should attach near the microcontroller, connector, or protocol surface it
explains. A board status claim should attach to the manufacturing/validation
surface, not to a generic project note.

## Raw Artifacts Are Witnesses

The bundle should preserve source artifacts as witnesses:

- `.kicad_sch`, `.kicad_pcb`, `.kicad_pro`
- Eagle `.sch` and `.brd`
- EasyEDA Standard JSON and EasyEDA project ZIPs
- Gerbers, drill files, IPC-D-356, pick-and-place, BOMs, PDFs, images, STEP
  files, firmware links, datasheets, product pages, and README documents

The raw file does not own the canonical meaning after import. It remains a
witness that can be reread through a chosen dialect surface. If two imports
produce different structure, the conflict should be visible as read residue or
tension.

## Dialect Read Surface

KiCad, EasyEDA, Eagle, Gerber, and G-code all need explicit dialect reads. The
reader should preserve the format lens that produced a result:

```text
source artifact
read through dialect/version/basis
produced parsed structure
left parser residue
with parser provenance
```

For EasyEDA Standard samples this matters immediately:

- PCB files often read as `docType=3` with shape arrays.
- Schematic projects may read as `docType=5` containers with one or more
  schematic sheets.
- Shape strings are compact, delimiter-heavy source records, not already-clean
  semantic objects.
- Schematic and PCB pairing may come from naming conventions or README claims
  rather than explicit project metadata.

The dialect surface should include coordinate basis, unit/scaling basis,
orientation assumptions, layer dictionary, shape ordering evidence, and raw
fields that did not settle. A future parser may convert these into richer Gojo
structure, but the first import should not discard the weirdness.

## Documentary Claims

The samples show that documentation carries design facts that are not optional
decoration. A Gojo bundle should preserve claims as local read witnesses:

```text
claim source
selected target structure
claim surface or basis
claimed result
verification state
residue or tension
provenance
```

Examples from the current samples:

- The BQ24074 PMIC board README says high-current VBUS/VOUT/BAT tracks use a
  minimum 0.5 mm width, low-ESR capacitors are placed near PMIC pins, ground
  planes and via stitching support return paths and thermal dissipation, DRC
  passed at a stated clearance, and STEP models are intentionally excluded.
- The BME280/MPU6050 board README says 4.7k pull-ups were selected for I2C up
  to 400 kHz, address jumpers expose alternate addresses, decoupling capacitors
  sit near sensor power pins, Gerbers are ready, but fabrication and testing
  have not happened yet.
- The AVR ZIF programmer README contains an operation/compatibility table:
  which AVR family maps to which ISP header, and how the user must orient the
  IC in the socket.
- The Salty Spoon README states that one EasyEDA schematic is an older variant
  and that the PCB corresponds to the newer schematic.
- The SHOC keyboard README says the board is tied to QMK firmware, repeated
  switch/diode structure, OLED support, and an EasyEDA export limitation.
- The SparkFun Qwiic BME280 README ties the board to Arduino/Python libraries,
  Fritzing, a hookup guide, and the Qwiic ecosystem.

These are not all the same kind of fact. Some are design intent, some are
verification, some are operation, some are ecosystem context, some are tool
limitations, and some are unresolved or possibly stale. The common move is that
they attach to structure as witnesses and remain available to later reads.

## Validation Ladder

Documentation often collapses several states into phrases like "complete" or
"ready." Gojo should split those into a validation ladder:

```text
idea / product goal
schematic captured
PCB placed
PCB routed
DRC passed
manufacturing outputs generated
BOM / pick-and-place settled
fabricated
assembled
powered
tested
measured
field-proven
```

A low rung is not failure. It is the current read. A board with Gerbers and no
test evidence should not be treated like a board with measured performance.
The missing test evidence is useful tension.

## Correspondence And Variants

Real projects include variants, revisions, stale files, screenshots, exported
PDFs, and generated manufacturing outputs. A bundle needs explicit
correspondence surfaces:

```text
source A
corresponds to / implements / generated from / supersedes / conflicts with
source B
with basis and confidence
with residue
```

This is especially important when:

- README text says one schematic corresponds to one PCB.
- A schematic is missing even though documentation lists it.
- A Gerber ZIP is generated from an unknown board revision.
- An imported EasyEDA project has multiple schematic sheets.
- A board has Rev A/Rev B layouts, alternate connectors, or package options.

The correspondence should be evidence, not a filename heuristic hidden inside
the importer.

## Concept Ladder For Boards

The bundle should support low-to-high resolution concept ladders:

```text
product goal
functional block
component capability
pin role
net / signal / power domain
pad / footprint / package
trace / via / zone / copper area
manufacturing output
physical board evidence
test evidence
```

A temperature sensor board can be read roughly as "BME280 weather sensor on
Qwiic I2C." It can refine into component capabilities, pull-ups, address
selection, connector pinout, exact traces, solder jumpers, and library links.
Both readings are valid at their requested resolution.

A charger board can be read roughly as "USB-C to LiPo charger and system power
path." It can refine into PMIC pins, charge-current constraints, thermal
evidence, battery pads, high-current copper, decoupling placement, and assembly
risks.

A keyboard can be read roughly as "repeated switch matrix." It can refine into
row/column nets, diode orientation, switch footprint repetition, firmware key
positions, enclosure files, and human use.

## Geometry, Area, And Orientation

PCB geometry must preserve orientation and sidedness. A read should never have
to guess whether it is looking at the top from above, the bottom from below, or
a mirrored manufacturing view.

Useful surfaces:

- board coordinate basis and origin
- top and bottom face basis
- layer stack and layer order
- board outline and edge cuts
- copper participation
- anti-area, keepout, holes, slots, and cutouts
- solder mask and paste openings
- silkscreen and documentation markings
- component placement orientation
- drill and plating state
- fixture, panel, or milling orientation

Area should not be only a polygon. Copper zones, board outline, cutouts,
clearances, isolation paths, solder openings, and empty space are all forms of
participation or anti-participation. A G-code reader may care about the same
area at a different resolution than a schematic reader.

## Component And Capability Surface

Components should not be only footprints or schematic symbols. They carry
capabilities, constraints, alternatives, costs, sourcing, and external evidence:

```text
component instance
  symbol witness
  footprint witness
  package and pin basis
  datasheet / product page / library links
  manufacturer and supplier parts
  cost / availability / assembly class
  capabilities and limits
  unresolved substitutions
```

This can remain sparse. The important thing is that a datasheet or supplier
link attaches to the component, pin, package, or capability it supports. It
should not disappear into a project-wide attachment pile.

## Operation And Human Use

Some board meaning only appears when a person uses it:

- align pin 1 with this socket edge
- select this programming header for this MCU family
- bridge this solder jumper for this I2C address
- connect this Qwiic cable in this orientation
- flash this firmware before expecting this connector behavior
- avoid this pad during manual soldering

These should be operation surfaces with selected structure, action basis,
expected result, failure/tension, and provenance. They are part of the board
project, even when they are not part of the PCB layout file.

## Physical Encoding Direction

The physical storage can be repository-like without becoming "just Git."

A useful bundle layout might look like:

```text
bundle/
  artifacts/
    raw/
    generated/
    external/
  reads/
    dialect/
    schematic/
    pcb/
    manufacturing/
    documentation/
  witnesses/
    claims/
    validation/
    correspondence/
    source-links/
  ladders/
    concept/
    resolution/
    revisions/
  residue/
  exports/
```

This is not a required directory schema. It is a storage intuition:

- raw artifacts stay available
- read outputs are reproducible or traceable
- witnesses attach claims to local structure
- unresolved residue is preserved
- revisions and alternatives can keep participating in later reads
- export products such as G-code can be traced back to the source structure,
  basis, settings, and residue that produced them

A Git-like history is useful, but Gojo needs the old versions as active
starting points and comparison witnesses, not only as archival commits.

## Readout Examples

### G-code isolation read

```text
selected copper and board-side basis
tool diameter / machine basis
isolation and clearance surfaces
holes and drill surfaces
produced toolpath / G-code witness
left unresolved collisions, uncut islands, or unsupported geometry as residue
```

### ML concept read

```text
selected board project
requested resolution: rough product explanation
source witnesses: README, schematic, PCB, datasheet links
produced low-resolution concept ladder
left missing datasheets, unverified claims, and parser gaps as tension
```

### Design-review read

```text
selected net or functional block
constraints and documentation claims
geometry and component witnesses
validation state
produced concern / confidence / unresolved question
left unverifiable claims as residue
```

## What Should Stay Loose

Several things should remain intentionally loose for now:

- exact on-disk structure
- how much raw JSON/S-expression data to preserve in each read
- whether all documentation claims become Gojo relation structure immediately
  or first pass through a lighter witness index
- how aggressively to infer intent from component names, net names, and README
  prose
- how to represent confidence separately from ordinary tension
- how to keep very large projects tractable without flattening meaning into
  arrays that own the model

Loose does not mean vague. It means unresolved information should stay
available until a reader needs to settle it.

## Near-Term Import Direction

The next importer work should probably aim for a small durable spine:

1. Preserve raw artifacts and source paths.
2. Create dialect read witnesses for KiCad PCB and EasyEDA schematic/PCB JSON.
3. Convert boards, layers, nets, pads, traces, vias, zones, holes, and outlines
   into GojoCAD-compatible relation structure where the geometry is understood.
4. Attach parser residue where the source shape is unsupported or ambiguous.
5. Attach documentation claims from sample READMEs as local witnesses, even if
   the first version uses a simple hand-authored extraction.
6. Create correspondence witnesses between schematic, PCB, generated Gerbers,
   BOMs, PDFs, and README claims.
7. Add one G-code read that traces output back to the selected copper,
   board-side orientation, machine/tool basis, and unresolved residue.

That would give us a working documentary bundle without pretending the whole
domain is already solved.
