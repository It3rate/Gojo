# Colour as an intended ordinal relation

This experiment adds a small reusable colour-intention contract to the
qualitative ornament work. It is implemented in
[`OrnamentColourIntent.cs`](../../experiments/QualitativeFrieze/Composition/OrnamentColourIntent.cs).
It supplies structural declarations and local evidence reads, rather than a
new colour model or a claim that the current program understands human colour
perception.

## Historical guidance

Jones's *Grammar of Ornament* has **37 propositions** in the consulted edition.
Proposition 32 concerns separating ornament and ground; it is not the last
rule. The [Smithsonian's original 1856 edition](https://library.si.edu/digital-library/book/grammarornament00jone)
is the source used by the earlier colour experiments. The
[original French edition's propositions](https://fr.wikisource.org/wiki/Grammaire_de_l_ornement/Principes_g%C3%A9n%C3%A9raux)
were checked again for this extension.

The useful historical suggestions are to use colour to distinguish forms and
parts (14), support small strong accents with larger quieter masses (16 and
19), preserve a coherent distant impression (22), and use separating edges
between competing coloured masses (28–34). These become revisable intentions
attached to actual parts of a design. Jones's proposition 21 assigns advancing
and retiring colours to **moulded surfaces**. Applying a comparable convention
to a flat ornament is our chosen visual interpretation. His pigment ratios
and his prescription for a neutral distant impression are historical design
advice, not calibrated sRGB laws. [Jones, propositions 14–34](https://fr.wikisource.org/wiki/Grammaire_de_l_ornement/Principes_g%C3%A9n%C3%A9raux).

The earlier [source notes](../../experiments/QualitativeFrieze/research/jones-colour-notes.md)
also identify Jones's interwoven large, intermediate and fine design levels.
These remain useful guides to focus selection.

## Native shape before helper names

The contract uses the existing `OrdinalReferenceFrame`; its finite law and
opposed/equal/forward alternatives remain connected. It adds no colour enums
or operation-name tags:

```text
selection = source -> Closed(actual selected participants)
comparison = (subject -> reference) -> ordinal frame
claim = comparison -> desired ordinal support
contrary = comparison -> complementary ordinal support
question = ((claim -> contrary) -> Closed(claim, contrary)) -> context
intent = selection -> (comparison -> (desired support -> question))

observation = existing ordinal publication for source, subject, reference
judgement = intent -> (observation ->
             (supported -> (contradicted -> unresolved)))
```

Each result scope contains the actual intent identity. The same intent returned
through multiple observations does not acquire independent votes. Conflicting
resolved observations of that identity normalize to unresolved in the existing
`CompatibilityEvidence` aggregate channel. Its calibration is explicitly
selected, not learned from these pictures.

`Declare` does not assert that the desired order already holds. `Assess` requires
the actual source, selected subject/reference and ordinal frame. A copied role,
foreign source, missing publication, open support or mixed support remains
unresolved. A support entirely within the desired alternatives supports the
intent; disjoint support contradicts it. The source's selected surface is an
exposed contract: this helper does not inspect private ancestry to certify that
the publisher selected every relevant colour region.

`ObserveAnswer` can ask either the claim or its contrary. Asking the contrary
and denying it preserves the positive claim while retaining the different
question orientation. A declaration's desired condition, the observed support,
the binary response and the aggregate likelihood remain distinct.

## Examples of contextual readings

The comparison's endpoints may be actual selected bands, entire layer scopes,
or exposed compound reading ports. Useful constructions include:

| Intended read | Subject and reference ports | Desired order |
| --- | --- | --- |
| Detail receives stronger attention | Detail/ground contrast versus frame/ground contrast | Higher |
| Supporting frame recedes | Frame/ground contrast versus main motif/ground contrast | Lower |
| An edge separates two fills | Edge/fill contrast versus a selected minimum-separation reference | Higher or equal |
| A family stays coherent | Two repeat instances under the chosen family-similarity observer | Equal |
| A repeated alternation is retained | A pair of phase-related colour roles under the chosen difference observer | Higher |

These rows explain possible uses. The helper does not infer the observer law
from a label such as "recedes" or "family". A meaningful application connects
its observer basis, the selected source parts, the published reading and its
provenance. An undefined observer yields an open publication, not a synthetic
successful score.

Actual paint strings and any numerical display calculations belong to the
presentation adapter. A display adapter may publish a coarse ordinal contrast
observation with its calibration retained; that is evidence about the selected
display metric, not proof of perceptual salience, chromatic harmony, area
balance or legibility at all sizes. Width, area, edge presence, texture and
context can change the visual result without changing a simple contrast read.

## What is established and what remains open

Implemented and tested: identity-aware selection, explicit expected support,
supported/contradicted/unresolved assessment, question orientation, native
provenance retention, duplicate-check normalization and reuse of a selected
compatibility calibration. The reader rechecks native result scopes rather
than trusting an editable diagnostic boolean.

Still exploratory: which appearance observations deserve to be published,
whether attention should combine contrast with occupied area and width, how
display conditions revise those observations, and how learned preferences
should revise an intention or calibration. No new probability acquisition,
perceptual model, autonomous palette optimizer or universal Jones law is claimed.

Focused verification:

```powershell
dotnet test Tests/QualitativeExecution.Tests/QualitativeExecution.Tests.csproj --filter FullyQualifiedName~OrnamentColourIntentTests
```
