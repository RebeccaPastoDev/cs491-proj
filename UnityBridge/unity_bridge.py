"""
Unity-Rover Communication Bridge

Provides bidirectional WebSocket communication between
Unity and the rover software framework.

Current capabilities:
    - Stream actual recorded rover telemetry.
    - Receive Unity A* waypoint sequences.
    - Validate incoming mission requests.
    - Convert Unity coordinates into ROS 2 coordinates.
    - Return mission acknowledgments.

Future capabilities:
    - Live ROS 2 telemetry subscriptions.
    - Integration with the autonomous path follower.
    - Mission progress and completion tracking.

Safety:
    Physical execution is disabled.
    This module does not publish ROS 2 movement commands.
"""

import asyncio
import argparse
import copy
import json
import math

from pathlib import Path
from datetime import datetime, timezone

import websockets
from websockets.exceptions import ConnectionClosed
from mission_validator import prepare_mission


# Network configuration.
# Localhost restricts access to the local environment.
DEFAULT_HOST = "127.0.0.1"
DEFAULT_PORT = 8765

# Transmit telemetry at approximately 10 Hz.
# This is the communication rate, not the sensor sampling rate.
TELEMETRY_INTERVAL = 0.1

# Limit incoming messages to 256 KB.
MAX_MESSAGE_SIZE = 256 * 1024

# Resolve the recording relative to this Python file.
# This allows the project to run from different directories.
BASE_DIR = Path(__file__).resolve().parent
RECORDING_PATH = BASE_DIR / "recorded_telemetry.json"


def utc_timestamp():
    """Return the current UTC transmission timestamp."""
    return datetime.now(timezone.utc).isoformat()


def load_recorded_telemetry():
    """
    Load actual measurements exported from the physical rover.

    Returns:
        dict: Original battery, odometry, IMU and LiDAR data.

    Raises:
        ValueError: If required sensor datasets are missing.
    """

    with RECORDING_PATH.open("r", encoding="utf-8") as file:
        data = json.load(file)

    # Verify that all required sensor datasets are present.
    for sensor in ("battery", "odometry", "imu", "lidar"):
        if not isinstance(data.get(sensor), dict):
            raise ValueError(f"Missing recorded sensor: {sensor}")

    # Verify that the recording can be transmitted as valid JSON.
    json.dumps(data, allow_nan=False)

    return data


def generate_telemetry(recording, mission_state):
    """
    Construct a telemetry message using recorded measurements.

    Original sensor values and timestamps remain unchanged.
    Transmission metadata and mock mission status are added
    separately to distinguish recorded data from live data.
    """

    # Copy the recording to preserve the original dataset.
    telemetry = copy.deepcopy(recording)

    # Preserve the original source before identifying the mode.
    telemetry["sensor_source"] = telemetry["source"]

    # Add communication metadata.
    telemetry["type"] = "telemetry"
    telemetry["source"] = "recorded"
    telemetry["rover_id"] = "rover_01"
    telemetry["timestamp_utc"] = utc_timestamp()
    telemetry["is_live"] = False
    telemetry["physical_execution_enabled"] = False

    # Convert the actual battery reading into volts.
    telemetry["battery"]["voltage_v"] = (
        telemetry["battery"]["raw_value"] / 10.0
    )

    # Mission status is simulated, unlike the sensor measurements.
    telemetry["mission"] = {
        "mission_id": mission_state["mission_id"],
        "status": mission_state["status"],
        "source": "mock"
    }

    return telemetry


async def send_telemetry(websocket, recording, mission_state):
    """
    Continuously transmit recorded rover telemetry.

    Repeating the snapshot tests communication and visualization
    without falsely presenting it as newly measured sensor data.
    """

    while True:
        telemetry = generate_telemetry(
            recording,
            mission_state
        )

        await websocket.send(
            json.dumps(telemetry, allow_nan=False)
        )

        # Allow incoming mission messages to be processed.
        await asyncio.sleep(TELEMETRY_INTERVAL)


