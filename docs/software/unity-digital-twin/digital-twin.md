# Unity Digital Twin

The Unity Digital Twin is a virtual lunar environment developed using Unity and Cesium for Unity. It provides a simulated lunar surface where the rover can move, interact with environmental objects, and monitor surrounding terrain conditions.

## ◈ Lunar Environment

The lunar environment is created using Cesium for Unity and a Cesium 3D Tileset connected to Cesium ion.

The project uses the following configuration:

- **Cesium 3D Tileset:** Loads the lunar terrain.
- **Cesium ion:** Provides the terrain data.
- **Lunar Surface:** Represents the environment where the rover operates.
- **Environmental Assets:** Includes rocks, boulders, solar panels, base camp structures, and other lunar equipment.

The current Cesium configuration uses Asset ID `2684829`.

## ⬡ Terrain and Collision

Terrain collision allows the rover and other objects to interact with the lunar surface.

The project uses Cesium's physics mesh generation to support terrain physics interactions.

Important components include:

- **Create Physics Meshes:** Enabled in the Cesium 3D Tileset settings.
- **Chassis Collider:** A Box Collider attached to the rover's chassis.
- **Rigidbody:** Controls the rover's physical movement and interaction with the environment.

Terrain collision should be tested during simulation to ensure the rover remains properly positioned on the lunar surface.

## ⚙ Rover Physics

The rover uses a Rigidbody-based physics system.

Current configuration:

| Property | Value |
|---|---|
| Rigidbody Mass | 1.897 kg |
| Use Gravity | Enabled |
| Lunar Gravity X | 0 |
| Lunar Gravity Y | -1.62 |
| Lunar Gravity Z | 0 |

The project uses lunar gravitational acceleration of approximately 1.62 m/s² instead of Unity's default Earth gravity.

### Chassis Collision

The rover has a separate `ChassisCollider` object with an enabled Box Collider.

This collider defines the rover's main collision boundary.

### Mecanum Wheel System

The rover uses four mecanum wheel meshes.

The `MecanumRoverController` script handles movement and wheel animation through:

- Forward and backward movement.
- Sideways movement.
- Rotation in place.
- Four-wheel speed calculations.
- Script-controlled wheel rotation.

The controller uses Rigidbody velocity to move the rover and animates the wheel meshes through Transform rotation.

## ⌘ Rover Movement and Controls

The rover controller supports keyboard-based movement.

| Key | Function |
|---|---|
| W | Move forward |
| S | Move backward |
| A | Move left |
| D | Move right |
| Q | Rotate left |
| E | Rotate right |

The movement controller includes configurable speed, acceleration, rotation speed, and wheel animation settings.

## ◉ Ground Detection and Terrain Monitoring

The `RoverTerrainMonitor` script checks the terrain beneath the rover using physics raycasting.

Its responsibilities include:

- Detecting the nearest valid ground surface.
- Ignoring the rover's own colliders.
- Calculating terrain slope.
- Sharing ground and slope information with the warning manager.
- Updating desktop and VR slope displays.

### Slope Monitoring

The system uses two slope thresholds:

| Status | Slope |
|---|---|
| Normal | Below 10° |
| Caution | 10° or higher |
| Warning | 15° or higher |

The slope information is displayed using different colors to help operators identify terrain conditions.

## ⌖ Obstacle Monitoring and Warning System

The `RoverWarningManager` combines information from the terrain and obstacle monitoring scripts.

The system evaluates terrain slope and obstacle distance to generate warnings.

### Obstacle Thresholds

| Status | Distance |
|---|---|
| Normal | Greater than 10 m |
| Caution | 5–10 m |
| Warning | 5 m or less |

The warning manager receives obstacle distance and direction from `RoverObstacleMonitor`.

### Warning Notifications

Warnings are displayed through:

- Desktop monitoring interface.
- Desktop notification banner.
- VR monitoring interface.
- VR notification banner.

Critical warnings receive higher priority than caution warnings. The monitoring interface displays up to two warnings at a time, while the notification banner displays the highest-priority warning.

## ▣ Environmental Assets

The lunar environment includes additional objects to represent exploration infrastructure.

Examples include:

- Lunar rocks and boulders.
- Solar panels.
- Base camp structures.
- Charging stations.
- Water storage structures.
- Platforms and other supporting objects.

These assets help create a more complete lunar exploration environment.

## ✓ Testing and Validation

Testing is necessary to ensure that the Digital Twin operates correctly.

Important testing areas include:

- Verify that the Cesium lunar terrain loads correctly.
- Confirm that terrain physics meshes are generated.
- Test rover movement and rotation.
- Check chassis collision with environmental objects.
- Verify that the rover responds to lunar gravity.
- Test ground detection and slope measurements.
- Verify obstacle monitoring and warning notifications.
- Check desktop and VR monitoring displays.

## ✧ Future Improvements

Potential improvements include:

- Improving rover physics and terrain interaction.
- Adding lunar dust effects around the wheels.
- Expanding the lunar environment with additional exploration areas.
- Improving environmental object collision.
- Enhancing obstacle detection and pathfinding integration.
- Improving VR interaction and monitoring features.
