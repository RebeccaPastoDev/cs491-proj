# Lunar A* integration

## Run the demonstration

Open `My project` with Unity 6000.4.0f1. Open **Assets/Scenes/LunarSouthPole_AStarDemo.unity**, then enter Play mode.

The demo obtains the most detailed available Cesium Moon heights and waits for the local collision surface to agree with them. The top-left status identifies the active demonstration once the site is ready. This requires a working Cesium connection.

- **Site overview** shows the current rock-field route with START, DESTINATION, ROCK FIELD and ROVER labels.
- **Rover view** shows the rover with the surrounding lunar landscape.
- The longer obstacle test starts automatically after loading. **Start** can replan or resume it. A cyan ring marks the destination beyond an existing rock cluster.
- **Pause**, then **Start**, pauses and resumes movement.
- **Reset** restores the corrected start and simulation battery, leaving the longer destination selected.
- While stopped, right-click loaded terrain in either view to choose another destination, then press Start. Destinations snap to the grid, within half a cell on each horizontal axis. The planner is local; a distant overview destination may exceed its grid or battery limits.

The scene's `MissionControlManager` has the planner settings: Python executable, battery, speed, slope limit, grid spacing and rover footprint. `/usr/bin/python3` works on this Mac; another desktop may need its own Python 3 executable path. No pip packages, server, open port or separate terminal session is required.

## Longer obstacle test

The current demo places the destination beyond the nearest existing rock cluster, rather than adding artificial obstacles. Its direct distance is approximately **1.90 km**. Unity confirmed that the direct route intersects an obstacle. A* returned **193 waypoints** over **2,688.33 m**, avoiding the grid's **1,058 blocked cells**, with an estimated **144.999 Wh** trip cost.

The test uses **250 Wh of simulated battery** and **50× visual playback**. The Python battery model still uses **0.5 m/s**; fast-forward changes presentation time, not the model's driving speed or segment energy. At 50×, the full simulated trip takes roughly two minutes to watch. Grid spacing is chosen to fit the longer route within the 161-cell limit. Grid occupancy includes half a cell of extra padding so a coarse cell's edge cannot cut through a rock. Movement and route edges still check the actual rover footprint.

`LunarEnvironmentSetup.longObstacleDemo` and `autoStartObstacleDemo` control this mode. Disabling the long test restores the short base-camp demonstration. The grid request and approved path are saved in `LastTerrainGrid.json` and `LastRouteReport.json` for reproducible checks; they contain terrain and route data, not credentials.

The recorded Unity request was rerun through the original Python planner. Validation confirmed the same 193-cell route and energy, every waypoint on a walkable cell, four-neighbour continuity, and a physically blocked direct route. Successful full playback is reported separately from planning and route validation.

## Terrain and environment correction

The first prototype incorrectly used a high, coarse collision surface near Y=197. The precise height query confirmed that the real surface at the rover is near Y=-3720.175, close to the team's saved environment. The earlier conclusion that the entire environment was thousands of metres misplaced was incorrect: it was the selected collision surface that was wrong.

The demo registers the rover camera with Cesium, increases its viewing distance from 1 km to 30 km, uses a dark background and adds a site overview. Terrain raycasts are restricted to the local site's elevation range and choose the surface nearest the start elevation, avoiding distant overlapping coarse tiles.

`LunarEnvironmentSetup` aligns 21 imported object groups at runtime using `SampleHeightMostDetailed`. It retains their horizontal arrangement, rotations, scale and materials. Shelter, solar installation and water tanks stay assembled; individual rock bases are adjusted with a small embed. The rover is placed just above the precise surface. The solid location-marker cube is hidden in the demo and replaced with a cyan ring.

These placement changes happen only during Play in the separate demo scene. The saved team scene's prop positions are preserved. `EnvironmentPlacementReport.txt` records the queried heights and vertical adjustments. The source scene and intermediate integration scene are backed up in `IntegrationBackup`.

## Planner and movement

`Assets/StreamingAssets/AStarPlanner/planner.py` and `battery.py` are unchanged copies of the originals from `Documents/NASA`. `route_request.py` receives JSON on stdin, invokes the original four-neighbour energy-aware A*, and returns JSON containing cells and segment energy. Unity launches a Python process per request with a 15-second limit.

Grid rows increase along Unity world +Z, columns along +X, and elevations use world Y in metres. A fixed Cesium world frame is captured per route. Origin/transform changes stop movement and require replanning. The local grid is limited to 161 by 161 cells with a configurable margin.

The sampler accepts Cesium and Unity Terrain colliders. Missing cells, obstacles and excessive slopes are blocked. Obstacles are inflated by the rover footprint, at least the root collider bounds. Returned edges are checked at intervals no larger than 0.5 m, and movement rechecks terrain and obstacles. Missing terrain is never replaced with a fabricated flat grid. A refinement that invalidates a route stops the rover; it does not silently drive across it.

Movement is **kinematic**. The original manual wheel controller is disabled while the mission owns movement. This demonstrates planning, terrain sampling, route following and battery accounting, rather than traction, trailer dynamics, turning radius or lunar soil mechanics.

The defaults of 1 Wh, 0.5 m/s and 20 degrees, and the constants in `battery.py`, remain simulation assumptions rather than measured rover specifications. Energy is for the one-way trip; idle draw while paused, return reserves, charging and payload effects are not included. Segment energy comes from Python and is prorated as the rover moves.

The bridge supports desktop Unity. WebGL, mobile and standalone VR headsets need a planner service or C# port. The team's XR setup is preserved, so missing VR-runtime warnings can appear on this Mac. Cesium's large-triangle collision warnings can also remain.

## Verification

Run `python3 -m unittest discover -v` in `Assets/StreamingAssets/AStarPlanner`.

All 12 tests pass: six original planner tests and six bridge tests covering energy-based detours, steep uphill/downhill, insufficient battery, no route, same-cell routes, malformed requests, a real subprocess JSON exchange, and consistency between segment and total energy.

Unity compiled the integration. The corrected detailed-surface test returned five waypoints, reached the base-camp destination, and used approximately **0.422 Wh**, leaving **0.578 Wh**. Pause/Resume/Reset and terrain destination selection were also tested. The overview visibly shows the team's original site objects and lunar terrain.

The earlier 0.566 Wh result was obtained from the wrong coarse collision surface and is superseded by the detailed-surface test.
