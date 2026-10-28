import json
from pathlib import Path
import subprocess
import sys
import unittest
from route_request import solve


def request():
    return dict(rows=3, columns=5, heights=[0.0]*15, walkable=[True]*15,
                start=dict(row=1, column=0), goal=dict(row=1, column=4),
                spacing=2.0, speed=0.5, maxSlope=20.0, battery=10.0)


class BridgeTests(unittest.TestCase):
    def test_process_and_obstacle_detour(self):
        data = request()
        data['walkable'][7] = False
        result = subprocess.run([sys.executable, str(Path(__file__).with_name('route_request.py'))],
                                input=json.dumps(data), text=True, capture_output=True, check=True)
        response = json.loads(result.stdout)
        self.assertTrue(response['feasible'])
        self.assertNotIn(dict(row=1, column=2), response['waypoints'])
        self.assertEqual(response['waypoints'][0], data['start'])
        self.assertEqual(response['waypoints'][-1], data['goal'])
        self.assertAlmostEqual(response['energyWh'], 12 * (0.05 + 5/0.5/3600))
        self.assertEqual(len(response['segmentEnergyWh']), len(response['waypoints']) - 1)
        self.assertAlmostEqual(sum(response['segmentEnergyWh']), response['energyWh'])

    def test_no_path(self):
        data = request()
        for i in (2, 7, 12): data['walkable'][i] = False
        self.assertEqual(solve(data)['reason'], 'no_path')

    def test_battery_refusal(self):
        data = request(); data['battery'] = 0.001
        result = solve(data)
        self.assertEqual(result['reason'], 'insufficient_battery')
        self.assertEqual(result['waypoints'], [])
        self.assertTrue(result['hasEnergyEstimate'])

    def test_steep_slope_refusal(self):
        data = request(); data['heights'] = [0, 0, 10, 10, 10] * 3
        self.assertEqual(solve(data)['reason'], 'no_path')

    def test_same_cell(self):
        data = request(); data['goal'] = data['start']
        result = solve(data)
        self.assertEqual(result['waypoints'], [data['start']])
        self.assertEqual(result['energyWh'], 0)

    def test_invalid_input(self):
        for field, value in [('heights', [0]), ('rows', 1000), ('speed', float('nan')),
                             ('walkable', [1]*15), ('start', dict(row=-1,column=0))]:
            with self.subTest(field=field):
                data = request(); data[field] = value
                with self.assertRaises(ValueError): solve(data)


if __name__ == '__main__': unittest.main()
