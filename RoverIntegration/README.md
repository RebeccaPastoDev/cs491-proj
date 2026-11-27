# ARPS Rover Integration

Checkpoint: November 27, 2026

## Components

- Qt-ROS2-Framework: Operator GUI and ROS 2 telemetry.
- ControlArbiter: Manual and autonomous command arbitration.
- UnityBridge: Validated WebSocket and mission interface.
- RoverTelemetry: Recorded physical rover dataset.
- Documentation: Unity and A* integration guides.

## Recorded Telemetry

From the RoverIntegration directory, extract:

    tar -xzf RoverTelemetry/manual_drive_01.tar.gz

Includes JSONL, original ROS bag, and metadata.
Duration: 368.52 seconds.
Total messages: 30,084.

Topics:
- /odom_raw
- /imu
- /scan
- /battery
- /cmd_vel_manual
- /cmd_vel
- /control_state

Use recorded telemetry for offline testing.
Never replay recorded commands to physical hardware.

## Validation Completed

- Qt application compiled successfully.
- 16 Control Arbiter unit tests passed.
- 11 ROS integration tests passed.
- 3 physical configuration checks passed.
- Supervised physical manual driving tested.
- Directional controls and release-to-stop tested.
- Unity Bridge and Path Follower tested with a mock rover.

## Safety and Limitations

- Simulation: ROS_DOMAIN_ID=77, ROS_LOCALHOST_ONLY=1.
- Physical: ROS_DOMAIN_ID=20, ROS_LOCALHOST_ONLY=0.
- Physical autonomous execution remains disabled.
- Independent firmware watchdog behavior is unverified.
- Recorded odometry velocities exceeded command limits.
- Coordinate alignment and scale require calibration.
- Recorded sensor streams are asynchronous.
- No firmware changes were made.

## Remaining Integration Tasks

- Validate the middleware live telemetry adapter.
- Connect Unity to recorded and live telemetry.
- Connect A* waypoints to the Path Follower.
- Persist the two ROS Humble compatibility corrections.
- Reconcile the existing UnityBridge with this checkpoint.
- Complete integrated simulation testing.

This directory preserves a separate integration checkpoint
without overwriting existing teammate contributions.

Physical autonomous operation is not certified.
