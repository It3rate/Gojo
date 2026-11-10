# Interoperability and existing foundations

The packed and native APIs can express some of the same small laws. That does
not make every packed record a native structure, or every native structure a
bitmap. A correspondence must say which participants the codes address and
which information the conversion retains.

The first conformance fixture asks a deliberately small question: **do both
representations give the same possible answers to a declared two-state law?**
It compares all 16 Boolean truth tables against all 16 pairs of closed input
supports, including empty supports and uncertain `{false, true}` supports.
That gives 256 comparisons. It also checks selected laws after native graph
transport and checks boundaries where a conversion would lose information.
The fixture is [InteropTests.cs](../../Tests/Gojo.Encoding.Core.Tests/InteropTests.cs).

## The explicit finite contract

Two native participants are placed in an ordered, closed domain. In this test,
the first is assigned packed code `000`, and the second code `001`. The code
assignment belongs to this adapter; it is not inferred from labels, carrier
values, or global identifiers. These are truth-state alternatives for an
explicit finite law, not measured magnitudes or an inventory of material.

| Packed support | Native support | Meaning in this fixture |
| --- | --- | --- |
| `00000000` | Closed empty scope | No admitted alternative |
| `00000001` | Closed first participant | False |
| `00000010` | Closed second participant | True |
| `00000011` | Closed both participants | Either alternative |

The mask is a set of allowed codes: it is neither another ODV value nor a
selection bit on a source. In particular, the empty mask is different from
the mask admitting the word `000`. The fixture does not interpret ODV `V=0`
as nonexistence, unselection, or exact measured zero.

Each row is compiled both into `Packed.BitRelation` and into
`Native.FiniteLaw`. The reader considers every admitted pair of input
alternatives and retains the possible output alternatives. It does not choose
an actual outcome, execute a transfer, or mutate the inputs. The native read
also retains consumed rows and unresolved evidence; the comparison checks
that unresolved evidence is empty for this fully declared contract.

The adapter stays in the tests. There is no general conversion API, and the
fixture does not establish a universal equivalence between the two families.

## What this contract cannot compress away

- **Open support:** a native open scope has an unaccounted tail. A plain closed
  mask cannot distinguish that tail from a complete set. The test adapter
  refuses open scopes, even when all currently visible entries have codes.
- **Unmapped participants:** a newly encountered native participant is not
  silently removed or assigned an existing code. The adapter refuses it.
- **Shared-input correlation:** equal support masks do not say whether two
  input roles refer to one source. The ordinary finite-law evaluation and this
  adapter use Cartesian input alternatives. `BitRelation` additionally has an
  explicit shared-reference overload. For uncertain XOR inputs, Cartesian
  alternatives allow false or true, while one shared input XOR itself allows
  only false. The tests preserve this difference instead of claiming both
  calls mean the same thing.
- **Provenance and identity:** matching output masks does not make histories,
  suppliers, identities, or consumed rows interchangeable. A mask-only result
  omits that evidence.
- **Capacity:** three packed roles and eight codes per role bound this bitmap
  format. They do not bound a Gojo concept, owner, workspace, or native domain.
- **Material behavior:** Boolean conformance does not certify transfer,
  capacity, timing, or settlement. Those require their own connected contracts
  and accounting tests.

The serialized fixture carries its law and its two mapped participants as
explicit roots. Restoring it creates a new local world with shared references
preserved inside that world. It does not require persistent runtime IDs or
recover code assignments from names.

## Reuse these implementations rather than duplicate them

The new core folder consolidates reusable encodings. It does not replace the
numeric kernel or the shared relation builders already in the repository.

| Concern | Existing implementation |
| --- | --- |
| Native numeric graph transport | [GojoNumericFormat](../../Gojo/Core/Serialization/GojoNumericFormat.cs) and [format notes](../../Gojo/docs/GojoNumericFormat.md). It preserves ordered references, shared sites, attachments, and operative graph state. |
| Compact JSON graph interchange | [GojoCompactArchive](../Structures/GojoCompactArchive.cs) and [GojoStructureCompactJson](../Structures/GojoStructureCompactJson.cs) adapt the existing graph snapshot for JSON. Display names do not determine meaning. |
| Selection and correspondence | [FocusSurface](../../Gojo/Structures/FocusSurface.cs) |
| Structural and numeric refinement | [StructuralLadderSurface](../../Gojo/Structures/StructuralLadderSurface.cs), [ResolutionLadder](../../Gojo/Structures/ResolutionLadder.cs) |
| Read evidence and finite publication | [ReadWitness](../../Gojo/Structures/ReadWitness.cs), [FiniteReadSurface](../../Gojo/Structures/FiniteReadSurface.cs) |
| Changes, dependencies, and replay | [TransformSurface](../../Gojo/Structures/TransformSurface.cs) |
| Repetition and counting | [RepeatSurface](../../Gojo/Structures/RepeatSurface.cs), [RepeatController](../../Gojo/Patterns/RepeatController.cs) |
| Patterns, matching, and open/closed scopes | [StructuralPattern](../../Gojo/Patterns/StructuralPattern.cs), [StructuralConstraint](../../Gojo/Patterns/StructuralConstraint.cs) |
| Choice and support | [ScopedChoice](../../Gojo/Structures/ScopedChoice.cs), [SupportDecisionSurface](../../Gojo/Structures/SupportDecisionSurface.cs) |

The general [workspace contract](../../Gojo/Workspace/README.md) remains
documented direction with narrow implemented relatives; this consolidation
does not turn it into a complete workspace runtime.

[EncodingC](../../Gojo.EncodingC/README.md) replaced EncodingB and remains
exploratory. It is a lean bit-level substrate. Ports describe nothing. Copying
and moving show in a connection's law. Nothing is identified by a stored name.
Structures have any size. It borrows some ideas from this core but no code.
Its experiments provide useful evidence for future promotion, but this core
does not depend on that project or claim that its substrate is already
interchangeable with the native kernel.
