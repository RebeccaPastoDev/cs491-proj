# ⚙ Rover Physics

The rover physics system controls how the rover moves and interacts with the lunar environment. It uses Unity's Rigidbody physics system and custom C# scripts to control movement and monitor terrain conditions.

## ⚙ Rigidbody and Lunar Gravity

The `FlexiRover_Controller` uses a Rigidbody component to manage its physical movement.

The current Rigidbody configuration includes:

- **Mass:** `1.897`
- **Use Gravity:** Enabled

The project uses lunar gravity instead of Earth's standard gravity.

- **Gravity X:** `0`
- **Gravity Y:** `-1.62`
- **Gravity Z:** `0`

This represents the Moon's approximate gravitational acceleration of 1.62 m/s².

## ⌘ Rover Movement and Controls

The `MecanumRoverController` script manages rover movement using keyboard input and Rigidbody velocity.

The current controls are:

| Key | Function |
|---|---|
| W | Move forward |
| S | Move backward |
| A | Move left |
| D | Move right |
| Q | Rotate left |
| E | Rotate right |

The script also animates the four wheel meshes to represent rover movement.

## ⬡ Rover Collider

The rover has a chassis collider using a Box Collider component.

This collider helps the rover interact physically with the environment. The collider should be tested to make sure the rover does not pass through the lunar surface or other objects.

## ◉ Ground Detection and Terrain Monitoring

The `RoverTerrainMonitor` script checks the terrain underneath the rover using downward physics raycasts.

It calculates the ground slope and updates the rover's status.

The current slope thresholds are:

- **Normal:** Below 10°
- **Caution:** From 10° to below 15°
- **Warning:** 15° or higher

The results are displayed through the rover's desktop and VR interface.

## ⌖ Obstacle Monitoring and Warning System

The `RoverObstacleMonitor` script provides obstacle monitoring.

The `RoverWarningManager` collects information from the terrain and obstacle monitoring scripts and displays warnings through the desktop and VR interfaces.

The current warning thresholds include:

- **Slope caution:** 10°
- **Slope warning:** 15°
- **Obstacle caution:** 10 meters
- **Obstacle warning:** 5 meters

The warning manager prioritizes more serious warnings when multiple conditions are detected.

## ✓ Testing and Validation

The rover physics system should be tested to verify:

- The rover responds to keyboard controls.
- The rover moves and rotates correctly.
- Wheel animations respond to movement.
- Lunar gravity affects the rover.
- The chassis collider interacts with the terrain.
- Ground detection reports terrain conditions.
- Warning messages appear when required.

## ✧ Future Improvements

Possible improvements include refining rover movement, improving terrain interaction, testing rover stability on slopes, and validating obstacle detection.
