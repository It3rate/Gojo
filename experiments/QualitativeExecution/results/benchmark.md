# Qualitative execution diagnostics

Run: 2026-10-15T21:04:18.3756246+00:00; .NET 10.0.12; X64.
Inputs: 64 prepared closed qualitative support pairs; 2048 repeated operations per execution mode.
Native source law: 90 relation nodes, 21 placeholder instances, 0 carriers, 0 carrier locations.

| Mode | Operations | ns/op | Allocated bytes/op | Checksum | New witness per read |
|---|---:|---:|---:|---:|---|
| native interpretation + witness + result encoding | 2048 | 64,449.1 | 59,019.2 | -4425838155167264751 | yes |
| compiled execution + witness + result encoding | 2048 | 4,026.1 | 2,935.5 | -4425838155167264751 | yes |
| compiled masks + current-law validation | 2048 | 693.8 | 0.0 | -4425838155167264751 | no |
| capture native law into derived table | 25 | 312,804.0 | 301,648.0 | 9149324277192176428 | no |

- The source fixture uses relation/placeholder identities and supplied detector categories, with no measured numeric operands.
- All 64 closed support pairs are reused in each execution mode; prepared inputs and capture are outside repeated execution timing.
- Native and compiled witness modes materialize read/result/residue/provenance graphs and encode the resulting support for the checksum.
- Compute-only masks omit new witness graphs, but validate the captured law on every call and retain SourceLaw for interpretation.
- Capture is reported separately and interprets the native atomic rows to derive its table.
- Thread allocation counts are managed allocations on the calling thread; these are one-process diagnostic timings, not stable performance guarantees.
- There is no eager numeric baseline: the detector fixture does not expose numeric samples, and this run does not claim a numerical-arithmetic speedup.
