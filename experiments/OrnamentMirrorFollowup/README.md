# Which marks should be mirrored?

Primary champion 2282 learned to add a stroke opposite an occupied site. When an accident added a new mark, it copied the accident too. This local follow-up gave the reader one extra piece of evidence: whether the corresponding source owner had authorized that mark. Both worlds start with precisely the same visible ink. Only authorship differs. The actual computation uses native bit laws, a local port mapping and a stocked passage.

The [protocol](Protocol.md) was frozen against the primary report before training. A separate global lock covered all three follow-up finalist sets before opening the reserve. This project leaves the original campaign executable unchanged.

## What was measured

- Three source arrangements each tried all 256 two-table Boolean programs. The resulting 768 paid candidates include the same program bytes tested under different bindings. There were 294,912 training executions and 4,608 validation executions.
- The reserve used 48 parent recipes, two views, two authorship conditions, nine machine/control arms and two resource schedules. This gives 3,456 executions. The ample scores below have 192 worlds per arm.
- The bound winner recovered 192/192 whole outlines with no unwanted or missed marks. The current-ink winner recovered 96/192: all 96 authorized worlds and none of the accidentally marked worlds. It introduced 96 extra counterparts. Idle recovered no whole outlines and left 1,188 required sites missing.
- Rebinding the same winning program one site off produced 721 unnecessary marks and missed 793 genuine counterparts. Cutting its wire left 1,188 genuine sites missing.
- With the original scarce budgets, the bound winner recovered 34/192 whole outlines, still added no unnecessary marks, and missed 618 required sites. The current-ink winner recovered 14/192, with 50 unnecessary and 658 missing.
- The six preregistered assessments were supported across all 48 parent recipes. Four were comparative predictions. Two were instrument controls. The measured training/validation compute took about 0.42 seconds; the assay loop and CSV construction took about 0.11 seconds locally, apart from startup, compilation, reading and final publication.

The trained bound genome is `0100114A006204`. Each owner contains one NOR row. The sensor reads its bound counterpart and the current local site. The writer combines the resulting pulse with its local occupancy. The whole connection admits stock when an authorized counterpart exists and the local site is empty. The same behavior can be constructed with the primary 2282 writer and one rebound sensor input.

The author label, correspondence, grammar, goal and evaluation were supplied. With identical visible worlds a current-only reader has no evidence about which member of a pair was authorized. The exhaustive training result demonstrates an executable use of the supplied reference. It does not show discovery of that source, a new gate family, or broad ornamental generalization.

## Reproduce

Run from the repository root. For a new run, use a new empty directory. Preserve the primary report named in the protocol.

```powershell
dotnet build experiments/OrnamentMirrorFollowup -c Release
dotnet run --project experiments/OrnamentMirrorFollowup -c Release --no-build -- freeze artifacts/local-ornament/mirror-reproduction
dotnet run --project experiments/OrnamentMirrorFollowup -c Release --no-build -- train artifacts/local-ornament/mirror-reproduction
dotnet run --project experiments/OrnamentMirrorFollowup -c Release --no-build -- verify artifacts/local-ornament/mirror-reproduction
```

Run `assay` only after sealing the intended cross-experiment winner set. A fresh process can subsequently read just the prepared public inputs and packed programs, without loading the source owner, training memory, or critic. Its 36 replay results matched the saved expected file byte-for-byte in the recorded run.

```powershell
dotnet run --project experiments/OrnamentMirrorFollowup -c Release --no-build -- assay artifacts/local-ornament/mirror-reproduction
dotnet run --project experiments/OrnamentMirrorFollowup -c Release --no-build -- execute artifacts/local-ornament/mirror-reproduction/reserve/replay-inputs.json artifacts/local-ornament/mirror-reproduction/reserve/replay-actual.json
Get-FileHash artifacts/local-ornament/mirror-reproduction/reserve/replay-expected.json, artifacts/local-ornament/mirror-reproduction/reserve/replay-actual.json
```

Raw outputs from the sealed run are under `artifacts/local-ornament/mirror-followup/`. The protocol SHA-256 is `961B7050F59C3F9C6FE7EFB6CAEFA74FC8B2FEF31CEC4842C560705892835BF3`. The winner-lock SHA-256 is `76707F639D0C36870118C7380ED5E8FEACF541C0AD4D64E744E946E352C9C3BE`.
