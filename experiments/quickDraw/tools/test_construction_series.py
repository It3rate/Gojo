"""Negative and source-identity checks for the independent construction audit."""
import copy
import unittest

from verify_construction_series import VerificationError, canonical_sha256, evaluate, verify_inventory, verify_program


def fixture():
    source = {"id": "original", "sites": [
        {"id": "a", "x": 0, "y": 0}, {"id": "b", "x": 100, "y": 100},
        {"id": "c", "x": 0, "y": 0}, {"id": "d", "x": 100, "y": 0}],
        "edges": [{"id": "first", "from": "a", "to": "b", "controls": [[0, 0], [100, 100]]},
                  {"id": "second", "from": "c", "to": "d", "controls": [[0, 0], [100, 0]]}],
        "strokes": [{"id": "p0", "edges": ["first"]}, {"id": "p1", "edges": ["second"]}]}
    positions = {site["id"]: [site["x"], site["y"]] for site in source["sites"]}
    def locator(value):
        return {"from": "rough-a", "to": "rough-b", "numerator": value + 50, "denominator": 200}
    program = {"id": "construction", "viewBox": [0, 0, 100, 100], "parameters": [],
               "sites": [{"id": "rough-a", "at": 0, "x": -50, "y": -50},
                         {"id": "rough-b", "at": 0, "x": 150, "y": 150}] + [
                             {"id": key, "at": 1, "x": locator(point[0]), "y": locator(point[1])}
                             for key, point in positions.items()],
               "edges": [{"id": "rough", "at": 0, "controls": ["rough-a", "rough-b"]},
                         {"id": "final-1", "at": 1, "controls": ["a", "b"]},
                         {"id": "final-2", "at": 1, "controls": ["c", "d"]}],
               "rungs": [{"id": "r0", "visible": ["rough"], "guides": [], "selected": [], "introduced": ["rough"]},
                         {"id": "r1", "visible": ["final-1", "final-2"], "guides": ["rough"],
                          "selected": ["rough"], "introduced": ["final-1", "final-2"]}],
               "sourceMapping": {"status": "authored-source-correspondence", "sceneId": "original",
                                 "sourceSha256": canonical_sha256(source), "sourceEdgeOrder": ["first", "second"],
                                 "sourceStrokeOrder": [["first"], ["second"]],
                                 "siteMap": {key: key for key in positions},
                                 "edgeMap": {"first": "final-1", "second": "final-2"},
                                 "expectedSites": positions,
                                 "expectedControls": {edge["id"]: edge["controls"] for edge in source["edges"]}}}
    all_positions = {"rough-a": [-50, -50], "rough-b": [150, 150], **positions}
    run = {"id": program["id"], "viewBox": program["viewBox"],
           "nativeRestoreVerified": True, "nativeReplayVerified": True, "rungs": []}
    for index, plan in enumerate(program["rungs"]):
        native_sites = [{"id": site["id"], "x": all_positions[site["id"]][0],
                         "y": all_positions[site["id"]][1], "at": site["at"],
                         "dependencies": [] if site["at"] == 0 else ["rough-a", "rough-b"]}
                        for site in program["sites"] if site["at"] <= index]
        native_edges = []
        for key in plan["visible"] + plan["guides"]:
            edge = next(edge for edge in program["edges"] if edge["id"] == key)
            native_edges.append({"id": key, "at": edge["at"], "guide": key in plan["guides"],
                                 "introduced": key in plan["introduced"],
                                 "controlSiteIds": edge["controls"],
                                 "controls": [all_positions[key] for key in edge["controls"]],
                                 "from": edge["controls"][0], "to": edge["controls"][-1]})
        run["rungs"].append({"id": plan["id"], "verifiedNativeRestore": True, "verifiedNativeReplay": True,
                             "sites": native_sites, "edges": native_edges})
    return program, run, source


