# Drawing through retained construction steps

This experiment builds a drawing from earlier construction sites. It extends
the [line-art controls](LineArt.md) with executable numeric programs and
refinement history. It does not fit a church to Quick Draw observations or
claim learned drawing intention.

The public study is under the [Quick Draw category](https://gojo.inklingai.ca/quickdraw/)
at [Construction steps](https://gojo.inklingai.ca/quickdraw/construction/).
Its four families have twelve parameterized runs and 56 drawing rungs:

| Family | Rungs | Runs | Selected question |
| --- | ---: | ---: | --- |
| Church facade | 6 | 4 | Can a wall frame guide a window, an arched replacement, three occurrences and later detail? |
| Partitioned star | 4 | 3 | Can circular partition, radial inset/outset and ordered joins remain inspectable? |
| Crescent | 4 | 3 | Do shared tips, facing and earlier envelope references survive refinement? |
| Organic contour | 4 | 2 | Can one local bend change while other curve identities carry forward? |

Three church runs are valid authored constructions with different proportions.
The fourth intentionally spaces the windows too far apart. Its failed wall
containment claim remains visible; the generator does not silently clamp or
repair its geometry. A cross is included by this particular authored recipe.
There is no rule that classifies churches or assigns crosses to a category.

## Construction is the program

The Python source authors locators and references, rather than exporting final
coordinates as an answer. Import creates an ordinary native Gojo program with
ordered site and curve participation, bound parameter carriers, rung plans,
containment requests, repeated-template occurrences and circular construction
requests. Text labels remain outside that structure.

For each coordinate axis, the selected finite locator is:

```text
origin + (to - from) * numerator / denominator
```

`origin`, `from` and `to` refer to actual earlier site rows. `origin` defaults
to `from`; an explicit origin also permits a shared displacement to be applied
to an existing template control. The numerator is a signed literal or a sample
of a bound parameter carrier. The denominator is a positive selected grade.
The native evaluator uses the existing affine equation read, truncates each
displacement toward zero, and preserves its exact source-basis remainder.
It does not silently increase precision to a hidden exact final drawing.

Only the page basis has literal root coordinates in these fixtures. Wall
corners are sampled in that frame. Window corners consume the wall frame;
their shoulders and arch consume the window frame. Later occurrences consume
the earlier window controls and a common wall-relative displacement. The
third occurrence continues that construction through the second occurrence.
Equal-looking controls with independent identities do not establish repetition.

Each finite line, quadratic or cubic refers to its actual ordered control-site
rows. The closing edge returns to the actual initial site. Coordinates may
coincide without creating a join. Same-rung references are permitted only after
the referenced site has been introduced; future references are rejected.

```text
earlier sites / exposed construction frame
  -> relative coordinate read + retained remainder
  -> new control sites and finite curves
  -> focus: selected, carried, introduced, correspondence
  -> transform application and complete next rung
  -> ordered timeline and retained guide context
```

`StructuralLadderSurface`, `FocusSurface`, `TransformSurface`, `RepeatSurface`
and `TimeStepSequence` supply the native construction spine. The exact reader
can execute a restored program without consulting its first execution's
geometry. Public program archives retain the reusable construction. Separate
full execution archives retain the richer witnesses and rung history locally.

## What changes between rungs

The church begins with a wall rectangle, a triangular roof approximation and a
tower box. Their sites guide the finished silhouette. The next rung creates one
rectangular window. Its straight upper profile is replaced with an arch, using
the rectangle as reference. Repetition creates three occurrences of that
construction. Door and tower details arrive last, while the window curves keep
their identity.

The star begins with a center and common reach. A real native closed circular
traversal is equally partitioned. The measured XY association of arbitrary
partition landmarks is an explicitly authored finite calibration: the native
circle fixture does not yet prove arbitrary trigonometric coordinates. Every
other selected radius is reduced by the shared inset parameter, then ordered
joins close the outline. Changing inset rebinds the existing program; changing
partition count selects another program variant. These are distinct changes.

The crescent starts with an oval envelope and a coarse closed wedge. An outer
curve and then an inner curve replace the rough spans while keeping the same
tips. Reversing the horizontal envelope retains the intended facing through
the actual locator references. The organic contour similarly replaces coarse
chords with finite curves, then changes only one selected bend.

Circles, ovals and rounded profiles in the displayed drawings are finite
Bézier approximations. No analytic-circle or automatic smooth-tangent claim is
inferred from their names. The recipes introduce rectangles, triangles, a
cross, oval guides, crescents and organic curves through their connected parts;
this is not a closed shape taxonomy.

## Conditions and visibility

The wall condition tests all selected curve controls against the actual convex
wall boundary. An inside control hull is a sufficient certificate that its
Bézier curve stays inside. A failed control-hull certificate alone is not a
proof that every corresponding curve leaves the wall. The deliberately bad
spacing trial also places window endpoints outside, making its failure visible.
Unsupported or non-convex boundary requests remain open.

The inspector separates three concerns:

1. The **rung** selects a complete construction result and its retained context.
2. **Guide visibility** hides, fades or retains old strokes for inspection. It
   changes no native reference, program or witness.
3. **Numeric resolution** is currently a fixed finite coordinate read. This
   checkpoint has no nonnumeric reconstruction mode or invented resolution dial.

There is no destructive forgetting operation here. Later adaptive memory could
deliberately compress or discard history, but would need an explicit retained
promise and loss surface. A hidden guide still participates as construction
evidence.

## Preparing later training

The independent audit exports 44 ordered transition examples to
`D:\_quickDraw\gojo-construction\transitions.ndjson`, also retained as a
[compressed transition set](results/construction/transitions.ndjson.gz). Each contains:

- an input rung with only sites and curves already introduced;
- the next instruction's new locators, references and required parameter bindings;
- the next native result and its condition reads, kept in a separate target;
- authored provenance, run identity and a family grouping for later splitting.

Two target transitions explicitly carry `failed-authored-condition`: they are
negative feedback examples, not accepted finished-window targets. Other outcome
reads distinguish satisfied selected conditions, open conditions and the absence
of selected conditions; absence of a check is not a general validity claim.

These are conditional construction examples, not demonstrations of recovering
unknown intention from a rough picture. Supplying the next instruction is a
different task from learning to propose it. No training or evaluation split is
claimed; whole recipe/parameter families should be separated before testing
generalization. The full program archive also contains future instructions and
must not be mistaken for a restricted model input.

Useful next experiments are to perturb the earlier frames, withhold selected
parameters or instructions, publish coarser positional distinctions with
explicit loss, and compare competing completions against retained conditions.
The original imperfect drawing and any idealized proposal must remain separate
sources with correspondence. Semantic expectations such as a window belonging
inside a wall are authored premises here, not trained priors.

## Reproduction and evidence

This checkpoint passes 5,374 .NET tests across twelve projects, including twelve
focused construction cases, and 77 Python tests. The independent source audit
checks 5,358 assertions across all twelve runs. All 56 rungs and twelve program
downloads pass live browser checks. See the retained
[source audit](results/construction/verification.json) and
[publication checkpoint](results/construction/publication-summary.json).

From the repository root:

```powershell
python experiments/quickDraw/tools/construction_recipes.py --output D:\_quickDraw\gojo-construction
dotnet run --project experiments/quickDraw -- --construct --input D:\_quickDraw\gojo-construction\programs.ndjson --output D:\_quickDraw\gojo-construction\native
python experiments/quickDraw/tools/verify_construction_ladders.py --root D:\_quickDraw\gojo-construction
python experiments/quickDraw/tools/inspect_construction_ladders.py --input D:\_quickDraw\gojo-construction\native --output D:\_quickDraw\gojo-construction\review.html
python experiments/quickDraw/tools/inspect_construction_ladders.py --input D:\_quickDraw\gojo-construction\native --public --output experiments/quickDraw/results/construction/public/index.html
node Visualizations/Gojo.Demos.Web/tools/build-quickdraw.mjs
```

The native tests cover restored replay, parameter propagation, causal ordering,
identity, replacement accounting, repetition and failed/open containment.
The independent Python audit uses rational arithmetic to check generated
geometry, exact translated occurrences, mirrored facing and transition inputs.
The browser reads only restored native exports and tests guide controls,
dependencies, rung navigation and downloads. Retained summaries and the source
manifest are under `results/construction`; larger full execution archives and
uncompressed transition data remain in the authorized Quick Draw data folder.