async def receive_messages(websocket, mission_state, scale):
    """
    Receive, validate and acknowledge Unity mission requests.

    A successful acknowledgment confirms message validation,
    not authorization to move the physical rover.
    """

    async for raw_message in websocket:

        try:
            # Decode the incoming Unity JSON message.
            message = json.loads(raw_message)

            # Validate and convert the A* waypoint sequence.
            mission = prepare_mission(message, scale)

            # Construct the acknowledgment for Unity.
            response = {
                "type": "mission_ack",
                "schema_version": "1.0",
                "timestamp_utc": utc_timestamp(),
                "accepted": True,
                "physical_execution_enabled": False,
                "mission": mission
            }

            # Verify serialization before updating mission state.
            encoded = json.dumps(response, allow_nan=False)

            mission_state["mission_id"] = mission["mission_id"]
            mission_state["status"] = "validated"

            print(
                "Mission validated:",
                mission["mission_id"],
                flush=True
            )

        except (ValueError, TypeError, OverflowError) as error:

            # Reject malformed requests without closing the session.
            response = {
                "type": "mission_ack",
                "schema_version": "1.0",
                "timestamp_utc": utc_timestamp(),
                "accepted": False,
                "error": str(error),
                "physical_execution_enabled": False
            }

            encoded = json.dumps(response, allow_nan=False)

            print("Mission rejected:", error, flush=True)

        # Return the acknowledgment to Unity.
        await websocket.send(encoded)


async def handle_client(websocket, recording, scale):
    """
    Manage bidirectional communication with one client.

    Each connection maintains an independent test mission state.
    """

    print(
        "Client connected:",
        websocket.remote_address,
        flush=True
    )

    mission_state = {
        "mission_id": None,
        "status": "idle"
    }

    # Run telemetry transmission and mission reception concurrently.
    tasks = [
        asyncio.create_task(
            send_telemetry(websocket, recording, mission_state)
        ),
        asyncio.create_task(
            receive_messages(websocket, mission_state, scale)
        )
    ]

    try:
        # End the session when either communication task finishes.
        await asyncio.wait(
            tasks,
            return_when=asyncio.FIRST_COMPLETED
        )

    finally:
        # Cancel remaining tasks when the client disconnects.
        for task in tasks:
            task.cancel()

        results = await asyncio.gather(
            *tasks,
            return_exceptions=True
        )

        # Report unexpected errors without treating normal
        # connection closure as a server failure.
        for result in results:
            if isinstance(result, Exception) and not isinstance(
                result, ConnectionClosed
            ):
                print("Client error:", result, flush=True)

        print("Client disconnected", flush=True)


async def main(args):
    """Load recorded telemetry and start the WebSocket server."""

    recording = load_recorded_telemetry()

    # WebSockets 9.1 requires a two-argument connection handler.
    async def handler(websocket, path):
        await handle_client(websocket, recording, args.scale)

    async with websockets.serve(
        handler,
        args.host,
        args.port,
        max_size=MAX_MESSAGE_SIZE,
        ping_interval=20,
        ping_timeout=20
    ):
        print("UNITY-ROVER COMMUNICATION BRIDGE", flush=True)
        print(f"Endpoint: ws://{args.host}:{args.port}", flush=True)
        print("Telemetry mode: RECORDED", flush=True)
        print("Sensor source: PHYSICAL ROVER", flush=True)
        print("Recorded at:", recording["exported_at_utc"], flush=True)
        print("Physical execution: DISABLED", flush=True)

        # Keep the server available until manually stopped.
        await asyncio.Future()


if __name__ == "__main__":

    # Configure deployment settings without editing source code.
    parser = argparse.ArgumentParser(
        description="Unity-Rover WebSocket Bridge"
    )

    parser.add_argument("--host", default=DEFAULT_HOST)
    parser.add_argument("--port", type=int, default=DEFAULT_PORT)
    parser.add_argument("--scale", type=float, default=0.03048)

    args = parser.parse_args()

    # Reject invalid coordinate scaling configurations.
    if not math.isfinite(args.scale) or args.scale <= 0:
        parser.error("Scale must be positive and finite")

    try:
        asyncio.run(main(args))

    except KeyboardInterrupt:
        print("\nUnity bridge stopped.")
