# Control Arbiter — Autonomous Rover Project

## Overview

The Control Arbiter is an independent ROS 2 Humble component
responsible for movement authorization, command arbitration,
manual override, and software-level safety enforcement.

It acts as an intermediary between the Qt Mission Control
interface, the autonomous Path Follower, and the physical rover.

The arbiter ensures that only one controller has movement
authorization at a time and that unsafe or unauthorized
velocity requests are rejected.

Current version: MVP v1.0 — Isolated Simulation

## Architecture

```text
Qt Manual Override          Path Follower
        |                         |
 /cmd_vel_manual           /cmd_vel_auto
        |                         |
        +------------+------------+
                     |
              Control Arbiter <--- /odom_raw
                     |
          /cmd_vel_arbiter_test
                     |
               Simulated Rover

Future physical output: /cmd_vel (NOT ENABLED)
```

The arbiter must eventually be the only component publishing
directly to the physical rover's velocity command topic.

## Project Structure

```text
ControlArbiter/
├── arbiter_core.py
├── arbiter_node.py
├── test_arbiter.py
├── test_ros_integration.py
├── README.md
└── .gitignore
```

| File | Description |
|---|---|
| arbiter_core.py | Independent arbitration and safety logic |
| arbiter_node.py | ROS 2 publishers, subscribers, and services |
| test_arbiter.py | Standalone unit tests |
| test_ros_integration.py | Isolated ROS 2 integration tests |

## Requirements

- Ubuntu 22.04 or equivalent ROS 2 Humble Docker environment
- ROS 2 Humble
- Python 3.10
- rclpy
- geometry_msgs
- nav_msgs
- std_msgs
- std_srvs

No additional third-party Python packages are required.

## Control States

| State | Description |
|---|---|
| idle | No movement authorization |
| manual | Manual controller has authorization |
| auto | Autonomous controller has authorization |
| stop_latched | All movement blocked until explicit reset |

Control priority:

1. STOP
2. Manual control
3. Autonomous control

Manual takeover revokes autonomous authorization.
Disarming or resetting STOP does not automatically resume
an autonomous mission.

## Safety Configuration

| Parameter | Value |
|---|---|
| Maximum linear velocity | 0.10 m/s |
| Maximum angular velocity | 0.30 rad/s |
| Command timeout | 0.5 seconds |
| Odometry timeout | 0.5 seconds |
| Output frequency | 20 Hz |
| Control interval | 50 milliseconds |

These values are provisional development limits and have
not been validated for physical rover operation.

The arbiter enforces:

- Zero velocity at startup.
- Explicit movement authorization.
- Manual override priority.
- Latched STOP.
- Command reception timeout.
- Odometry reception timeout.
- Basic odometry validity checks.
- Rejection of nonfinite velocities.
- Rejection of excessive velocities.
- Rejection of unsupported velocity axes.
- No automatic resumption after authorization is revoked.

## ROS 2 Topic Interfaces

| Topic | Direction | Message Type |
|---|---|---|
| /cmd_vel_manual | Input | geometry_msgs/msg/Twist |
| /cmd_vel_auto | Input | geometry_msgs/msg/Twist |
| /odom_raw | Input | nav_msgs/msg/Odometry |
| /cmd_vel_arbiter_test | Output | geometry_msgs/msg/Twist |
| /control_state | Output | std_msgs/msg/String |

Velocity commands use:

- linear.x: Forward/reverse velocity in m/s.
- angular.z: Rotational velocity in rad/s.

All other Twist fields must remain zero.

Positive angular.z represents counterclockwise rotation
under standard ROS coordinate conventions.

Command subscriptions and the velocity publisher use reliable
QoS with a queue depth of one.

Odometry uses the ROS 2 sensor data QoS profile.

## ROS 2 Control Services

All services use std_srvs/srv/Trigger.

| Service | Description |
|---|---|
| /control/authorize_manual | Request manual authorization |
| /control/authorize_auto | Request autonomous authorization |
| /control/disarm | Revoke current movement authorization |
| /control/stop | Latch STOP and publish zero velocity |
| /control/reset_stop | Clear STOP and return to idle |

Each service returns:

- success: Boolean indicating whether the request succeeded.
- message: Human-readable result or state reason.

A velocity command alone cannot authorize movement.

Autonomous authorization requires an idle arbiter and
fresh, valid odometry.

Manual authorization requires fresh, valid odometry and
cannot override a latched STOP.

## Control State Feedback

The /control_state topic publishes a JSON object encoded
inside a std_msgs/msg/String message.

Example:

```json
{
  "state": "auto",
  "reason": "auto_authorized",
  "odometry_fresh": true,
  "linear_m_s": 0.05,
  "angular_rad_s": 0.0,
  "physical_execution_enabled": false
}
```

The velocity fields represent the approved command output,
not the rover's measured velocity.

Measured velocity must continue to come from /odom_raw.

The current control-state schema is provisional and may be
refined during team integration.

## Running the Arbiter

Run all simulation components in the same isolated ROS
environment.

```bash
source /opt/ros/humble/setup.bash
export ROS_DOMAIN_ID=77
export ROS_LOCALHOST_ONLY=1

cd ControlArbiter
python3 arbiter_node.py
```

Expected startup messages:

```text
Control arbiter started in TEST MODE
Isolated output: /cmd_vel_arbiter_test
```

The physical rover must remain powered off during these tests.

## Running Automated Tests

### Standalone Unit Tests

ROS 2 is not required for the core unit tests.

```bash
python3 test_arbiter.py
```

Verified result:

```text
Ran 16 tests
OK
```

### ROS Integration Tests

Start a fresh arbiter instance in the first terminal.

In a second terminal, using the same isolated ROS environment:

