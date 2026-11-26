"""
Mission Validation Module

Validates incoming mission requests from Unity and converts
virtual A* waypoints into physical ROS 2 coordinates.

This module is independent of ROS 2 and WebSockets, allowing
it to be tested separately from the communication server.

Safety:
    This module does not authorize mission execution,
    publish ROS 2 messages, or control physical hardware.
"""

import math
from coordinate_converter import CoordinateConverter


# Limit the number of waypoints accepted in a single mission
# to prevent excessively large requests.
MAX_WAYPOINTS = 1000


def validate_mission(message):
    """
    Validate an incoming Unity mission request.

    Parameters:
        message (dict): Decoded JSON mission request from Unity.

    Returns:
        dict: The original mission if validation succeeds.

    Raises:
        ValueError: If the mission contains missing, invalid,
                    or unsupported information.
    """

    # Ensure the incoming JSON represents an object rather
    # than an array, string, number, or other JSON value.
    if not isinstance(message, dict):
        raise ValueError("Message must be a JSON object")

    # Define the supported communication contract.
    # Currently, only the Fetch Water mission is supported.
    required = {
        "type": "mission_start",
        "schema_version": "1.0",
        "mission_name": "fetch_water",
        "coordinate_frame": "unity_world"
    }

    # Reject requests that do not follow the agreed contract.
    for key, expected in required.items():
        if message.get(key) != expected:
            raise ValueError(f"Invalid or unsupported {key}")

    # Extract the unique mission identifier.
    # This will allow Unity and the bridge to associate
    # acknowledgments and future status updates with a mission.
    mission_id = message.get("mission_id")

    # Reject missing, empty, or incorrectly typed identifiers.
    if not isinstance(mission_id, str) or not mission_id.strip():
        raise ValueError("Missing or invalid mission ID")

    # Prevent excessively long mission identifiers.
    if len(mission_id) > 128:
        raise ValueError("Mission ID too long")

    # Retrieve the ordered A* waypoint sequence from Unity.
    waypoints = message.get("waypoints")

    # Ensure the waypoint sequence is represented as a list.
    if not isinstance(waypoints, list):
        raise ValueError("Waypoints must be a list")

    # Require at least two waypoints to represent a route.
    # Enforce the maximum request size defined above.
    if not 2 <= len(waypoints) <= MAX_WAYPOINTS:
        raise ValueError("Invalid number of waypoints")

    # Validate every waypoint before performing conversion.
    for index, point in enumerate(waypoints):

        # Each waypoint must be a JSON object containing
        # coordinates on Unity's horizontal X-Z plane.
        if not isinstance(point, dict):
            raise ValueError(f"Invalid waypoint {index}")

        for axis in ("x", "z"):
            value = point.get(axis)

            # Accept only integer or floating-point coordinates.
            # Explicit type checking also rejects Boolean values.
            if type(value) not in (int, float):
                raise ValueError(
                    f"Invalid coordinate at waypoint {index}"
                )

            # Reject NaN, infinity, and numbers too large
            # to represent as finite floating-point values.
            try:
                finite = math.isfinite(value)
            except OverflowError:
                finite = False

            if not finite:
                raise ValueError(
                    f"Non-finite coordinate at waypoint {index}"
                )

    # Return the original request after all checks succeed.
    return message


def prepare_mission(message, scale=0.03048):
    """
    Validate and convert a Unity mission into ROS 2 coordinates.

    Parameters:
        message (dict): Incoming Unity mission request.
        scale (float): Physical meters per virtual Unity meter.
                       The default represents one physical foot
                       per ten virtual meters.

    Returns:
        dict: Validated mission containing both the original
              Unity waypoints and converted ROS 2 waypoints.

    Safety:
        Successful validation does not authorize movement.
        Physical execution remains explicitly disabled.
    """

    # Validate the complete request before converting coordinates.
    mission = validate_mission(message)

    # Initialize the coordinate converter with the configured
    # virtual-to-physical distance ratio.
    converter = CoordinateConverter(scale)

    # Preserve both coordinate systems for debugging,
    # integration testing, and future mission tracking.
    return {
        "mission_id": mission["mission_id"],
        "mission_name": mission["mission_name"],
        "unity_waypoints": mission["waypoints"],
        "ros_waypoints_m": converter.convert_path(
            mission["waypoints"]
        ),
        "scale": scale,

        # Indicates successful message validation only.
        "status": "validated",

        # Physical execution must remain disabled until
        # navigation and safety interlocks are implemented
        # and independently verified.
        "execution_enabled": False
    }
