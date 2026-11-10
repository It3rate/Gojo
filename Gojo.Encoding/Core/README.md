# Shared encoding core

This folder is the reusable home for the encoding mechanisms developed in the
primitive and qualitative experiments. Start here when building another
experiment: use the existing storage, connection, law, flow and construction
mechanisms where they fit, then keep genuinely new work local until it is ready
to join the core.

The first promotion, on 10 November 2026, brings together three groups. They
compile into **`Gojo.Encoding`**, with no dependency on experiment projects.
Existing experiments retain their original code and results.

| Area | What can be reused | Entry point |
| --- | --- | --- |
| Packed encoding | ODV words, positional constant buckets, owner-local bindings and ports, tuple laws, uncertain supports, Boolean token passages, ordered routes, family templates and codecs | [Packed](Packed/README.md) |
| Native relation encoding | Finite laws and compiled plans, observation and refinement, choices, packet transport, conditional passages, timing, repetition, ordering, motion, curves, bands and periodic cells | [Native](Native/README.md) |
| Composition | Exposed path ports and permission-controlled routing; chronological construction reads that distinguish a continuous bend, later attachment, outward branch and arriving stop | [Composition](Composition/README.md) |

These are implemented, tested mechanisms with stated contracts. Their inclusion
does not make every modeling choice a universal Gojo rule. The numeric kernel
remains in `Gojo/Core`; this is a core **of reusable encodings** over and alongside
that kernel.

## One collection, distinct representations

Packed records and native relation graphs are not silently interchangeable.
A packed row addresses local positions and executes a declared bitmap law.
A native law is connected relation structure with actual participant identity,
open or closed support, and retained evidence. Both are useful, and some small
laws have an explicit correspondence, but a general compiler between them is
still unfinished work.

[Interoperability and existing foundations](INTEROPERABILITY.md) defines the
first bounded comparison: all sixteen two-input Boolean laws over all closed
input supports. It also indexes the existing kernel implementations for
selection, ladders, matching, repetition, witnesses and numeric graph transport.
Those implementations remain in place rather than acquiring another copy here.

The conventions that matter most are:

- **ODV is still three bits.** `V=0` means undetectable at the selected
  resolution; `V=1` means detectable. Neither is a selection bit, and detectable
  does not mean the quantity one. An undetectable measure is not an exact
  additive identity. Historical coarse arithmetic that assumed otherwise was
  not promoted.
- **Selection, possible answers and stored values are separate.** An empty
  support mask, a mask containing the word `000`, and an unselected source have
  different meanings. Open native support has an unaccounted tail that cannot
  be discarded in a closed packed mask.
- **A port exposes a local surface.** Packed ports resolve through an
  owner-local binding; native ports are actual participants in a connected
  contract. A landmark does not automatically become a port. Exposing a site
  does not by itself grant passage, calibration, permission or capacity.
- **Discrete tokens are an explicit protocol.** The packed Boolean passage
  machinery opts into occupied/empty slots. This does not reinterpret an
  undetectable measured quantity as an empty physical vessel. Native packet
  transport preserves actual packet ownership through accepted moves.
- **A frame flip needs a declared interpretation.** The bit operation is
  available, but preservation of meaning across orientations depends on the
  chosen frame correspondence and calibration.
- **Layout limits belong to implementations.** Fixed mask widths and helper
  allocation guards do not impose a maximum size on a concept or owner.

The port grammars and concrete examples are documented within each group. There
is deliberately no new universal header, string identifier or global table
that every structure must carry.

## Using the core

Reference `Gojo.Encoding/Gojo.Encoding.csproj`, then import the relevant namespace:

```csharp
using Gojo.Encoding.Core.Packed;
using Gojo.Encoding.Core.Native;
using Gojo.Encoding.Core.Composition;
```

Use only the families needed by the experiment. The
[native packet example](Native/README.md#small-example) and
[composition tests](../../Tests/Gojo.Encoding.Core.Tests/CompositionTests.cs)
show complete local connections without a glyph, ornament, or classifier making
the operational decision. The [core tests](../../Tests/Gojo.Encoding.Core.Tests)
reference this library directly, never an experiment assembly.

```powershell
dotnet test Tests/Gojo.Encoding.Core.Tests/Gojo.Encoding.Core.Tests.csproj
```

For whole-repository verification, use `dotnet test Gojo.slnx`.

## How experimental work joins the core

1. Start with the existing core contract. Prototype additional behavior in the
   experiment when its semantics are not settled.
2. Promote a useful mechanism with its actual grammar, builders/readers and
   focused tests. Remove dependencies on example names, renderers, scores,
   corpora and experiment-specific policies.
3. Document source provenance, legal operations, uncertainty and failure states,
   implementation limits, and what the mechanism does not establish. Keep
   source meaning separate from a derived acceleration layout.
4. Test unresolved and invalid cases as well as successful reads. For serialized
   structures, check correspondence and sharing after transport; for material
   passages, check ownership and accepted/blocked/remainder accounting.
5. Prefer the shared implementation in subsequent experiments. Migrate older
   experiments when useful, with explicit compatibility checks. Their retained
   copies are historical implementations, not automatically synchronized forks.

The group READMEs record the source files and any changes made during promotion.
This is a practical shared starting point, not a declaration that every
experiment is finished or suitable for the core.

## Still outside this promotion

Domain corpora, ornament quality metrics, classifiers, training/search policies,
learned preferences, renderers, galleries, and experimental geometric receivers
remain with their experiments. The general workspace runtime, automatic
crystallization, and universal native-to-packed lowering remain incomplete.
The [plane-law proposal](../../Gojo/docs/GojoEncodingConsolidation-2026-11.md)
and `Gojo.EncodingB` are exploratory and have not been adopted as the common
substrate by this consolidation.

This organization follows the [canonical prompt](../../Gojo/docs/GojoSystemPrompt.md).
The prompt and current tests govern semantics; this inventory is a reuse guide.