```bash
source /opt/ros/humble/setup.bash
export ROS_DOMAIN_ID=77
export ROS_LOCALHOST_ONLY=1

python3 test_ros_integration.py
```

Expected result:

```text
ALL 11 ROS INTEGRATION TESTS PASSED
```

Do not run other simulated controllers or odometry publishers
while executing the automated integration tests.

The test script leaves the arbiter in STOP_LATCHED.
Restart the arbiter before repeating the complete test suite.

## Verified Test Results

Test date: November 25, 2026

| Test Category | Result |
|---|---|
| Standalone unit tests | 16/16 passed |
| ROS integration tests | 11/11 passed |
| Total | 27/27 passed |

Verified behaviors include:

- Startup with zero velocity.
- Authorization denied without odometry.
- Authorized autonomous movement requests.
- Manual takeover.
- Autonomous requests blocked during manual control.
- STOP latch and reset.
- Command timeout.
- Stale odometry timeout.
- Excessive velocity rejection.
- Independent observation of approved test-topic output.

These results verify software behavior in an isolated ROS
environment, not physical movement or stopping performance.

## Path Follower Integration

The Path Follower should continue publishing autonomous
velocity requests to:

```text
/cmd_vel_auto
```

It should subscribe to:

```text
/control_state
```

The Path Follower must respect the arbiter's authorization
state and must not publish directly to /cmd_vel.

### Proposed Autonomous Mission Lifecycle

1. Receive and validate the mission path.
2. Confirm that valid odometry is available.
3. Obtain explicit operator-approved mission authorization.
4. Coordinate authorization with /control/authorize_auto.
5. Confirm the arbiter reports state "auto".
6. Begin publishing autonomous velocity requests.
7. Monitor control-state feedback throughout execution.
8. Stop and terminate or suspend execution if authorization
   is revoked, STOP is latched, or feedback becomes stale.
9. Publish zero velocity upon completion or cancellation.
10. Coordinate disarming and report the final mission status.

The existing Path Follower mission authorization service
and the arbiter authorization service are separate.

Their coordination must be implemented and tested so that
both components agree before autonomous execution begins.

Receiving a mission path from Unity must not independently
authorize movement.

The exact authorization owner and mission lifecycle contract
remain subject to team agreement.

### Manual Takeover

When the arbiter transitions from "auto" to "manual":

- Autonomous authorization has been revoked.
- The Path Follower must stop autonomous execution.
- The previous mission must not automatically resume.
- A new explicit authorization is required.

A mission cancellation or completion must not accidentally
disarm an operator who has already taken manual control.
Coordinate service ownership before implementing this behavior.

### Simulated Rover Integration

IMPORTANT:

The simulated rover should subscribe to:

```text
/cmd_vel_arbiter_test
```

instead of:

```text
/cmd_vel_auto
```

This ensures that simulated movement passes through the
same arbitration layer intended for eventual physical use.

The simulated rover should continue publishing /odom_raw.

A command reception watchdog should also be implemented
in the simulated rover so that loss of arbiter output
causes simulated movement to stop.

Preserve the Path Follower's existing isolated simulation
safety restrictions.

## Middleware and Unity Integration

The middleware should coordinate:

- Mission receipt and validation.
- Waypoint delivery to the Path Follower.
- Mission authorization coordination.
- Mission progress and completion reporting.
- Control-state feedback.
- Cancellation and failure reporting.

Unity may display the current arbiter state and mission
status through the middleware.

Unity mission requests must not bypass the arbiter or
directly command the physical rover.

## Qt Integration

The Qt Mission Control interface is being updated to:

- Publish manual requests to /cmd_vel_manual.
- Request manual authorization through ROS services.
- Enable directional controls only after authorization.
- Monitor /control_state.
- Disable controls when authorization is lost.
- Request STOP through /control/stop.
- Explicitly reset STOP without automatically arming.
- Display commanded and measured velocities separately.

Qt integration is currently in development.

## Known Limitations

The current MVP does not yet provide:

- Physical rover motion validation.
- Verified firmware-level command watchdog.
- Automatic obstacle avoidance.
- LiDAR-based collision prevention.
- Sensor timestamp freshness validation.
- Complete protection against delayed or queued commands.
- Authenticated ROS service access.
- Mission-specific authorization identifiers.
- Guaranteed stopping after process or network failure.
- Fully integrated Qt and Path Follower authorization.

The arbiter currently evaluates message reception freshness
using a monotonic clock.

A software STOP request cannot guarantee physical stopping
if communication with the rover is interrupted.

## Physical Deployment Restrictions

Physical execution is intentionally disabled.

Do not remap /cmd_vel_arbiter_test to /cmd_vel, remove
simulation restrictions, or connect the arbiter to physical
motor control without approval from the rover integration lead.

Before physical deployment, the team must verify:

- Firmware command timeout and watchdog behavior.
- Communication-loss stopping behavior.
- Exclusive ownership of the physical command topic.
- Manual override during autonomous execution.
- STOP behavior.
- Safe physical velocity limits.
- Obstacle clearance and operating environment.
- Controlled hardware testing.

## MVP Scope

The immediate project objective is one successfully integrated
Fetch Water mission using the Unity environment, middleware,
A* waypoint generation, Path Follower, Control Arbiter,
and Qt Mission Control interface.

Additional missions, generalized navigation, and fully
portable deployment are outside the immediate MVP scope.

## Development Status

- [x] Core arbitration logic
- [x] Standalone unit testing
- [x] ROS 2 node implementation
- [x] ROS interface verification
- [x] Isolated ROS integration testing
- [ ] Path Follower integration
- [ ] Middleware integration
- [ ] Qt integration
- [ ] End-to-end simulated mission
- [ ] Physical safety validation
- [ ] Physical mission execution
- [ ] Final MVP packaging

EOF
