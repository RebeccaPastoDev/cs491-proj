# Unity digital twin

The digital twin is a virtual lunar environment, built from Cesium's
Lunaverse in Unity, where the rover can be placed, animated, and tested.

!!! info "Section status"
    **Owners:** Dinh, Joshua, Nicole 
    **Status:** In progress

## What's in this section

<div class="grid cards" markdown>

- :material-moon-waning-crescent: __[Digital twin](digital-twin.md)__

    Notes, building, and assets.

- :material-car-traction-control: __[Rover physics](rover-physics.md)__

    Wheels, mass, lunar gravity, and terrain collision.

</div>

## Requirements

- Import Cesium and Lunaverse environments into Unity
- A framework for defining assets and connecting them to the Lunaverse
- Terrain collision using Mesh Colliders
- Rover physics with a mass system
- Lunar gravity and lunar-specific environment settings
- Rover movement animations, including rotating wheels
- Integration of pathfinding into the rover's movement

## Stretch goals

- Dust particles on the wheels to simulate moon dust
- Better rover models
- Visualization of the pathfinding route

## How this connects to other sections

- Pathfinding decides where the rover drives:
  [Path-finding algorithm](../path-finding-algorithm.md)
- VR lets users explore this environment: [Unity VR](../unity-VR.md)

## Open questions

- Is there an existing lunar rover model, or a schematic to model from?
- Which assets already exist for the Lunaverse?
- Which behaviors are physically simulated, and which are only visual?

## Contributing to this section

1. Copy an existing page in this folder as a starting point.
2. Add it to `nav` in `mkdocs.yml`.
3. Link it from the "What's in this section" cards above.
4. To track ownership, please write the contributers (including your name) of that page. 