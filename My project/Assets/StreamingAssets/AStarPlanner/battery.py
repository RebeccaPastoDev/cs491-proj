import math


def energy_cost(slope, speed, distance):
    """Estimate energy in watt-hours.

    slope: angle in degrees; positive means uphill
    speed: rover speed in meters per second
    distance: distance traveled along the terrain in meters

    These are placeholder simulation values, not measured rover data.
    """
    if not all(math.isfinite(value) for value in (slope, speed, distance)):
        raise ValueError("Inputs must be finite numbers.")

    if not -90 < slope < 90:
        raise ValueError("Slope must be between -90 and 90 degrees.")

    if speed <= 0:
        raise ValueError("Speed must be greater than zero.")

    if distance < 0:
        raise ValueError("Distance cannot be negative.")

    base_wh_per_meter = 0.05
    electronics_watts = 5.0

    uphill_multiplier = 1.0 + max(slope, 0.0) / 10.0

    driving_energy = distance * base_wh_per_meter * uphill_multiplier
    travel_time_hours = distance / speed / 3600.0
    electronics_energy = electronics_watts * travel_time_hours

    return driving_energy + electronics_energy