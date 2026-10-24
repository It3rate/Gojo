"""Publication checks for retained identity, causal reads and native downloads."""
import base64
import copy
import gzip
import json
from pathlib import Path
import re
import tempfile
import unittest

from inspect_construction_ladders import build, group_resolutions, load_exports, normalize


def fixture(key="fixture"):
    sites = [{"id": "a", "x": 0, "y": 0, "at": 0, "dependencies": []},
             {"id": "b", "x": 100, "y": 0, "at": 0, "dependencies": []}]
    copied = [{"id": "c", "x": 0, "y": 0, "at": 1, "dependencies": ["a"]},
              {"id": "d", "x": 100, "y": 0, "at": 1, "dependencies": ["b"]}]
    edge = {"id": "base", "label": "Base", "at": 0, "controls": [[0, 0], [100, 0]],
            "controlSiteIds": ["a", "b"], "from": "a", "to": "b", "guide": False,
            "introduced": True, "dependencies": []}
    old = {**copy.deepcopy(edge), "guide": True, "introduced": False}
    new = {**copy.deepcopy(edge), "id": "copy", "label": "Copy", "at": 1,
           "controlSiteIds": ["c", "d"], "from": "c", "to": "d",
           "dependencies": [{"rungId": "r0", "edgeId": "base", "siteId": "a", "sameRung": False},
                            {"rungId": "r0", "edgeId": "base", "siteId": "b", "sameRung": False}]}
    rung = {"id": "r0", "title": "First", "note": "", "sites": sites,
            "edges": [edge], "constraints": [], "verifiedNativeRestore": True,
            "verifiedNativeReplay": True}
    return {"id": key, "family": "lines", "title": "Two constructions", "description": "",
            "variantLabel": key, "viewBox": [0, 0, 120, 120], "parameters": [],
            "archive": "001.construction.gojo.json", "programArchive": "001.program.gojo.json",
            "nativeRestoreVerified": True, "nativeReplayVerified": True,
            "rungs": [rung, {**copy.deepcopy(rung), "id": "r1", "title": "Second",
                              "sites": copy.deepcopy(sites) + copied, "edges": [old, new]}]}


def write_source(folder, run, program=b'{"program":"actual native selection"}'):
    folder.mkdir(parents=True)
    (folder / "001.construction.gojo.json").write_bytes(b'{"execution":"retained witnesses"}')
    (folder / "001.program.gojo.json").write_bytes(program)
    (folder / "inspector.ndjson").write_text(json.dumps(run) + "\n", encoding="utf-8")


def embedded(output):
    match = re.search(r'<script id="construction-data" type="application/json">(.*?)</script>',
                      output.read_text(encoding="utf-8"), flags=re.S)
    data = json.loads(match.group(1))
    if data.get("encoding") == "gzip-base64":
        data = json.loads(gzip.decompress(base64.b64decode(data["payload"])))
    return data


