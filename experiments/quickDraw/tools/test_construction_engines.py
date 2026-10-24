"""Behavioral checks for reusable frame construction and source correspondence."""
import copy
import unittest

from authored_line_art import Scene
from construction_engines import SourceRecipe
from construction_recipes import Recipe, loc
from verify_construction_ladders import evaluate


def fixture(split_target=False):
    s = Scene("source", "Source")
    a, b, c = s.site("a", 40, 40), s.site("b", 80, 40), s.site("c", 80, 80)
    d, e, f = s.site("d", 140, 40), s.site("e", 180, 40), s.site("f", 180, 80)
    if split_target:
        split = s.site("e-independent", 180, 40)
    else:
        split = e
    s.edge("ab", a, b)
    s.edge("bc", b, c)
    s.edge("de", d, e)
    s.edge("ef", split, f)
    return s.finish()


class ConstructionEngineTests(unittest.TestCase):
    def engine_recipe(self):
        r = Recipe("engine", "Engine", {"apex": 250})
        r.begin("Frame", "Shared input frame")
        r.frame()
        a, b = r.page("a", 100, 120), r.page("b", 300, 360)
        return r, a, b

    def test_rectangle_uses_existing_opposite_corners_and_closes_at_same_site(self):
        r, a, b = self.engine_recipe()
        vertices, edges = r.rectangle("box", a, b)
        r.finish(edges)
        values, dependencies, _ = evaluate(r.data)
        self.assertEqual([values[v] for v in vertices], [[100, 120], [300, 120], [300, 360], [100, 360]])
        self.assertEqual(vertices[0], a)
        self.assertEqual(vertices[2], b)
        self.assertEqual(r.data["edges"][-1]["controls"][-1], a)
        self.assertEqual(dependencies[vertices[1]], {a, b})

    def test_triangle_reuses_frame_and_apex_parameter_changes_only_its_location(self):
        r, a, b = self.engine_recipe()
        vertices, edges = r.triangle("roof", a, b, "apex")
        r.finish(edges)
        initial, _, _ = evaluate(r.data)
        r.data["parameters"][0]["value"] = 750
        changed, _, _ = evaluate(r.data)
        self.assertEqual([initial[v] for v in vertices], [[100, 360], [150, 120], [300, 360]])
        self.assertEqual(changed[vertices[1]], [250, 120])
        self.assertEqual(changed[vertices[0]], initial[vertices[0]])
        self.assertEqual(changed[vertices[2]], initial[vertices[2]])
        self.assertEqual(r.data["edges"][-1]["controls"][-1], vertices[0])

    def test_cross_has_one_actual_four_arm_center(self):
        r, a, b = self.engine_recipe()
        sites, edges = r.cross("cross", a, b)
        r.finish(edges)
        values, _, _ = evaluate(r.data)
        self.assertEqual(values[sites[0]], [200, 240])
        self.assertEqual([e["controls"][0] for e in r.data["edges"]], [sites[0]]*4)
        self.assertEqual(len(set(sites)), 5)

    def test_ellipse_is_four_cubic_profiles_with_shared_closing_tip(self):
        r, a, b = self.engine_recipe()
        tips, edges = r.ellipse("oval", a, b)
        r.finish(edges)
        self.assertEqual(len(edges), 4)
        for i, edge in enumerate(r.data["edges"]):
            self.assertEqual(len(edge["controls"]), 4)
            self.assertEqual(edge["controls"][0], tips[i])
            self.assertEqual(edge["controls"][-1], tips[(i+1)%4])

    def test_overshooting_rough_frame_has_negative_coordinates_without_containment(self):
        r = SourceRecipe(fixture(), "source")
        r.begin("Rough", "Unrestricted rough guide")
        r.frame()
        frame = r.source_frame("rough", (-40, -20, 600, 560), kind="ellipse")
        r.finish(frame.edges)
        values, _, _ = evaluate(r.data)
        self.assertEqual(values[frame.a], [-40, -20])
        self.assertEqual(values[frame.b], [600, 560])
        self.assertEqual(r.data["constraints"], [])

    def test_exposed_frame_is_read_from_actual_primitive_output_sites(self):
        for kind in ("rectangle", "triangle", "inverted-triangle", "oval", "cross"):
            with self.subTest(kind=kind):
                r = SourceRecipe(fixture(), "source")
                r.begin("Rough", "Frame")
                r.frame()
                frame = r.source_frame("rough", (20, 30, 180, 230), kind=kind)
                r.finish(frame.edges)
                values, deps, _ = evaluate(r.data)
                self.assertEqual(values[frame.a], [20, 30])
                self.assertEqual(values[frame.b], [180, 230])
                edge_sites = {s for e in r.data["edges"] for s in e["controls"]}
                for endpoint in (frame.a, frame.b):
                    self.assertTrue(endpoint in edge_sites or deps[endpoint] <= edge_sites,
                                    "Exposed coordinates must consume produced curve controls")

    def test_source_join_reuses_identity_and_final_controls_come_from_prior_frame(self):
        r = SourceRecipe(fixture(), "source", edge_ids=["ab", "bc"])
        r.begin("Rough", "Frame")
        r.frame()
        frame = r.source_frame("rough", (20, 20, 100, 100))
        r.finish(frame.edges)
        r.begin("Draw", "Consume retained frame")
        final = r.source_scope(["ab", "bc"], frame)
        r.finish(final, frame.edges)
        r.finalize_source_mapping()
        values, dependencies, _ = evaluate(r.data)
        self.assertEqual(values[r.site_map["b"]], [80, 40])
        self.assertEqual(r.control_map["ab"][-1], r.control_map["bc"][0])
        self.assertEqual(dependencies[r.site_map["a"]], {frame.a, frame.b})
        scaled = copy.deepcopy(r.data)
        next(s for s in scaled["sites"] if s["id"] == "page-extent")["x"] = 1024
        altered, _, _ = evaluate(scaled)
        self.assertEqual(altered[r.site_map["b"]], [160, 40])

    def test_repeated_scope_consumes_actual_prior_controls_and_preserves_new_identity(self):
        r = SourceRecipe(fixture(), "source")
        r.begin("Rough", "Frame")
        r.frame()
        frame = r.source_frame("rough", (0, 0, 256, 128))
        r.finish(frame.edges)
        r.begin("Template", "Earlier drawing")
        first = r.source_scope(["ab", "bc"], frame)
        r.finish(first, frame.edges)
        r.begin("Copy", "Actual template translation")
        copied = r.translate_scope("repeat", ["ab", "bc"], ["de", "ef"], 100, 0, frame)
        r.finish(first+copied, frame.edges)
        r.finalize_source_mapping()
        values, dependencies, _ = evaluate(r.data)
        self.assertEqual(values[r.site_map["e"]], [180, 40])
        self.assertIn(r.site_map["b"], dependencies[r.site_map["e"]])
        self.assertNotEqual(r.site_map["b"], r.site_map["e"])
        self.assertEqual(r.control_map["de"][-1], r.control_map["ef"][0])

    def test_copy_rejects_coordinate_equal_independent_target_sites_before_mutation(self):
        r = SourceRecipe(fixture(split_target=True), "source")
        r.begin("Rough", "Frame")
        r.frame()
        frame = r.source_frame("rough", (0, 0, 256, 128))
        first = r.source_scope(["ab", "bc"], frame)
        r.finish(first, frame.edges)
        r.begin("Copy", "Identity must agree")
        before = copy.deepcopy((r.data, r.site_map, r.edge_map))
        with self.assertRaisesRegex(ValueError, "identity correspondence"):
            r.translate_scope("bad-copy", ["ab", "bc"], ["de", "ef"], 100, 0, frame)
        self.assertEqual((r.data, r.site_map, r.edge_map), before)

    def test_bad_translation_fails_atomically_for_complete_scope(self):
        scene = fixture()
        scene["edges"][-1]["controls"][-1][0] += 1
        r = SourceRecipe(scene, "source")
        r.begin("Rough", "Frame")
        r.frame()
        frame = r.source_frame("rough", (0, 0, 256, 128))
        first = r.source_scope(["ab", "bc"], frame)
        r.finish(first, frame.edges)
        r.begin("Copy", "All controls must agree")
        before = copy.deepcopy(r.data)
        with self.assertRaisesRegex(ValueError, "declared translation"):
            r.translate_scope("bad-copy", ["ab", "bc"], ["de", "ef"], 100, 0, frame)
        self.assertEqual(r.data, before)


if __name__ == "__main__":
    unittest.main()
