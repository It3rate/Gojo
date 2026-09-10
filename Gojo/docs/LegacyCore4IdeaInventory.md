# Legacy Core4 Idea Inventory

This note records the old Core4 ideas that looked worth keeping as design memory while extracting Gojo into a lean repo. These are not instructions to port old code directly. Most old code used model objects that Gojo should now express through carriers, relations, unit shapes, tension, residue, slots, timelines, and local reads.

The September 2026 [Core2/Core4 continuity review](GojoCore2Core4ContinuityReview-2026-09.md)
now compares actual demo families and historical tests with current Gojo,
including capabilities that remain only in the old projects and current CAD
helpers that still need the newer construction contracts.

## Keep As Design Memory

### Boundary Overflow Routing

The old boundary overflow notes had a strong route-based model: boundary crossing can drain, observe, wrap, branch, merge, count, filter, or become tension depending on attached route structure. The important Gojo-native idea is that loop, clamp, filter, counter, and branch behavior should be boundary-route structure, not enum-driven control flow.

### Traversal As Carried Structural Signature

The old trolley code should not come over as a class hierarchy, but the target behavior is useful. A traversal carries a local signature: selected support, placement, perspective, carried value or tension, provenance/weight, and local constraints. Re-entry, reflection, merge, wrap, and periodicity should be detected by comparing structural signatures rather than replaying a hidden path list.

### Basis Encounter And Seam

A seam is not a primitive object. It is a pinned basis encounter where continuation is nontrivial. Preserve the idea that traversal negotiates between carried calibration, local traversal state, encountered calibration, topology, and commitment. Reflection, clamp, closure, and partial transfer should emerge from the encountered structure.

### Support Projection Reads

Old support projection code separated forward support, reverse support, total support, tension, exclusive support, forward residue, and reverse residue. Gojo now has better boundary/flow lane structure, but this read family is still useful for future pass/block/filter surfaces.

### Attachment As Live Samples Through Frozen Basis

Old attachments bound live sample sites into a frozen support structure, preserving grouping and degrading weak reads into tension. In Gojo this should become slot/binding/repeat/read structure, not a wrapper object, but the "live samples through stable basis" behavior is valuable.

### Resolved Publication And Feedback

Old carrier communication distinguished local unresolved tension, resolved publication, resolved feedback, and sender-side aggregation. This is worth keeping for future port/field work. A receiving site should generally inspect or respond to publication, not mutate the publisher's structure in place.

### Equal Result, Different Structure

Several old notes emphasized that equal numeric reads can come from different participation lineages. Keep this. Gojo reads should preserve enough structural provenance that equal visible values can still differ by route, basis, repetition, resolution, or residue.

### Topology Examples

The old topology tests contain useful examples: T junctions, Y branches, endpoint bridges, H-like bridges, triangle loops, terminal reflection, diamond merges, and distinct exact sites sharing a whole location. These are good future diagnostic examples, but they should be rebuilt from Gojo relation/unit-shape structure.

## Do Not Port Directly

- Old `Core4`, `Core4.Kernel`, `Core4.Runtime`, examples, runner, Avalonia probe, SIMD, and gear demo projects.
- Ratio/Basis/Site/SupportStructure wrappers as sources of meaning.
- Trolley, propagation wave, and resistive circuit classes as canonical mechanics.
- Compatibility aliases from the old support projection layer.

## Gojo-Native Re-expression Targets

- Boundary events as visible route surfaces with residue/tension witnesses.
- Traversal signatures as relation-shaped local reads, possibly with timelines.
- Basis encounter as unit-shape/lane/flow evidence plus continuation routes.
- Publication and feedback as explicit port structures with read-only source evidence.
- Data attachment as repeated slot binding and resolution-ladder structure.
