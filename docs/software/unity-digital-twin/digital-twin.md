# ◈ Unity Digital Twin

The Unity Digital Twin represents the lunar environment in a virtual 3D space. It provides the terrain and environment where the rover operates.

## ◈ Lunar Environment

The project uses Cesium for Unity to display the lunar surface. The `LunarSouthPole_Cesium` scene contains the lunar terrain and environment objects.

The Cesium Georeference component defines the geographic origin of the virtual environment.

## ⬡ Terrain and Collision

The lunar surface is provided through a Cesium 3D Tileset.

The following settings are important for the environment:

- **Cesium ion Asset ID:** `2684829`
- **Maximum Screen Space Error:** `16`
- **Maximum Simultaneous Tile Loads:** `20`
- **Maximum Cached Bytes:** `536870912`
- **Create Physics Meshes:** Enabled

The Create Physics Meshes setting allows Cesium to generate physics meshes for terrain interaction.

## ▣ Environmental Assets

The environment includes objects that help represent a lunar exploration area, such as:

- Lunar rocks and boulders
- Base camp structures
- Solar panels
- Charging station structures
- Water storage structures and platforms

These objects help create a more complete lunar environment for the digital twin.

## ✓ Testing and Validation

The digital environment should be checked to ensure:

- The lunar terrain loads correctly.
- Environmental objects appear in the scene.
- Terrain physics meshes are generated.
- Environment objects are positioned correctly.
- The scene works in Play Mode.

## ✧ Future Improvements

Possible improvements include adding more lunar surface details, improving environmental object placement, and testing terrain collision across different areas of the lunar surface.