class ConstructionPublicationTests(unittest.TestCase):
    def test_resolution_selection_groups_native_reads_without_recomputing_coordinates(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            fine, coarse = fixture("first--exact"), fixture("first--q8")
            for rung in coarse["rungs"]:
                for site in rung["sites"]:
                    site["x"] *= 2
                for edge in rung["edges"]:
                    for point in edge["controls"]:
                        point[0] *= 2
            write_source(root / "fine", fine)
            write_source(root / "coarse", coarse, b'{"coarse":"program without fine source"}')
            manifest = {"levels": [{"id": "exact", "label": "Original placement"},
                                     {"id": "q8", "label": "Coarser"}],
                        "programs": [{"id": run["id"], "baseId": "first", "levelId": level,
                                      "label": "First drawing", "changedCoordinates": count}
                                     for run, level, count in ((fine, "exact", 0), (coarse, "q8", 2))],
                        "qualification": "Numeric placement with retained structural references."}
            file = root / "resolution.json"
            file.write_text(json.dumps(manifest), encoding="utf-8")
            for public in (False, True):
                output = root / str(public) / "index.html"
                summary = build([root / "coarse", root / "fine"], output, public=public, resolution_manifest=file)
                data = embedded(output)
                self.assertEqual((summary["variants"], summary["programs"], summary["rungs"]), (1, 2, 4))
                design = data["scenes"][0]["variants"][0]
                self.assertEqual((design["id"], design["label"]), ("first", "First drawing"))
                reads = design["resolutions"]
                self.assertEqual([v["id"] for v in reads], ["first--exact", "first--q8"])
                self.assertEqual([v["analysis"]["edges"][0]["controls"][1][0] for v in reads], [100, 200])
                self.assertEqual(reads[1]["resolutionMetrics"]["changedCoordinates"], 2)
                self.assertIn("first--q8", reads[1]["archivePath"] if public else reads[1]["id"])
                if public:
                    self.assertEqual(gzip.decompress((output.parent / reads[1]["archivePath"]).read_bytes()),
                                     b'{"coarse":"program without fine source"}')

    def test_resolution_correspondence_cannot_invent_missing_reads_or_duplicate_a_level(self):
        variants = [normalize(fixture("a--exact")), normalize(fixture("a--coarse"))]
        family = {"lines": {"id": "lines", "variants": variants}}
        manifest = {"levels": [{"id": "exact"}, {"id": "coarse"}], "programs": [
            {"id": "a--exact", "baseId": "a", "levelId": "exact"},
            {"id": "a--coarse", "baseId": "a", "levelId": "coarse"}]}
        missing = copy.deepcopy(manifest)
        missing["programs"].pop()
        with self.assertRaisesRegex(ValueError, "exactly the native"):
            group_resolutions(copy.deepcopy(family), missing)
        duplicate = copy.deepcopy(manifest)
        duplicate["programs"][1]["levelId"] = "exact"
        with self.assertRaisesRegex(ValueError, "exactly one native"):
            group_resolutions(copy.deepcopy(family), duplicate)
        wrong_steps = copy.deepcopy(family)
        wrong_steps["lines"]["variants"][1]["rungs"].pop()
        with self.assertRaisesRegex(ValueError, "corresponding construction steps"):
            group_resolutions(wrong_steps, manifest)

    def test_coincident_sites_and_edges_keep_distinct_identities_and_actual_dependencies(self):
        result = normalize(fixture())
        self.assertEqual([s["id"] for s in result["analysis"]["sites"]], ["a", "b", "c", "d"])
        self.assertEqual([e["id"] for e in result["analysis"]["edges"]], ["base", "copy"])
        self.assertEqual(result["rungs"][1]["guideEdgeIds"], ["base"])
        self.assertEqual(result["rungs"][1]["visibleEdgeIds"], ["copy"])
        self.assertEqual(result["rungs"][1]["dependencies"][0]["sourceSiteIds"], ["a", "b"])

    def test_changed_site_or_curve_under_one_identity_is_rejected(self):
        run = fixture()
        run["rungs"][1]["sites"][0]["x"] = 1
        with self.assertRaisesRegex(ValueError, "site identity changed"):
            normalize(run)
        run = fixture()
        run["rungs"][1]["edges"][0]["controls"][0][0] = 1
        with self.assertRaisesRegex(ValueError, "edge identity changed"):
            normalize(run)

    def test_future_rung_or_unavailable_site_cannot_be_published_as_a_dependency(self):
        for field, value, error in (("rungId", "r2", "later rung"),
                                    ("siteId", "missing", "unavailable native site")):
            with self.subTest(field=field):
                run = fixture()
                run["rungs"][1]["edges"][1]["dependencies"][0][field] = value
                with self.assertRaisesRegex(ValueError, error):
                    normalize(run)

    def test_run_and_each_rung_must_have_native_restore_and_replay_verification(self):
        for field in ("nativeRestoreVerified", "nativeReplayVerified"):
            with self.subTest(field=field), tempfile.TemporaryDirectory() as temp:
                run = fixture()
                run[field] = False
                write_source(Path(temp) / "input", run)
                with self.assertRaisesRegex(ValueError, "restored and replayed"):
                    load_exports([Path(temp) / "input"])
        run = fixture()
        run["rungs"][1]["verifiedNativeReplay"] = False
        with self.assertRaisesRegex(ValueError, "Every published rung"):
            normalize(run)

    def test_public_downloads_preserve_the_selected_program_bytes_and_are_deterministic(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            first, second = b'{"program":"first"}', b'{"program":"second"}'
            write_source(root / "first", fixture("first"), first)
            write_source(root / "second", fixture("second"), second)
            outputs = [root / "public-a/index.html", root / "public-b/index.html"]
            for output in outputs:
                build([root / "first", root / "second"], output, public=True)
            self.assertEqual(outputs[0].read_bytes(), outputs[1].read_bytes())
            manifest = json.loads((outputs[0].parent / "public-files.json").read_text())
            self.assertEqual(manifest["files"], ["index.html", "archives/first.gojo.json.gz", "archives/second.gojo.json.gz"])
            for key, original in (("first", first), ("second", second)):
                relative = f"archives/{key}.gojo.json.gz"
                packed = (outputs[0].parent / relative).read_bytes()
                self.assertEqual(gzip.decompress(packed), original)
                self.assertEqual(packed, (outputs[1].parent / relative).read_bytes())
            data = embedded(outputs[0])
            self.assertEqual(data["scenes"][0]["variants"][0]["archiveKind"], "program")
            self.assertNotIn("file:///", json.dumps(data))
            self.assertNotIn(str(root), json.dumps(data))

    def test_representative_download_selection_is_explicit_for_other_variants(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write_source(root / "first", fixture("first"))
            write_source(root / "second", fixture("second"))
            output = root / "public/index.html"
            build([root / "first", root / "second"], output, public=True, public_archives="representative")
            variants = embedded(output)["scenes"][0]["variants"]
            self.assertIn("archivePath", variants[0])
            self.assertNotIn("archivePath", variants[1])
            self.assertEqual(json.loads((output.parent / "public-files.json").read_text())["files"],
                             ["index.html", "archives/first.gojo.json.gz"])

    def test_local_review_links_to_the_executable_program(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write_source(root / "input", fixture())
            output = root / "review.html"
            build([root / "input"], output)
            variant = embedded(output)["scenes"][0]["variants"][0]
            self.assertEqual(variant["archiveKind"], "program")
            self.assertEqual(variant["archivePath"], (root / "input/001.program.gojo.json").as_uri())

    def test_source_labels_cannot_close_the_embedded_script_and_survive_public_compression(self):
        with tempfile.TemporaryDirectory() as temp:
            root, run = Path(temp), fixture()
            label = '</script><script>untrusted()</script>\u2028'
            run["description"] = label
            write_source(root / "input", run)
            for public in (False, True):
                output = root / str(public) / "index.html"
                build([root / "input"], output, public=public)
                self.assertNotIn('</script><script>untrusted()', output.read_text(encoding="utf-8"))
                self.assertEqual(embedded(output)["scenes"][0]["description"], label)

    def test_machine_local_notes_and_archive_traversal_are_refused(self):
        for key, value, error in (("description", "C:/private/observations.ndjson", "machine-local"),
                                   ("archive", "../outside.json", "sibling"),
                                   ("programArchive", "../outside.json", "sibling")):
            with self.subTest(key=key), tempfile.TemporaryDirectory() as temp:
                root, run = Path(temp), fixture()
                run[key] = value
                write_source(root / "input", run)
                output = root / "public/index.html"
                with self.assertRaisesRegex(ValueError, error):
                    build([root / "input"], output, public=True)
                self.assertFalse(output.exists())

    def test_display_capacity_refuses_rounding_or_silent_clipping(self):
        for value in (True, 0.5, 2**24):
            with self.subTest(value=value):
                run = fixture()
                run["rungs"][0]["sites"][0]["x"] = value
                with self.assertRaisesRegex(ValueError, "integer capacity"):
                    normalize(run)


if __name__ == "__main__":
    unittest.main()
