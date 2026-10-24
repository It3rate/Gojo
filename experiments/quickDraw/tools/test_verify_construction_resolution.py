"""False-read protection for the independent numeric resolution audit."""
import copy
import unittest

from test_construction_series import fixture as exact_fixture
from verify_construction_resolution import (Audit, VerificationError, evaluate, verify_axis_order,
                                            verify_connected_reads, verify_correspondences, verify_program)


def fixture(level="q8"):
    program, run, source = exact_fixture()
    program = copy.deepcopy(program)
    program["resolution"] = {"baseId": program["id"], "levelId": level,
                             "originalSourceSha256": program["sourceMapping"]["sourceSha256"]}
    program["id"] += "--" + level
    run["id"] = program["id"]
    return program, run, source


def update_positions(program, run, positions):
    """Create an internally consistent native fixture after a deliberate edit."""
    for site in program["sites"]:
        if site["id"] in positions:
            site["x"]["numerator"] = positions[site["id"]][0] + 50
            site["y"]["numerator"] = positions[site["id"]][1] + 50
    values, dependencies, _ = evaluate(program)
    edges = {row["id"]: row for row in program["edges"]}
    for rung in run["rungs"]:
        for site in rung["sites"]:
            site.update(x=values[site["id"]][0], y=values[site["id"]][1], dependencies=list(dependencies[site["id"]]))
        for edge in rung["edges"]:
            controls = edges[edge["id"]]["controls"]
            edge.update(controls=[values[key] for key in controls], controlSiteIds=controls,
                        **{"from": controls[0], "to": controls[-1]})
    mapping = program["sourceMapping"]
    mapping["expectedSites"] = {key: values[target] for key, target in mapping["siteMap"].items()}
    mapping["expectedControls"] = {key: [values[control] for control in edges[target]["controls"]]
                                   for key, target in mapping["edgeMap"].items()}


