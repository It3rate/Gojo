# Experiment Notes

These notes are the pieces worth carrying forward from `GojoCAD.SketchViewer`
and `GojoDraw.Visualizer` after removing those projects from the solution.

## What To Keep

- A rung result should be the current structure. Evidence and application
  relations can remain connected, but they must not masquerade as the current
  drawing object.
- `FromArea` means an outline was derived from an area; it is not itself
  outline mode. If a page is working with outlines, carry the outline witness
  as the active structure.
- Fill and outline are intertwined but separate. Area can produce outline, and
  outline can produce area, but conversion should be explicit on the ladder.
- Selection and action should use the same kind of structure: a concept-shaped
  read such as a cusp can become the write shape for a rounded cusp.
- Orthogonal side evidence matters. Offset lines need explicit positive and
  inverse side witnesses so later fill synthesis can determine area, anti-area,
  or no-fill from local structure.
- Endpoint caps are not ordinary stroke offsets. They need their own rung so
  tip circles can grow outward while preserving the intended inner hole.

## What To Rebuild

- Boolean synthesis should be rebuilt in GojoCAD from line, arc, circle, and
  orthogonal-fill evidence. The visualizer tried too much Skia-side merging.
- Composite loops should be emitted directly as exterior and interior outline
  witnesses, then turned into fills only when that conversion is the explicit
  operation being shown.
- Cusp search should remain generic: pass a concept shape/domain, emit matches,
  and let the action replace only the selected local structure where possible.

## What Not To Port

- Do not port sample-specific radial ladder geometry into core. It was useful
  scaffolding, but too much of it was tuned to a few shapes.
- Do not port broad renderer fallbacks as meaning. Gojo structure should carry
  the distinction between current result, evidence, residue, and earlier rungs.
- Do not turn visual page state into the model. UI pages can store page index
  and presentation state, but Gojo meaning belongs in carriers and relations.
