"""Behavioral checks for source-relative interpretation, beyond DTO shape."""
import copy
import unittest

from quickdraw_interpretation import MODES, construct
from quickdraw_structure_observations import observe_record
from verify_quickdraw_interpretation import audit_source_construction, evaluate_program


def fixture():
    # Near-horizontal top, angled roof and a pen lift back onto an earlier site.
    return {"key_id": "fixture", "drawing": [
        [[0, 0, 50, 100, 100], [100, 50, 2, 53, 100]],
        [[20, 80], [70, 74]],
        [[20, 20], [70, 95]],
    ]}


class InterpretationTests(unittest.TestCase):
    def test_every_mode_has_complete_causal_acquisition_correspondence(self):
        observation = observe_record(fixture())
        for mode in MODES:
            with self.subTest(mode=mode[0]):
                program = construct(observation, 1, *mode)
                audit_source_construction(program, observation)
                self.assertEqual(len(program["rungs"]), len(fixture()["drawing"])+2)
                self.assertEqual(set(program["rungs"][-1]["visible"]),
                                 set(program["sourceMapping"]["spanMap"].values()))

    def test_changing_prior_envelope_changes_later_drawing_without_source_access(self):
        observation = observe_record(fixture())
        program = construct(observation, 1, *MODES[0])
        before, _ = evaluate_program(program)
        moved = copy.deepcopy(program)
        for parameter in moved["parameters"]:
            if parameter["id"] in ("envelopeLeft", "envelopeRight"):
                parameter["value"] += 23
        for key in ("sourceMapping", "observationMapping", "interpretationPolicy"):
            del moved[key]
        after, _ = evaluate_program(moved)
        final_edges = set(program["rungs"][-1]["visible"])
        controls = {site for edge in program["edges"] if edge["id"] in final_edges
                    for site in edge["controls"]}
        for site in controls:
            self.assertEqual(after[site], [before[site][0]+23, before[site][1]])

    def test_axis_attraction_changes_supported_line_but_preserves_roof_direction(self):
        observation = observe_record(fixture())
        program = construct(observation, 1, *MODES[1])
        values, _ = evaluate_program(program)
        spans = {span["id"]: span for span in observation["spans"]}
        decisions = program["observationMapping"]["decisions"]
        edges = {edge["id"]: edge for edge in program["edges"]}
        horizontal = [key for key, d in decisions.items() if d["axisAttraction"] == "horizontal"]
        self.assertTrue(horizontal)
        for key in horizontal:
            controls = edges[program["sourceMapping"]["spanMap"][key]]["controls"]
            self.assertEqual(values[controls[0]][1], values[controls[-1]][1])
        angular = [key for key, span in spans.items() if span["orientation"] == "angular"]
        self.assertTrue(angular)
        self.assertTrue(all(decisions[key]["axisAttraction"] is None for key in angular))

    def test_singleton_and_empty_stroke_do_not_shift_acquired_order(self):
        source = {"key_id": "single", "drawing": [[[5], [8], [3]], [[], [], []],
                                                        [[5, 8], [8, 10], [2, 1]]]}
        observation = observe_record(source)
        program = construct(observation, 1, *MODES[2])
        audit_source_construction(program, observation)
        self.assertEqual(program["observationMapping"]["sourceStrokes"][1]["edgeIds"], [])

    def test_all_empty_acquisition_has_no_invented_construction_extent(self):
        observation = observe_record({"key_id": "empty", "drawing": [[[], []]]})
        with self.assertRaisesRegex(ValueError, "no measured extent"):
            construct(observation, 1, *MODES[0])


if __name__ == "__main__":
    unittest.main()