class ConstructionSeriesAuditTests(unittest.TestCase):
    def test_overshooting_rough_guides_are_valid_and_coincident_source_sites_stay_distinct(self):
        program, run, source = fixture()
        result = verify_program(program, run, source)
        self.assertEqual(result["sourceSites"], 4)
        self.assertEqual(result["sourceEdges"], 2)
        self.assertGreater(result["checks"], 100)

    def test_native_numeric_change_is_detected_independently(self):
        program, run, source = fixture()
        run["rungs"][1]["sites"][2]["x"] = 1
        with self.assertRaisesRegex(VerificationError, "independent affine site position"):
            verify_program(program, run, source)

    def test_falsifying_expected_controls_cannot_change_the_independently_loaded_original(self):
        program, run, source = fixture()
        program["sourceMapping"]["expectedControls"] = copy.deepcopy(program["sourceMapping"]["expectedControls"])
        program["sourceMapping"]["expectedControls"]["first"][0][0] = 1
        with self.assertRaisesRegex(VerificationError, "expected controls agree with independent original"):
            verify_program(program, run, source)

    def test_coordinate_equal_source_endpoints_cannot_be_welded(self):
        program, run, source = fixture()
        program["sourceMapping"]["siteMap"]["c"] = "a"
        with self.assertRaisesRegex(VerificationError, "cannot be welded"):
            verify_program(program, run, source)

    def test_native_control_identity_change_is_detected_even_when_positions_match(self):
        program, run, source = fixture()
        run["rungs"][1]["edges"][0]["controlSiteIds"] = ["c", "b"]
        with self.assertRaisesRegex(VerificationError, "native control identity and order"):
            verify_program(program, run, source)

    def test_future_rung_reference_is_rejected_even_if_source_row_appears_first(self):
        program, _, _ = fixture()
        program["sites"][0]["at"] = 1
        program["sites"][2]["at"] = 0
        with self.assertRaisesRegex(VerificationError, "future rung"):
            evaluate(program)

    def test_same_rung_reference_must_appear_before_its_consumer(self):
        program, _, _ = fixture()
        program["sites"][0], program["sites"][2] = program["sites"][2], program["sites"][0]
        with self.assertRaisesRegex(VerificationError, "unavailable or later site"):
            evaluate(program)

    def test_missing_retirement_is_not_an_acceptable_refinement(self):
        program, run, source = fixture()
        program["rungs"][1]["selected"] = []
        with self.assertRaisesRegex(VerificationError, "retired result edges"):
            verify_program(program, run, source)

    def test_source_stroke_scopes_cannot_be_reordered(self):
        program, run, source = fixture()
        program["sourceMapping"]["sourceStrokeOrder"].reverse()
        with self.assertRaisesRegex(VerificationError, "source acquisition scopes and order"):
            verify_program(program, run, source)

    def test_coordinate_equal_unrelated_rough_geometry_is_not_a_dependency(self):
        program, run, source = fixture()
        independent = [{"id": "independent-rough-a", "at": 0, "x": -50, "y": -50},
                       {"id": "independent-rough-b", "at": 0, "x": 150, "y": 150}]
        program["sites"][2:2] = independent
        names = [s["id"] for s in independent]
        program["edges"][0]["controls"] = names
        for rung in run["rungs"]:
            rung["sites"][2:2] = [{**site, "dependencies": []} for site in independent]
            edge = next(e for e in rung["edges"] if e["id"] == "rough")
            edge.update(controlSiteIds=names, **{"from": names[0], "to": names[-1]})
        with self.assertRaisesRegex(VerificationError, "actual earlier visible construction sites"):
            verify_program(program, run, source)

    def test_selected_final_edge_cannot_disappear(self):
        program, run, source = fixture()
        program["rungs"][1]["visible"].remove("final-2")
        run["rungs"][1]["edges"] = [edge for edge in run["rungs"][1]["edges"] if edge["id"] != "final-2"]
        with self.assertRaisesRegex(VerificationError, "finished result contains every selected source edge"):
            verify_program(program, run, source)

    def test_negative_fractional_displacement_truncates_toward_zero(self):
        program, _, _ = fixture()
        program["sites"] = [{"id": "a", "at": 0, "x": 0, "y": 0},
                            {"id": "b", "at": 0, "x": 10, "y": 10},
                            {"id": "c", "at": 1,
                             "x": {"from": "a", "to": "b", "numerator": -1, "denominator": 3},
                             "y": {"from": "a", "to": "b", "numerator": 1, "denominator": 3}}]
        values, dependencies, remainders = evaluate(program)
        self.assertEqual(values["c"], [-3, 3])
        self.assertEqual(dependencies["c"], {"a", "b"})
        self.assertEqual([row["coordinateRemainder"] for row in remainders], [[-1, 3], [1, 3]])

    def test_transition_split_group_follows_original_source_across_display_families(self):
        from verify_construction_ladders import transition
        program, run, _ = fixture()
        program.update(family="individual-view", constraints=[], repeats=[], circular=[])
        for rung in run["rungs"]:
            rung["constraints"] = []
        first = transition(program, run, 0)
        program["family"] = "atlas-view"
        second = transition(program, run, 0)
        self.assertEqual(first["provenance"]["splitUnit"], "original")
        self.assertEqual(first["provenance"]["splitUnit"], second["provenance"]["splitUnit"])
        self.assertNotEqual(first["provenance"]["constructionFamily"], second["provenance"]["constructionFamily"])
        del program["sourceMapping"]
        self.assertEqual(transition(program, run, 0)["provenance"]["splitUnit"], "atlas-view")

    def test_requested_inventory_requires_each_letter_and_a_complete_atlas(self):
        sources = {key: {"edges": [{"id": "outline"}]} for key in
                   [*(f"church-{index:02d}" for index in range(1, 13)), "lucide-church", "tabler-church"]}
        programs = [{"sourceMapping": {"sceneId": key, "edgeMap": {"outline": "native"}}} for key in sources]
        letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
        sources["diagnostic-alphabet"] = {"edges": [{"id": letter} for letter in letters],
                                           "objects": [{"label": letter, "edges": [letter]} for letter in letters]}
        programs += [{"sourceMapping": {"sceneId": "diagnostic-alphabet", "edgeMap": {letter: "native"}}}
                     for letter in letters]
        atlas = {"sourceMapping": {"sceneId": "diagnostic-alphabet", "edgeMap": {letter: letter for letter in letters}}}
        programs.append(atlas)
        self.assertGreater(verify_inventory(programs, sources), 26)
        programs.remove(atlas)
        with self.assertRaisesRegex(VerificationError, "complete alphabet atlas"):
            verify_inventory(programs, sources)

    def test_complete_atlas_does_not_cover_an_omitted_individual_letter(self):
        sources = {key: {"edges": [{"id": "outline"}]} for key in
                   [*(f"church-{index:02d}" for index in range(1, 13)), "lucide-church", "tabler-church"]}
        programs = [{"sourceMapping": {"sceneId": key, "edgeMap": {"outline": "native"}}} for key in sources]
        letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
        sources["diagnostic-alphabet"] = {"edges": [{"id": letter} for letter in letters],
                                           "objects": [{"label": letter, "edges": [letter]} for letter in letters]}
        programs += [{"sourceMapping": {"sceneId": "diagnostic-alphabet", "edgeMap": {letter: "native"}}}
                     for letter in letters.replace("M", "A")]
        programs.append({"sourceMapping": {"sceneId": "diagnostic-alphabet", "edgeMap": {letter: letter for letter in letters}}})
        with self.assertRaisesRegex(VerificationError, "letter A has one complete scoped construction"):
            verify_inventory(programs, sources)


if __name__ == "__main__":
    unittest.main()
