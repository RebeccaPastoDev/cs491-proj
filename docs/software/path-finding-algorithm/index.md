# A* Pathfinding Algorithm

## 🚀 Overview

The A* (A-Star) Pathfinding Algorithm is used to help the lunar rover find an efficient route between a starting position and a destination.

The goal is to adapt A* for lunar navigation by considering terrain elevation, obstacles, slopes, and travel distance.

## ⚙️ How A* Works

The algorithm evaluates possible routes using movement costs and estimated remaining distance to find a suitable path.

The process includes:

- 📍 **Starting Point:** Identifies the rover's current position.
- 🎯 **Destination:** Defines where the rover needs to go.
- 🗺️ **Terrain Analysis:** Examines elevation, slopes, and obstacles.
- 🔍 **Path Search:** Compares possible routes using movement costs.
- 🛣️ **Route Selection:** Finds an efficient route to the destination.

## 🌕 Terrain Data Acquisition

Cesium for Unity is used as the source of lunar terrain data.

The terrain sampling process includes:

- Collecting latitude, longitude, and elevation information.
- Selecting a sampling location near the lunar south pole (88°S).
- Using terrain data with approximately 5-meter-per-pixel resolution.
- Sampling neighboring grid locations to build terrain information.

## 🧩 Graph Generation

The terrain is converted into a graph made of nodes and edges.

- **Nodes:** Represent individual terrain grid positions.
- **Edges:** Represent possible movements between neighboring nodes.
- **8-Connected Grid:** Allows movement in eight directions, including horizontal, vertical, and diagonal directions.
- **Edge Costs:** Consider distance, slope, traversability, and estimated energy consumption.
- **Blocked Paths:** Unsafe or blocked movements are excluded from the graph.

## 🔋 Terrain Costs and Energy Considerations

The pathfinding system considers different factors when evaluating possible routes.

- 📏 **Distance:** Measures the distance between nodes.
- ⛰️ **Slope:** Considers changes in terrain elevation.
- 🚧 **Obstacles:** Identifies movements that may not be safe.
- ⚡ **Energy:** Estimates the energy required to travel along a route.

These factors help the algorithm select a route that is suitable for lunar rover navigation.

## 📊 Current Progress

The project has made progress in developing the terrain data and graph-generation components.

- Cesium for Unity has been integrated as the source of terrain data.
- Terrain sampling methods have been developed.
- The graph-generation approach uses an 8-connected grid.
- Edge costs are designed to account for terrain conditions and estimated energy use.

Further work includes completing pathfinding integration and testing the system with the rover.

## 🔮 Future Improvements for the Next Team

The next team can continue developing the pathfinding system by focusing on:

- 🛣️ Completing A* implementation and connecting it to the rover.
- 🗺️ Improving terrain analysis and obstacle detection.
- 🔋 Developing battery and energy consumption modeling.
- 🎯 Adding destination selection and automatic route planning.
- 🧪 Testing different routes to evaluate safety and algorithm performance.
- 🔄 Improving communication between the pathfinding algorithm, Unity environment, and rover system.

The long-term goal is to create a reliable navigation system that allows the rover to select and follow suitable routes across the lunar surface.