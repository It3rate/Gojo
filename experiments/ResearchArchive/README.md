# Large research result archive

Source code and raw research results are tracked in ordinary Git. The one
oversized result, `artifacts/primitive-forager/results.json`, is stored as
`primitive-forager-results.json.gz` beside `large-results.json`. Build caches
are excluded. The manifest records the original and compressed byte counts
and SHA-256 hashes.

From the repository root, using Python 3.10 or later:

```powershell
python experiments/ResearchArchive/Restore-LargeResults.py --verify
python experiments/ResearchArchive/Restore-LargeResults.py --restore
```

No arguments also verifies. Verification streams the compressed archive and
its decompressed contents, checking both sizes and hashes without creating an
original file. Restore creates only missing originals. It keeps an identical
existing file and refuses a differing existing file. Errors return a nonzero
exit status.

To restore into another checkout or an empty test directory:

```powershell
python experiments/ResearchArchive/Restore-LargeResults.py --restore --destination D:/temp/gojo-archive-check
```

Archives are always read from this checkout. Targets use manifest-relative
paths under the destination. Absolute paths, traversal, and paths escaping
through symlinks are refused. Restore writes and verifies a temporary file in
the target directory, then atomically publishes it without overwriting another
file. This requires hard-link support, available on NTFS and usual Unix file
systems. A filesystem without that support fails safely. The temporary file
is removed after completion or an error.
