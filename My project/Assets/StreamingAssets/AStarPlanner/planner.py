import heapq
import math

from battery import energy_cost


def plan(
    grid,
    start,
    goal,
    battery,
    spacing=1.0,
    speed=0.5,
    max_slope=20.0,
):
    """Find the lowest-energy traversable route using A*.

    grid: rows of elevations in meters; None marks an obstacle
    start, goal: (row, column) tuples
    battery: available energy in Wh
    spacing: horizontal distance between adjacent cells in meters
    speed: rover speed in meters per second
    max_slope: maximum absolute slope in degrees, uphill or downhill

    The default slope limit is a placeholder simulation setting.
    Returns waypoints, energy_wh, feasible, and reason.
    """
    if not grid or not grid[0]:
        raise ValueError("Grid cannot be empty.")

    rows = len(grid)
    columns = len(grid[0])

    if any(len(row) != columns for row in grid):
        raise ValueError("Grid must be rectangular.")

    if not math.isfinite(battery) or battery < 0:
        raise ValueError("Battery must be finite and nonnegative.")

    if not math.isfinite(spacing) or spacing <= 0:
        raise ValueError("Spacing must be finite and positive.")

    if not math.isfinite(speed) or speed <= 0:
        raise ValueError("Speed must be finite and positive.")

    if not math.isfinite(max_slope) or not 0 <= max_slope < 90:
        raise ValueError(
            "Maximum slope must be at least 0 and less than 90 degrees."
        )

    for row in grid:
        for elevation in row:
            if elevation is not None and not math.isfinite(elevation):
                raise ValueError("Elevations must be finite or None.")

    for point in (start, goal):
        if (
            not isinstance(point, (tuple, list))
            or len(point) != 2
            or any(
                not isinstance(value, int) or isinstance(value, bool)
                for value in point
            )
        ):
            raise ValueError(
                "Start and goal must each contain two integer coordinates."
            )

        row, column = point

        if not (0 <= row < rows and 0 <= column < columns):
            raise ValueError("Start and goal must be inside the grid.")

        if grid[row][column] is None:
            raise ValueError("Start and goal cannot be obstacles.")

    start = tuple(start)
    goal = tuple(goal)

    def heuristic(point):
        # With four-direction movement, Manhattan distance is a lower
        # bound on horizontal travel. Flat travel provides the minimum
        # energy per meter under the current battery model.
        steps = abs(point[0] - goal[0]) + abs(point[1] - goal[1])
        return energy_cost(0, speed, steps * spacing)

    frontier = [(heuristic(start), 0.0, start)]
    best_energy = {start: 0.0}
    came_from = {}

    while frontier:
        _, current_energy, current = heapq.heappop(frontier)

        if current_energy > best_energy[current]:
            continue

        if current == goal:
            if current_energy > battery:
                return {
                    "waypoints": [],
                    "energy_wh": current_energy,
                    "feasible": False,
                    "reason": "insufficient_battery",
                }

            path = [current]

            while current in came_from:
                current = came_from[current]
                path.append(current)

            path.reverse()

            return {
                "waypoints": path,
                "energy_wh": current_energy,
                "feasible": True,
                "reason": "ok",
            }

        row, column = current

        # Four-direction movement: up, down, left, right.
        for dr, dc in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            next_row = row + dr
            next_column = column + dc

            if not (0 <= next_row < rows and 0 <= next_column < columns):
                continue

            next_elevation = grid[next_row][next_column]

            if next_elevation is None:
                continue

            elevation_change = next_elevation - grid[row][column]
            slope = math.degrees(
                math.atan2(elevation_change, spacing)
            )

            # Reject steep uphill and downhill segments.
            if abs(slope) > max_slope:
                continue

            distance = math.hypot(spacing, elevation_change)
            step_energy = energy_cost(slope, speed, distance)
            new_energy = current_energy + step_energy
            neighbor = (next_row, next_column)

            if new_energy < best_energy.get(neighbor, math.inf):
                best_energy[neighbor] = new_energy
                came_from[neighbor] = current

                heapq.heappush(
                    frontier,
                    (
                        new_energy + heuristic(neighbor),
                        new_energy,
                        neighbor,
                    ),
                )

    return {
        "waypoints": [],
        "energy_wh": None,
        "feasible": False,
        "reason": "no_path",
    }