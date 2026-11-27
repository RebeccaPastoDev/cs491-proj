"""
Mock Unity WebSocket Client

Simulates Unity's communication with the rover bridge
without requiring the Unity application or XR hardware.

Tests:
    - WebSocket connectivity
    - Recorded rover telemetry reception
    - Mission transmission
    - Mission acknowledgment
    - Physical execution restrictions
"""

import asyncio
import argparse
import json

from pathlib import Path

import websockets


# Default endpoint for local integration testing.
DEFAULT_URI = "ws://127.0.0.1:8765"

# Maximum waiting time for each test operation, in seconds.
TIMEOUT = 10

# Locate the sample mission relative to this script.
BASE_DIR = Path(__file__).resolve().parent
MISSION_PATH = BASE_DIR / "sample_mission.json"


async def receive_type(websocket, message_type):
    """
    Wait for a message of the requested type.

    The server continuously streams telemetry, so unrelated
    messages must be skipped while waiting for an acknowledgment.
    """

    async def wait_for_message():
        while True:
            raw_message = await websocket.recv()
            message = json.loads(raw_message)

            if message.get("type") == message_type:
                return message

    # Apply a timeout to the entire waiting operation.
    return await asyncio.wait_for(
        wait_for_message(),
        timeout=TIMEOUT
    )


async def main(uri):
    """Test bidirectional communication with the rover bridge."""

    # Load example waypoints representing Unity's A* output.
    with MISSION_PATH.open("r", encoding="utf-8") as file:
        mission = json.load(file)

    print("Connecting to:", uri)

    async with websockets.connect(uri) as websocket:

        print("PASS: WebSocket connected")

        # TEST 1: Receive actual recorded rover telemetry.
        telemetry = await receive_type(websocket, "telemetry")

        # Confirm that recorded data is not presented as live.
        assert telemetry["source"] == "recorded"
        assert telemetry["is_live"] is False
        assert telemetry["physical_execution_enabled"] is False

        print("PASS: Recorded telemetry received")
        print("Sensor source:", telemetry["sensor_source"])
        print("Recorded at:", telemetry["exported_at_utc"])

        # Extract actual sensor measurements for inspection.
        battery = telemetry["battery"]
        odometry = telemetry["odometry"]
        imu = telemetry["imu"]
        lidar = telemetry["lidar"]

        print("\nROVER TELEMETRY")
        print("Battery voltage:", battery["voltage_v"], "V")
        print("Position:", odometry["position_m"])
        print("Linear velocity:", odometry["linear_velocity_m_s"])
        print("Angular velocity:", odometry["angular_velocity_rad_s"])
        print("IMU acceleration:", imu["linear_acceleration_m_s2"])
        print("LiDAR measurements:", len(lidar["ranges_m"]))

        # TEST 2: Send the sample mission to the bridge.
        await websocket.send(
            json.dumps(mission, allow_nan=False)
        )

        print("\nMission sent:", mission["mission_id"])

        # TEST 3: Receive the mission acknowledgment.
        acknowledgment = await receive_type(
            websocket,
            "mission_ack"
        )

        assert acknowledgment["accepted"] is True
        assert acknowledgment["physical_execution_enabled"] is False

        prepared = acknowledgment["mission"]

        assert prepared["mission_id"] == mission["mission_id"]
        assert prepared["execution_enabled"] is False
        assert prepared["status"] == "validated"

        print("PASS: Mission acknowledgment received")

        # Display the converted physical waypoint sequence.
        print("\nCONVERTED ROS 2 WAYPOINTS")

        for index, point in enumerate(
            prepared["ros_waypoints_m"]
        ):
            print(
                f"Waypoint {index}: "
                f"x={point['x']:.4f} m, "
                f"y={point['y']:.4f} m"
            )

        # TEST 4: Confirm that subsequent telemetry reflects
        # the validated mission without initiating movement.
        async def wait_for_mission_update():
            while True:
                message = await receive_type(
                    websocket,
                    "telemetry"
                )

                state = message["mission"]

                if state["mission_id"] == mission["mission_id"]:
                    return message

        updated = await asyncio.wait_for(
            wait_for_mission_update(),
            timeout=TIMEOUT
        )

        assert updated["mission"]["status"] == "validated"
        assert updated["physical_execution_enabled"] is False

        print("\nPASS: Mission status updated")
        print("PASS: Physical execution remains disabled")
        print("\nALL INTEGRATION CHECKS PASSED")


if __name__ == "__main__":

    # Allow teammates to specify a remote endpoint without
    # modifying the source code.
    parser = argparse.ArgumentParser(
        description="Mock Unity WebSocket Client"
    )

    parser.add_argument(
        "--uri",
        default=DEFAULT_URI
    )

    args = parser.parse_args()

    asyncio.run(main(args.uri))
