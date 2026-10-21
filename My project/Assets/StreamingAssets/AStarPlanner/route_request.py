"""One JSON request on stdin, one JSON response on stdout. No server/dependencies."""
import json
import math
import sys
from planner import plan
from battery import energy_cost


def solve(request):
    if not isinstance(request, dict):
        raise ValueError('Request must be an object.')
    rows, columns = request.get('rows'), request.get('columns')
    if any(type(n) is not int or n < 1 or n > 161 for n in (rows, columns)):
        raise ValueError('Grid dimensions must be integers from 1 to 161.')
    heights, walkable = request.get('heights'), request.get('walkable')
    if not isinstance(heights, list) or not isinstance(walkable, list):
        raise ValueError('heights and walkable must be arrays.')
    if len(heights) != rows * columns or len(walkable) != rows * columns:
        raise ValueError('Grid array lengths must match rows * columns.')
    if any(type(v) is not bool for v in walkable):
        raise ValueError('walkable must contain booleans.')
    if any(type(v) not in (int, float) or not math.isfinite(v) for v in heights):
        raise ValueError('Heights must be finite numbers.')
    grid = [[heights[r * columns + c] if walkable[r * columns + c] else None
             for c in range(columns)] for r in range(rows)]
    def point(name):
        value = request.get(name)
        if not isinstance(value, dict):
            raise ValueError(name + ' must contain row and column.')
        return value.get('row'), value.get('column')
    def number(name):
        value = request.get(name)
        if type(value) not in (int, float) or not math.isfinite(value):
            raise ValueError(name + ' must be a finite number.')
        return value
    result = plan(grid, point('start'), point('goal'),
                  battery=number('battery'), spacing=number('spacing'),
                  speed=number('speed'), max_slope=number('maxSlope'))
    segment_energy = []
    for (r, c), (nr, nc) in zip(result['waypoints'], result['waypoints'][1:]):
        rise = grid[nr][nc] - grid[r][c]
        spacing = number('spacing')
        segment_energy.append(energy_cost(math.degrees(math.atan2(rise, spacing)),
                                          number('speed'), math.hypot(spacing, rise)))
    return dict(segmentEnergyWh=segment_energy, feasible=result['feasible'], reason=result['reason'],
                energyWh=result['energy_wh'] or 0.0,
                hasEnergyEstimate=result['energy_wh'] is not None,
                waypoints=[dict(row=r, column=c) for r, c in result['waypoints']])


def main():
    try:
        raw = sys.stdin.read(4_000_001)
        if len(raw) > 4_000_000:
            raise ValueError('Request is too large.')
        result = solve(json.loads(raw))
    except (ValueError, TypeError, KeyError, OverflowError) as error:
        result = dict(feasible=False, reason='invalid_request', error=str(error),
                      energyWh=0.0, hasEnergyEstimate=False, waypoints=[])
    print(json.dumps(result, allow_nan=False))


if __name__ == '__main__':
    main()
