"""Behavioral checks for finite geometry selection before native construction."""
from copy import deepcopy
import unittest

from authored_line_art import alphabet_scene, validate
from construction_churches import church_sources
from construction_resolution_geometry import coarsen_scene


class ConstructionResolutionGeometryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.scenes = church_sources() + [alphabet_scene()]

    def test_exact_read_is_independent_and_unchanged(self):
        original = self.scenes[0]
        source = deepcopy(original)
        exact, report = coarsen_scene(original, 0)
        self.assertEqual(source, exact)
        self.assertEqual(0, report["changedControls"])
        self.assertEqual(0, report["maximumDisplacement"])
        exact["sites"][0]["x"] += 1
        self.assertEqual(source, original)

    def test_every_level_preserves_claims_identity_incidence_and_scalar_order(self):
        for original in self.scenes:
            for divisions in (64, 32, 16, 8):
                with self.subTest(scene=original["id"], divisions=divisions):
                    finite, report = coarsen_scene(original, divisions)
                    self.assertEqual([site["id"] for site in original["sites"]],
                                     [site["id"] for site in finite["sites"]])
                    self.assertEqual([(edge["id"], edge["from"], edge["to"], len(edge["controls"]))
                                      for edge in original["edges"]],
                                     [(edge["id"], edge["from"], edge["to"], len(edge["controls"]))
                                      for edge in finite["edges"]])
                    for key in ("strokes", "groups", "objects", "regions"):
                        self.assertEqual(original[key], finite[key])
                    points = [(a, b) for first, second in zip(original["edges"], finite["edges"])
                              for a, b in zip(first["controls"], second["controls"])]
                    for dimension in (0, 1):
                        observed = {}
                        for before, after in points:
                            source, target = before[dimension], after[dimension]
                            self.assertIsInstance(target, int)
                            self.assertEqual(observed.setdefault(source, target), target)
                        self.assertEqual(sorted(observed.values()),
                                         [target for _, target in sorted(observed.items())])
                        self.assertEqual(len(observed), len(set(observed.values())))
                    edges = {edge["id"]: edge for edge in finite["edges"]}
                    for relation in finite["correspondences"]:
                        for a, b, reverse in zip(relation["source"], relation["target"],
                                                 relation["reversed"]):
                            first = edges[a]["controls"]
                            second = edges[b]["controls"][::-1] if reverse else edges[b]["controls"]
                            if relation["kind"] == "mirror":
                                center = relation["axis"][0][0]
                                self.assertEqual([[2 * center - x, y] for x, y in first], second)
                            else:
                                dx, dy = relation["translation"]
                                self.assertEqual([[x + dx, y + dy] for x, y in first], second)
                    self.assertEqual(report["independentReads"],
                                     sum(report["effectiveQuantumHistogram"].values()))

    def test_authored_validation_accepts_each_coarse_fixture(self):
        for original in self.scenes:
            if original["id"] in ("lucide-church", "tabler-church"):
                continue
            finite, _ = coarsen_scene(original, 8)
            validate(finite)

    def test_coarse_read_moves_geometry_and_exposes_finer_exceptions(self):
        source = next(scene for scene in self.scenes if scene["id"] == "church-11")
        finite, report = coarsen_scene(source, 8)
        self.assertGreater(report["changedControls"], 0)
        self.assertGreater(report["meanDisplacement"], 10)
        self.assertGreater(report["retainedFineCount"], 0)
        self.assertLess(report["retainedFineCount"], report["independentReads"])
        exceptions = [row for row in report["decisions"] if row["finerException"]]
        self.assertTrue(all(row["attempts"] for row in exceptions))
        self.assertNotEqual(source["edges"], finite["edges"])

    def test_resolution_is_not_confused_with_mandatory_change(self):
        _, report = coarsen_scene(alphabet_scene(), 64)
        self.assertEqual(2, report["nominalQuantum"])
        self.assertEqual(0, report["changedControls"])
        self.assertEqual(0, report["retainedFineCount"])

    def test_coarsening_does_not_merge_distinct_coincident_import_sites(self):
        source = next(scene for scene in self.scenes if scene["id"] == "tabler-church")
        positions = {}
        for site in source["sites"]:
            positions.setdefault((site["x"], site["y"]), []).append(site["id"])
        aliases = next(group for group in positions.values() if len(group) > 1)
        finite, _ = coarsen_scene(source, 8)
        selected = {site["id"]: (site["x"], site["y"]) for site in finite["sites"]}
        self.assertEqual(len(aliases), len(set(aliases)))
        self.assertEqual(1, len({selected[identity] for identity in aliases}))

    def test_unavailable_grade_and_unimplemented_mirror_basis_are_rejected(self):
        with self.assertRaisesRegex(ValueError, "division"):
            coarsen_scene(self.scenes[0], 7)
        malformed = deepcopy(self.scenes[0])
        mirror = next(row for row in malformed["correspondences"] if row["kind"] == "mirror")
        mirror["axis"] = [[0, 0], [512, 512]]
        with self.assertRaisesRegex(ValueError, "vertical mirror"):
            coarsen_scene(malformed, 8)

    def test_original_fixture_is_not_mutated_by_coarse_selection(self):
        source = deepcopy(self.scenes[-1])
        original = deepcopy(source)
        coarsen_scene(source, 8)
        self.assertEqual(original, source)


if __name__ == "__main__":
    unittest.main()
