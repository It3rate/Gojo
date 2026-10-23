"""Publication boundary: retain the selected archives and keep local paths private."""
import gzip
import json
from pathlib import Path
import tempfile
import unittest

from inspect_line_art import build


def source(folder, key, archive_bytes=b'{"retained":"graph"}', notes=""):
    folder.mkdir(parents=True)
    (folder / "001.gojo.json").write_bytes(archive_bytes)
    scene = {"id": key, "title": key, "notes": notes, "archive": "001.gojo.json",
             "nativeRestoreVerified": True, "coordinateQuantum": 1,
             "analysis": {"edges": [], "sites": [], "regions": []}}
    (folder / "inspector.ndjson").write_text(json.dumps(scene)+"\n", encoding="utf-8")


class InspectorPublicationTests(unittest.TestCase):
    def test_equal_archive_filenames_from_distinct_inputs_keep_their_own_bytes(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source(root / "authored", "alphabet", b'{"identity":"first"}')
            source(root / "imported", "church", b'{"identity":"second"}')
            output = root / "public/index.html"
            build([root / "authored", root / "imported"], output, public=True, default_scene="alphabet")
            selected = json.loads((output.parent / "public-files.json").read_text())["files"]
            self.assertEqual(selected, ["index.html", "archives/alphabet.gojo.json.gz", "archives/church.gojo.json.gz"])
            self.assertEqual(gzip.decompress((output.parent / selected[1]).read_bytes()), b'{"identity":"first"}')
            self.assertEqual(gzip.decompress((output.parent / selected[2]).read_bytes()), b'{"identity":"second"}')
            html = output.read_text(encoding="utf-8")
            self.assertNotIn("file:///", html)
            self.assertNotIn(str(root), html)
            self.assertIn('"defaultScene": "alphabet"', html)

    def test_local_provenance_in_a_note_is_refused_for_publication(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source(root / "input", "drawing", notes="C:/private/source.ndjson")
            with self.assertRaisesRegex(ValueError, "machine-local"):
                build([root / "input"], root / "public/index.html", public=True)
            self.assertFalse((root / "public/index.html").exists())

    def test_source_labels_cannot_end_the_embedded_script(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source(root / "input", "drawing", notes="</script><script>untrusted()</script>")
            build([root / "input"], root / "review.html")
            html = (root / "review.html").read_text(encoding="utf-8")
            self.assertNotIn("</script><script>untrusted()", html)
            self.assertIn(r"\u003c/script>", html)

    def test_default_scene_must_exist_and_identifiers_cannot_escape_the_package(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source(root / "input", "../outside")
            with self.assertRaisesRegex(ValueError, "safe filename"):
                build([root / "input"], root / "public/index.html", public=True)
            with self.assertRaisesRegex(ValueError, "Default scene"):
                build([root / "input"], root / "public/index.html", default_scene="missing")


if __name__ == "__main__":
    unittest.main()
