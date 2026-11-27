# Unity-Rover Communication Bridge

## Purpose

A standalone communication framework for testing integration
between Unity, A* path planning, and the physical rover system.

This framework allows developers to send mission requests and
receive actual recorded rover telemetry without requiring
physical hardware, Docker, Qt, or ROS 2.

## Requirements

- Python 3.10 (tested version)
- WebSockets 10.4

## Installation

Open a terminal inside the UnityBridge directory.

Install the required dependency:

```bash
python -m pip install -r requirements.txt
```

A Python virtual environment is recommended.

## Running the Bridge

In the first terminal:

```bash
python unity_bridge.py
```

Default WebSocket endpoint:

ws://127.0.0.1:8765

The server must remain running while clients are connected.

## Testing the Bridge

In a second terminal:

```bash
python mock_unity_client.py
```

Expected final output:

ALL INTEGRATION CHECKS PASSED

## Project Files

| File | Purpose |
|---|---|
| unity_bridge.py | Bidirectional WebSocket server |
| coordinate_converter.py | Unity and ROS coordinate conversion |
| mission_validator.py | Mission request validation |
| mock_unity_client.py | Example client and integration test |
| sample_mission.json | Example Fetch Water mission |
| recorded_telemetry.json | Actual recorded rover measurements |
| requirements.txt | Python dependencies |

## Communication Protocol

Transport: WebSocket
Data format: JSON
Mission schema version: 1.0

### Unity to Bridge

Unity sends a mission request containing ordered A* waypoints.

Example:

```json
{
  "type": "mission_start",
  "schema_version": "1.0",
  "mission_id": "fetch_water_001",
  "mission_name": "fetch_water",
  "coordinate_frame": "unity_world",
  "waypoints": [
    {"x": 0.0, "z": 0.0},
    {"x": 0.0, "z": 10.0},
    {"x": 10.0, "z": 10.0},
    {"x": 10.0, "z": 30.0}
  ]
}
```

The bridge validates the mission and converts the waypoints
into physical ROS coordinates.

### Bridge to Unity

Two message types are currently supported:

- telemetry
- mission_ack

Telemetry is transmitted approximately 10 times per second.

The client must distinguish incoming messages using the
"type" field because telemetry and acknowledgments may
arrive interleaved.

An accepted mission acknowledgment indicates successful
validation, not physical mission execution.

## Telemetry

Recorded from the physical Yahboom rover on November 23, 2026.

Available measurements:

- Battery voltage (V)
- Odometry position (m)
- Linear velocity (m/s)
- Angular velocity (rad/s)
- Orientation quaternion
- IMU acceleration (m/s^2)
- IMU angular velocity (rad/s)
- LiDAR ranges (m), including 360 measurements

The original recorded sensor values are preserved.

Recorded telemetry is not live telemetry. The transmission
timestamp changes, but the recorded measurements do not.

Invalid LiDAR measurements must be filtered before use.

## Coordinate Conversion

Unity ground plane: X and Z
ROS ground plane: X and Y

Default scale: 0.03048 physical meters per Unity unit.

This provisionally represents 10 virtual meters as
1 physical foot, assuming one Unity unit equals one meter.

Forward conversion:

ROS X = Unity Z * scale
ROS Y = -Unity X * scale

Inverse conversion:

Unity X = -ROS Y / scale
Unity Z = ROS X / scale

The current conversion assumes aligned origins and headings.
The scale and frame alignment must be confirmed before
physical navigation.

## Developer Responsibilities

### Unity Team

- Implement the Unity WebSocket client.
- Send mission requests when Fetch Water is selected.
- Receive and process telemetry and mission acknowledgments.
- Connect telemetry to the HUD.
- Keep the server endpoint configurable.

### A* Team

- Generate ordered waypoints for the Fetch Water mission.
- Replace sample waypoints with actual algorithm output.
- Verify coordinate conventions and units.
- Test waypoint validation and coordinate conversion.

A* does not require a separate network client if its
waypoints are passed directly to Unity.

### Middleware and Path Follower Developer

- Extend the bridge to receive live ROS 2 telemetry.
- Preserve or coordinate changes to the Unity interface.
- Deliver validated physical waypoints to the Path Follower.
- Implement physical waypoint following using odometry.
- Publish autonomous velocity requests to /cmd_vel_auto.
- Report navigation progress and mission status.

### ROS 2 and Qt Developer

- Physical rover connectivity.
- Qt telemetry and manual override.
- Control arbitration.
- Physical execution authorization and safety validation.

## Proposed ROS 2 Interfaces

ROS distribution: Humble
ROS domain ID: 20

| Topic | Message Type |
|---|---|
| /mission/path | nav_msgs/msg/Path |
| /odom_raw | nav_msgs/msg/Odometry |
| /cmd_vel_auto | geometry_msgs/msg/Twist |
| /mission/status | std_msgs/msg/String |
| /control_state | std_msgs/msg/String |

These navigation interfaces are proposed and must be
confirmed during team integration.

## Configuration

The bridge supports configurable host, port, and scale.

Example:

```bash
python unity_bridge.py --host 127.0.0.1 --port 8765 --scale 0.03048
```

The mock client supports a configurable endpoint:

```bash
python mock_unity_client.py --uri ws://127.0.0.1:8765
```

The default endpoint is local-only. Authentication and
encrypted communication are not currently implemented.
Do not expose the development server publicly.

## Safety Restrictions

Physical execution is intentionally disabled.

The current bridge does not publish ROS motor commands.

The Path Follower must not publish directly to /cmd_vel.
Autonomous commands must pass through the control arbiter.

Mission validation does not establish that a physical
route is safe or free of obstacles.

Physical movement requires separate safety validation,
including watchdog and emergency-stop behavior.

## Verified Tests

Completed November 25, 2026:

- Coordinate conversion
- Mission input validation
- WebSocket connectivity
- Recorded telemetry reception
- Valid mission transmission
- Mission acknowledgment
- Converted waypoint reception
- Mission status updates
- Invalid mission rejection
- Physical execution remains disabled

Tested using Python 3.10 and WebSockets 10.4 inside
the ROS 2 development container.

## Remaining Work

- Standalone Windows runtime verification
- Actual Unity integration
- Actual A* integration
- Live ROS 2 telemetry integration
- Path Follower implementation
- Control arbiter integration
- Physical navigation and safety validation

## Development Notes

This is a starting framework for collaborative development.

Developers may extend the implementation to support their
respective components. Coordinate shared protocol changes
with other developers to avoid breaking compatibility.

Prioritize one functional Fetch Water mission before
introducing additional features.
