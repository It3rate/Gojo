"""Synthetic negative reads and correspondence checks for the measured adapter."""
import math
import unittest
from fit_pilot import analyze, bezier, normalize


def stroke(points):
    return [[p[d] for p in points] for d in (0,1)]


class FitPilotTests(unittest.TestCase):
    def test_aspect_preserved_and_reconstructible_basis(self):
        raw=[(10,20),(50,20),(50,40)]
        points,basis=normalize([stroke(raw)])
        self.assertEqual(points[0][-1],(1,.5))
        for source,p in zip(raw,points[0]):
            self.assertEqual(tuple(p[d]*basis["source_units_per_normalized_unit"]+basis["origin"][d] for d in (0,1)),source)

    def test_exact_return_does_not_weld_identity_or_claim_closure(self):
        result=analyze([stroke([(0,0),(1,0),(1,1),(0,1),(0,0)])])
        row=result["strokes"][0]
        self.assertTrue(row["coordinate_return"])
        self.assertIn("hypothesis",row["closure_proposal"]["status"])
        self.assertEqual(row["retained_indices"],[0,1,2,3,4])
        self.assertIn("independent identity",result["closure_policy"])

    def test_multi_stroke_loop_retains_pen_lifts_and_possible_contacts(self):
        result=analyze([stroke([(0,0),(1,0)]),stroke([(1,0),(1,1)]),stroke([(1,1),(0,0)])])
        self.assertEqual(len(result["strokes"]),3)
        self.assertEqual(len(result["endpoint_proposals"]),3)
        self.assertTrue(all("independent acquisition" in c["status"] for c in result["endpoint_proposals"]))

    def test_singleton_has_one_endpoint_identity(self):
        result=analyze([stroke([(0,0)]),stroke([(0,0),(1,0)])])
        self.assertEqual(len(result["endpoint_proposals"]),1)

    def test_crossing_is_not_endpoint_contact(self):
        result=analyze([stroke([(-1,0),(1,0)]),stroke([(0,-1),(0,1)])])
        self.assertEqual(result["endpoint_proposals"],[])
        self.assertEqual(len(result["strokes"]),2)

    def test_collinear_retrace_is_visible_despite_zero_geometric_error(self):
        row=analyze([stroke([(0,0),(1,0),(.5,0),(2,0)])])["strokes"][0]
        section=row["alternatives"][0]["sections"][0]
        self.assertEqual(section["max_error"],0)
        self.assertEqual(section["backward_progress_steps"],1)
        self.assertEqual(section["projected_progress"],[0,.5,.25,1])

    def test_duplicate_samples_keep_source_correspondence(self):
        row=analyze([stroke([(0,0),(0,0),(1,0),(1,0),(2,0)])])["strokes"][0]
        self.assertEqual(row["retained_indices"],[0,2,4])
        self.assertEqual([d["sample"] for d in row["duplicate_read"]],[1,3])
        self.assertEqual(row["alternatives"][0]["sections"][0]["source_indices"],[0,2,4])

    def test_degenerate_empty_singleton_and_constant_strokes(self):
        for points in ([],[(4,4)],[(4,4),(4,4)]):
            result=analyze([stroke(points)])
            self.assertTrue(result["basis"]["degenerate"])
            self.assertTrue(all(a["section_count"]==0 for a in result["strokes"][0]["alternatives"]))

    def test_invalid_geometry_is_not_silently_truncated(self):
        for drawing in ([[[0,1],[0]]],[[[0],[0],[0,1]]],[[[True],[0]]],[[[float("nan")],[0]]]):
            with self.assertRaises(ValueError):
                analyze(drawing)

    def test_overflowing_measured_basis_is_explicitly_refused(self):
        with self.assertRaisesRegex(ValueError,"extent"):
            analyze([stroke([(-1e308,0),(1e308,0)])])

    def test_notch_survives_fine_read_but_can_disappear_coarsely(self):
        pts=[(0,0),(.45,0),(.5,.015),(.55,0),(1,0)]
        alternatives=analyze([stroke(pts)])["strokes"][0]["alternatives"]
        for grammar in ("line","line-quadratic-cubic"):
            fine=next(a for a in alternatives if a["grammar"]==grammar and a["epsilon"]==.005)
            coarse=next(a for a in alternatives if a["grammar"]==grammar and a["epsilon"]==.02)
            self.assertEqual(fine["section_count"],4)
            self.assertEqual(coarse["section_count"],1)

    def test_nearly_closed_remains_a_gap(self):
        row=analyze([stroke([(0,0),(1,0),(1,1),(0,1),(.01,0)])])["strokes"][0]
        self.assertGreater(row["endpoint_gap"],0)
        self.assertFalse(row["coordinate_return"])
        self.assertTrue(row["closure_proposal"]["within_threshold"])

    def test_translation_and_uniform_scale_do_not_change_proposals(self):
        pts=[(0,0),(.4,.2),(1,0)]
        self.assertEqual(analyze([stroke(pts)])["strokes"],analyze([stroke([(x*10+30,y*10+20) for x,y in pts])])["strokes"])

    def test_sampled_fit_bound_and_ordered_span_coverage(self):
        controls=[(0,0),(.1,1),(.9,-.2),(1,0)]
        pts=[bezier(controls,i/60) for i in range(61)]
        row=analyze([stroke(pts)])["strokes"][0]
        for alternative in row["alternatives"]:
            spans=alternative["sections"]
            self.assertEqual(spans[0]["source_start"],0)
            self.assertEqual(spans[-1]["source_end"],60)
            self.assertEqual(sum(s["source_end"]-s["source_start"] for s in spans),60)
            for a,b in zip(spans,spans[1:]):
                self.assertEqual(a["source_end"],b["source_start"])
            self.assertLessEqual(alternative["max_error"],alternative["epsilon"]+1e-12)

    def test_round_contour_retains_multiple_curve_proposals_without_circle_claim(self):
        pts=[(math.cos(t*2*math.pi/100),math.sin(t*2*math.pi/100)) for t in range(101)]
        row=analyze([stroke(pts)])["strokes"][0]
        self.assertTrue(any(s["degree"]>1 for a in row["alternatives"] for s in a["sections"]))
        self.assertEqual(len(row["alternatives"]),6)


if __name__=="__main__":
    unittest.main()
