import math

DEFAULT_SCALE = 0.03048


class CoordinateConverter:
    def __init__(self, scale=DEFAULT_SCALE):
        if not math.isfinite(scale) or scale <= 0:
            raise ValueError("Scale must be positive and finite")

        self.scale = scale

    def unity_to_ros(self, x, z):
        if not all(math.isfinite(v) for v in (x, z)):
            raise ValueError("Invalid coordinates")

        return {
            "x": z * self.scale,
            "y": -x * self.scale
        }

    def ros_to_unity(self, x, y):
        if not all(math.isfinite(v) for v in (x, y)):
            raise ValueError("Invalid coordinates")

        return {
            "x": -y / self.scale,
            "z": x / self.scale
        }

    def convert_path(self, waypoints):
        return [
            self.unity_to_ros(point["x"], point["z"])
            for point in waypoints
        ]
