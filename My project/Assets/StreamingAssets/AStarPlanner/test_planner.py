import unittest

from planner import plan


class PlannerTests(unittest.TestCase):
    def test_enough_battery(self):
        grid = [[0, 0, 0]]

        result = plan(
            grid,
            (0, 0),
            (0, 2),
            battery=1.0,
        )

        self.assertTrue(result["feasible"])
        self.assertEqual(
            result["waypoints"],
            [(0, 0), (0, 1), (0, 2)],
        )
        self.assertAlmostEqual(
            result["energy_wh"],
            0.1055555556,
        )

    def test_insufficient_battery(self):
        grid = [[0, 0, 0]]

        result = plan(
            grid,
            (0, 0),
            (0, 2),
            battery=0.01,
        )

        self.assertFalse(result["feasible"])
        self.assertEqual(
            result["reason"],
            "insufficient_battery",
        )
        self.assertEqual(result["waypoints"], [])

    def test_blocked_destination(self):
        grid = [[0, None, 0]]

        result = plan(
            grid,
            (0, 0),
            (0, 2),
            battery=10.0,
        )

        self.assertFalse(result["feasible"])
        self.assertEqual(result["reason"], "no_path")
        self.assertIsNone(result["energy_wh"])

    def test_avoids_expensive_hill(self):
        grid = [
            [0, 5, 0],
            [0, 0, 0],
        ]

        # Allow the hill so this tests energy-based route selection.
        result = plan(
            grid,
            (0, 0),
            (0, 2),
            battery=10.0,
            max_slope=89.0,
        )

        self.assertTrue(result["feasible"])
        self.assertNotIn((0, 1), result["waypoints"])
        self.assertAlmostEqual(
            result["energy_wh"],
            0.2111111111,
        )

    def test_blocks_steep_uphill(self):
        grid = [[0, 1]]

        result = plan(
            grid,
            (0, 0),
            (0, 1),
            battery=10.0,
            max_slope=20.0,
        )

        self.assertFalse(result["feasible"])
        self.assertEqual(result["reason"], "no_path")

    def test_blocks_steep_downhill(self):
        grid = [[1, 0]]

        result = plan(
            grid,
            (0, 0),
            (0, 1),
            battery=10.0,
            max_slope=20.0,
        )

        self.assertFalse(result["feasible"])
        self.assertEqual(result["reason"], "no_path")


if __name__ == "__main__":
    unittest.main()