class ConstructionResolutionVerificationTests(unittest.TestCase):
    def test_aspect_and_position_changes_are_allowed_with_topology_and_order_preserved(self):
        program, run, source = fixture()
        update_positions(program, run, {"a": [4, 5], "b": [94, 85], "c": [4, 5], "d": [94, 5]})
        result = verify_program(program, run, source)
        self.assertEqual(result["changedSourceEdges"], 2)
        self.assertEqual(result["sourceSites"], 4)
        self.assertEqual(result["maxControlAxisDisplacement"], 15)

    def test_exact_version_must_reproduce_original_geometry(self):
        program, run, source = fixture("exact")
        verify_program(program, run, source)
        update_positions(program, run, {"a": [4, 5], "b": [94, 85], "c": [4, 5], "d": [94, 5]})
        with self.assertRaisesRegex(VerificationError, "exact baseline matches"):
            verify_program(program, run, source)

    def test_equal_coordinates_do_not_authorize_welding_endpoints(self):
        program, run, source = fixture()
        program["sourceMapping"]["siteMap"]["c"] = "a"
        with self.assertRaisesRegex(VerificationError, "cannot be welded"):
            verify_program(program, run, source)

    def test_matching_converter_table_cannot_hide_collapsed_distinct_positions(self):
        program, run, source = fixture()
        update_positions(program, run, {"a": [0, 0], "b": [0, 100], "c": [0, 0], "d": [0, 0]})
        with self.assertRaisesRegex(VerificationError, "strict endpoint coordinate order"):
            verify_program(program, run, source)

    def test_matching_converter_table_cannot_hide_lost_alignment(self):
        program, run, source = fixture()
        update_positions(program, run, {"c": [1, 0]})
        with self.assertRaisesRegex(VerificationError, "aligned endpoint coordinates"):
            verify_program(program, run, source)

    def test_native_position_is_audited_before_source_table(self):
        program, run, source = fixture()
        run["rungs"][-1]["sites"][-1]["x"] += 1
        with self.assertRaisesRegex(VerificationError, "independent affine site position"):
            verify_program(program, run, source)

    def test_declared_expected_geometry_is_not_the_original_source(self):
        program, run, source = fixture()
        program["sourceMapping"]["expectedControls"]["first"][1] = [80, 100]
        with self.assertRaisesRegex(VerificationError, "declared coarse controls"):
            verify_program(program, run, source)

    def test_falsified_original_hash_is_rejected(self):
        program, run, source = fixture()
        program["resolution"]["originalSourceSha256"] = "f" * 64
        with self.assertRaisesRegex(VerificationError, "independent original source content hash"):
            verify_program(program, run, source)

    def test_source_acquisition_order_is_retained(self):
        program, run, source = fixture()
        program["sourceMapping"]["sourceStrokeOrder"].reverse()
        with self.assertRaisesRegex(VerificationError, "original stroke scope and order"):
            verify_program(program, run, source)

    def test_source_control_degree_cannot_be_simplified_silently(self):
        program, run, source = fixture()
        program["edges"][1]["controls"].insert(1, "c")
        update_positions(program, run, {})
        with self.assertRaisesRegex(VerificationError, "source curve degree retained"):
            verify_program(program, run, source)

    def test_equal_coordinate_unrelated_guides_are_not_causal_references(self):
        program, run, source = fixture()
        for site in program["sites"][2:]:
            site["x"] = source["sites"][[row["id"] for row in source["sites"]].index(site["id"])]["x"]
            site["y"] = source["sites"][[row["id"] for row in source["sites"]].index(site["id"])]["y"]
        update_positions(program, run, {})
        with self.assertRaisesRegex(VerificationError, "actual earlier visible guide controls"):
            verify_program(program, run, source)

    def test_mirror_axis_can_move_but_every_control_must_share_it(self):
        source = {"correspondences": [{"id": "mirror", "kind": "mirror", "source": ["left"],
                                      "target": ["right"], "reversed": [True], "axis": [[50, 0], [50, 100]]}]}
        controls = {"left": [[10, 0], [15, 40], [20, 80]], "right": [[140, 80], [145, 40], [150, 0]]}
        result = verify_correspondences(source, controls, Audit("moved mirror"))
        self.assertEqual(result[0]["axisTwiceX"], 160)
        controls["right"][1][0] += 1
        with self.assertRaisesRegex(VerificationError, "mirror paired controls share one transform"):
            verify_correspondences(source, controls, Audit("broken mirror"))

    def test_repeat_offset_can_change_but_control_shape_must_agree(self):
        source = {"correspondences": [{"id": "copies", "kind": "repeat", "source": ["a"], "target": ["b"],
                                      "translation": [100, 0], "reversed": [False]}]}
        controls = {"a": [[0, 0], [10, 40], [20, 80]], "b": [[70, 3], [80, 43], [90, 83]]}
        result = verify_correspondences(source, controls, Audit("moved repeat"))
        self.assertEqual(result[0]["translation"], (70, 3))
        controls["b"][1][1] += 1
        with self.assertRaisesRegex(VerificationError, "repeat paired controls share one transform"):
            verify_correspondences(source, controls, Audit("broken repeat"))

    def test_scope_missing_the_other_object_does_not_claim_symmetry(self):
        source = {"correspondences": [{"id": "copies", "kind": "repeat", "source": ["a"], "target": ["b"]}]}
        self.assertEqual(verify_correspondences(source, {"a": [[0, 0], [0, 20]]}, Audit("partial")), [])

    def test_strict_order_includes_negative_page_coordinates(self):
        original = {"a": [-4, -5], "b": [10, -5], "c": [2, 30]}
        actual = {"a": [-40, -10], "b": [5, -10], "c": [-2, 14]}
        verify_axis_order(original, actual, Audit("negative coordinates"))
        actual["c"][0] = -41
        with self.assertRaisesRegex(VerificationError, "strict endpoint coordinate order"):
            verify_axis_order(original, actual, Audit("crossed order"))

    def test_shared_coordinate_read_keeps_distinct_site_identities(self):
        program, run, source = fixture()
        site = next(row for row in program["sites"] if row["id"] == "c")
        site["x"] = {"from": "a", "to": "a", "numerator": 0, "denominator": 1}
        program["resolutionBindings"] = [{"site": "c", "axis": "x", "source": "a", "read": "alignment"}]
        update_positions(program, run, {})
        result = verify_program(program, run, source)
        self.assertEqual(result["connectedReads"]["alignment"], 1)
        self.assertNotEqual(program["sourceMapping"]["siteMap"]["a"], program["sourceMapping"]["siteMap"]["c"])

    def test_alignment_label_without_actual_native_reference_is_rejected(self):
        program, _, source = fixture()
        program["resolutionBindings"] = [{"site": "c", "axis": "x", "source": "a", "read": "alignment"}]
        _, dependencies, _ = evaluate(program)
        with self.assertRaisesRegex(VerificationError, "actual native dependencies"):
            verify_connected_reads(program, source, dependencies, Audit("descriptive-only read"))

    def test_translation_claim_must_use_actual_template_control_as_origin(self):
        program, _, source = fixture()
        source["correspondences"] = [{"id": "repeat", "kind": "repeat", "source": ["first"],
                                      "target": ["second"], "reversed": [False]}]
        site = next(row for row in program["sites"] if row["id"] == "c")
        site["x"] = {"from": "rough-a", "to": "rough-b", "origin": "a", "numerator": 0, "denominator": 200}
        program["resolutionBindings"] = [{"site": "c", "axis": "x", "source": "a", "read": "translation"}]
        _, dependencies, _ = evaluate(program)
        result = verify_connected_reads(program, source, dependencies, Audit("translated origin"))
        self.assertEqual(result["translation"], 1)
        site["x"]["origin"] = "rough-a"
        site["x"]["from"] = "a"
        _, dependencies, _ = evaluate(program)
        with self.assertRaisesRegex(VerificationError, "actual native translated origin"):
            verify_connected_reads(program, source, dependencies, Audit("lookalike translation"))


if __name__ == "__main__":
    unittest.main()
