using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CesiumForUnity;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using Debug = UnityEngine.Debug;

// Desktop simulation bridge. Coordinates are metres in the current Unity world frame.
public class LunarAStarMission : MonoBehaviour
{
    public Transform rover;
    public Transform destination;
    public Camera viewCamera;
    [Header("Simulation assumptions (not measured rover specifications)")]
    public float batteryWh = 1f;
    public float speed = 0.5f;
    [Tooltip("Visual playback only; Python energy still uses the model speed.")]
    public float playbackMultiplier = 1f;
    public float RouteLengthMetres { get; private set; }
    public int BlockedCellCount { get; private set; }
    public bool DirectRouteBlocked { get; private set; }
    public float maxSlope = 20f;
    public float cellSize = 2f;
    public float roverRadius = 1.8f;
    public float roverHeight = 3f;
    public float rayHeight = 5000f;
    public int marginCells = 8;
    [Tooltip("Python 3 executable. No pip packages are needed.")]
    public string pythonExecutable = "/usr/bin/python3";
    public string State { get; private set; } = "STANDBY";
    public string Detail { get; private set; } = "Start plans to base camp. Right-click terrain to choose another destination.";
    public float EstimatedWh { get; private set; }
    public float ConsumedWh { get; private set; }
    public float CurrentSpeed { get; private set; }
    public int WaypointCount => path.Count;
    public int CompletedWaypoints => waypoint;
    public string DestinationName => destination == null ? "NONE" : destination.name;

    [Serializable] public class Cell { public int row; public int column; }
    [Serializable] public class Request
    {
        public int rows, columns;
        public float[] heights;
        public bool[] walkable;
        public Cell start, goal;
        public float battery, spacing, speed, maxSlope;
    }
    [Serializable] public class Response
    {
        public bool feasible, hasEnergyEstimate;
        public string reason, error;
        public float energyWh;
        public Cell[] waypoints;
        public float[] segmentEnergyWh;
    }
    readonly List<Vector3> path = new List<Vector3>();
    int waypoint, generation;
    float offsetY, initialBattery;
    float[] segmentEnergy = Array.Empty<float>();
    Transform pickedDestination;
    bool frameCaptured;
    Vector3 initialPosition;
    Quaternion initialRotation;
    Rigidbody body;
    bool originalKinematic, originalGravity;
    MonoBehaviour manualDriver;
    bool originalManual;
    LineRenderer line;
    Material routeMaterial;
    Vector3 framePosition;
    Quaternion frameRotation;
    Vector3 frameScale;
    CesiumGeoreference georeference;
    double originX, originY, originZ;
    TMP_Text routeText, feasibilityText, estimateText, batteryText, warningText, slopeText, obstacleText;
    float currentSlope, plannedSpeed;

    void Awake()
    {
        if (rover == null) rover = GameObject.Find("RoverCleanMain")?.transform;
        if (destination == null) destination = GameObject.Find("BaseCampMarker")?.transform;
        if (rover == null) { Fail("Rover is not assigned."); return; }
        initialPosition = rover.position;
        initialRotation = rover.rotation;
        initialBattery = batteryWh;
        var footprint = rover.GetComponent<Collider>();
        if (footprint != null)
        {
            roverRadius = Mathf.Max(roverRadius, footprint.bounds.extents.x, footprint.bounds.extents.z);
            roverHeight = Mathf.Max(roverHeight, footprint.bounds.size.y);
        }
        body = rover.GetComponent<Rigidbody>();
        if (body != null)
        {
            originalKinematic = body.isKinematic;
            originalGravity = body.useGravity;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            body.useGravity = false;
        }
        foreach (var behaviour in rover.GetComponents<MonoBehaviour>())
            if (behaviour.GetType().Name == "RoverControllerNew")
            { manualDriver = behaviour; originalManual = behaviour.enabled; behaviour.enabled = false; }
        var agent = rover.GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;
        georeference = FindAnyObjectByType<CesiumGeoreference>();
        CaptureFrame();
        var lineObject = new GameObject("AStar Route Preview");
        lineObject.transform.SetParent(transform, false);
        line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.widthMultiplier = 0.3f;
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader != null)
        {
            routeMaterial = new Material(shader);
            routeMaterial.SetColor("_BaseColor", Color.cyan);
            line.sharedMaterial = routeMaterial;
        }
        foreach (var label in FindObjectsByType<TMP_Text>())
            switch (label.name)
            {
                case "RoverRoute": routeText = label; break;
                case "Feasibility": feasibilityText = label; break;
                case "BatteryCostEstimate": estimateText = label; break;
                case "BatteryLevel": batteryText = label; break;
                case "WarningDetector": warningText = label; break;
                case "TerrainSlope": slopeText = label; break;
                case "ObstacleDetect": obstacleText = label; break;
            }
        // Use the serialized label references when names differ between scenes.
        RefreshLabels();
    }

    public void StartMission()
    {
        if (State == "MOVING" || State == "PLANNING") return;
        var environment = GetComponent<LunarEnvironmentSetup>();
        if (environment != null && !environment.Ready) { Detail = environment.Status; return; }
        if (State == "PAUSED" && path.Count > 0 && FrameUnchanged())
        { State = "MOVING"; Detail = $"Following A* route • {playbackMultiplier:F0}× playback"; return; }
        generation++;
        path.Clear(); waypoint = 0; EstimatedWh = 0;
        if (line != null) line.positionCount = 0;
        if (rover == null || destination == null) { Fail("Assign a rover and destination."); return; }
        if (cellSize <= 0 || speed <= 0 || roverRadius <= 0 || roverHeight <= 0 || rayHeight <= 0 ||
            marginCells < 1 || maxSlope < 0 || maxSlope >= 90 || batteryWh < 0)
        { Fail("Check the mission settings: spacing, speed, footprint, slope and battery."); return; }
        if (Application.platform != RuntimePlatform.OSXEditor && Application.platform != RuntimePlatform.OSXPlayer &&
            Application.platform != RuntimePlatform.WindowsEditor && Application.platform != RuntimePlatform.WindowsPlayer &&
            Application.platform != RuntimePlatform.LinuxEditor && Application.platform != RuntimePlatform.LinuxPlayer)
        { Fail("The Python bridge requires desktop Unity. A separate service is needed for VR/mobile or Web builds."); return; }
        State = "PLANNING"; Detail = "Sampling loaded terrain…";
        if (!FrameUnchanged()) { initialPosition = rover.position; initialRotation = rover.rotation; }
        CaptureFrame();
        StartCoroutine(PlanRoute(generation));
    }

    public void PauseMission()
    {
        if (State != "MOVING" && State != "PLANNING") return;
        generation++;
        State = "PAUSED"; Detail = "Mission paused."; CurrentSpeed = 0;
    }

    public void ConfigureDemoRoute(Vector3 point, float capacityWh, float spacing, int margin, float playback)
    {
        ResetMission();
        batteryWh = initialBattery = capacityWh;
        cellSize = spacing; marginCells = margin;
        playbackMultiplier = Mathf.Max(1, playback);
        destination.position = point;
        destination.name = "Beyond rock field";
        Detail = "Long obstacle test ready. Press Start.";
    }

    public void SetRouteWidth(float width) { if (line != null) line.widthMultiplier = width; }

    public void SetSurfaceStart()
    {
        initialPosition = rover.position; initialRotation = rover.rotation;
        CaptureFrame();
    }

    public void ResetMission()
    {
        generation++; StopAllCoroutines(); path.Clear(); waypoint = 0;
        EstimatedWh = 0; ConsumedWh = 0; CurrentSpeed = 0; batteryWh = initialBattery;
        if (line != null) line.positionCount = 0;
        // An old world position is invalid after a Cesium origin change.
        if (rover != null && (georeference == null || FrameUnchanged()))
            rover.SetPositionAndRotation(initialPosition, initialRotation);
        State = "STANDBY"; Detail = "Start plans to base camp. Right-click terrain to choose another destination.";
    }

    void CaptureFrame()
    {
        if (georeference == null) return;
        frameCaptured = true;
        framePosition = georeference.transform.position;
        frameRotation = georeference.transform.rotation;
        frameScale = georeference.transform.lossyScale;
        originX = georeference.ecefX;
        originY = georeference.ecefY;
        originZ = georeference.ecefZ;
    }

    bool FrameUnchanged()
    {
        return georeference == null ||
            (frameCaptured && georeference.transform.position == framePosition && georeference.transform.rotation == frameRotation &&
             georeference.transform.lossyScale == frameScale &&
             Math.Abs(georeference.ecefX - originX) < 0.001 &&
             Math.Abs(georeference.ecefY - originY) < 0.001 &&
             Math.Abs(georeference.ecefZ - originZ) < 0.001);
    }

    bool GroundCollider(Collider col)
    {
        return col is TerrainCollider || col.GetComponentInParent<Cesium3DTileset>() != null;
    }
    bool Ignored(Collider col)
    {
        return col.transform.IsChildOf(rover) ||
            (destination != null && col.transform.IsChildOf(destination));
    }
    bool Surface(Vector3 at, out RaycastHit ground)
    {
        ground = default;
        // Restrict the query to the local site. A distant coarse Moon tile can overlap
        // the ray thousands of metres above the detailed surface.
        float range = Mathf.Min(rayHeight, 250f);
        var hits = Physics.RaycastAll(new Vector3(at.x, initialPosition.y + range, at.z),
            Vector3.down, range * 2, ~0, QueryTriggerInteraction.Ignore);
        float nearest = float.PositiveInfinity;
        foreach (var hit in hits)
            if (!Ignored(hit.collider) && GroundCollider(hit.collider) && Mathf.Abs(hit.point.y - initialPosition.y) < nearest)
            { ground = hit; nearest = Mathf.Abs(hit.point.y - initialPosition.y); }
        return nearest < float.PositiveInfinity;
    }
    bool ClearAt(Vector3 point, float padding = 0)
    {
        foreach (var col in Physics.OverlapBox(point + Vector3.up * (roverHeight * 0.5f + 0.2f),
                     new Vector3(roverRadius + padding, roverHeight * 0.5f, roverRadius + padding),
                     Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            if (!Ignored(col) && !GroundCollider(col)) return false;
        return true;
    }

    IEnumerator PlanRoute(int id)
    {
        Physics.SyncTransforms();
        Vector3 start = rover.position, goal = destination.position;
        int dc = Mathf.RoundToInt((goal.x - start.x) / cellSize);
        int dr = Mathf.RoundToInt((goal.z - start.z) / cellSize);
        int minC = Mathf.Min(0, dc) - marginCells, minR = Mathf.Min(0, dr) - marginCells;
        int cols = Math.Abs(dc) + 2 * marginCells + 1, rows = Math.Abs(dr) + 2 * marginCells + 1;
        if (rows > 161 || cols > 161)
        { Fail("Destination exceeds the local grid. Increase cell size or choose a closer destination."); yield break; }
        Vector3 origin = new Vector3(start.x + minC * cellSize, 0, start.z + minR * cellSize);
        var request = new Request {
            rows = rows, columns = cols, heights = new float[rows * cols], walkable = new bool[rows * cols],
            start = new Cell { row = -minR, column = -minC },
            goal = new Cell { row = dr - minR, column = dc - minC },
            spacing = cellSize, speed = speed, maxSlope = maxSlope, battery = batteryWh - ConsumedWh
        };
        for (int r = 0; r < rows; r++)
        {
            if (id != generation) yield break;
            for (int c = 0; c < cols; c++)
            {
                int k = r * cols + c;
                Vector3 at = origin + new Vector3(c * cellSize, 0, r * cellSize);
                if (Surface(at, out var hit))
                {
                    request.heights[k] = hit.point.y;
                    request.walkable[k] = Vector3.Angle(hit.normal, Vector3.up) <= maxSlope && ClearAt(hit.point, cellSize * 0.5f);
                }
            }
            Detail = $"Sampling terrain: {r + 1}/{rows} rows";
            yield return null;
        }
        if (id != generation) yield break;
        if (!FrameUnchanged()) { Fail("Cesium origin changed. Plan again."); yield break; }
        if (!request.walkable[request.start.row * cols + request.start.column] ||
            !request.walkable[request.goal.row * cols + request.goal.column])
        { Fail("Start or destination has missing terrain, an obstacle or excessive slope. Wait for terrain or choose another destination."); yield break; }
        var footprint = rover.GetComponent<Collider>();
        offsetY = footprint == null ? 0.3f : Mathf.Max(0.15f, rover.position.y - footprint.bounds.min.y + 0.1f);
        BlockedCellCount = 0;
        foreach (bool open in request.walkable) if (!open) BlockedCellCount++;
        DirectRouteBlocked = false;
        int directSamples = Mathf.Max(2, Mathf.CeilToInt(Vector3.Distance(start, goal) / 2f));
        for (int i = 0; i <= directSamples; i++)
        {
            Vector3 point = Vector3.Lerp(start, goal, (float)i / directSamples);
            if (Surface(point, out var surface) && !ClearAt(surface.point)) { DirectRouteBlocked = true; break; }
        }
        plannedSpeed = speed;
        string json = JsonUtility.ToJson(request);
        string script = Path.Combine(Application.streamingAssetsPath, "AStarPlanner", "route_request.py");
        string executable = pythonExecutable;
        Detail = "Running Python A*…";
        Task<string> task = Task.Run(() => RunPython(executable, script, json));
        while (!task.IsCompleted) { if (id != generation) yield break; yield return null; }
        if (id != generation) yield break;
        if (task.IsFaulted) { Fail("Python planner failed: " + task.Exception.GetBaseException().Message); yield break; }
        Response response = null;
        try { response = JsonUtility.FromJson<Response>(task.Result); }
        catch (Exception error) { Fail("Invalid planner response: " + error.Message); }
        if (response == null) yield break;
        EstimatedWh = response.energyWh;
        if (!response.feasible) { Fail("Planner: " + response.reason + " " + response.error); yield break; }
        if (!FrameUnchanged()) { Fail("Cesium origin changed. Plan again."); yield break; }
        if (response.waypoints == null || response.waypoints.Length == 0)
        { Fail("Planner returned an empty route."); yield break; }
        if (response.segmentEnergyWh == null || response.segmentEnergyWh.Length != response.waypoints.Length - 1 ||
            response.waypoints[0].row != request.start.row || response.waypoints[0].column != request.start.column ||
            response.waypoints[response.waypoints.Length-1].row != request.goal.row ||
            response.waypoints[response.waypoints.Length-1].column != request.goal.column)
        { Fail("Planner response does not match the requested trip."); yield break; }
        foreach (float e in response.segmentEnergyWh)
            if (float.IsNaN(e) || float.IsInfinity(e) || e < 0)
            { Fail("Invalid route energy."); yield break; }
        float sumEnergy = 0;
        for (int i = 0; i < response.segmentEnergyWh.Length; i++)
        {
            sumEnergy += response.segmentEnergyWh[i];
            var a = response.waypoints[i]; var b = response.waypoints[i+1];
            if (Math.Abs(a.row - b.row) + Math.Abs(a.column - b.column) != 1)
            { Fail("Planner returned non-adjacent cells."); yield break; }
        }
        if (float.IsNaN(EstimatedWh) || float.IsInfinity(EstimatedWh) || EstimatedWh < 0 ||
            Mathf.Abs(sumEnergy - EstimatedWh) > Mathf.Max(0.00001f, EstimatedWh * 0.0001f) ||
            EstimatedWh > request.battery + 0.00001f)
        { Fail("Planner returned an inconsistent energy estimate."); yield break; }
        segmentEnergy = response.segmentEnergyWh;
        foreach (var cell in response.waypoints)
        {
            if (cell.row < 0 || cell.row >= rows || cell.column < 0 || cell.column >= cols ||
                !request.walkable[cell.row * cols + cell.column])
            { path.Clear(); Fail("Planner returned an invalid cell."); yield break; }
            path.Add(origin + new Vector3(cell.column * cellSize,
                request.heights[cell.row * cols + cell.column] + offsetY, cell.row * cellSize));
        }
        // Validate edges at finer spacing so narrow obstacles and gaps cannot hide between grid centres.
        for (int i = 1; i < path.Count; i++)
        {
            if (Vector3.Distance(new Vector3(path[i].x, 0, path[i].z), new Vector3(path[i-1].x, 0, path[i-1].z)) > cellSize * 1.01f)
            { path.Clear(); Fail("Planner returned non-adjacent cells."); yield break; }
            int samples = Mathf.Max(2, Mathf.CeilToInt(cellSize / 0.5f));
            for (int j = 1; j <= samples; j++)
            {
                Vector3 p = Vector3.Lerp(path[i-1], path[i], (float)j / samples);
                if (!Surface(p, out var hit) || !ClearAt(hit.point) || Vector3.Angle(hit.normal, Vector3.up) > maxSlope ||
                    Mathf.Abs(hit.point.y + offsetY - p.y) > 0.5f)
                { path.Clear(); Fail("Route crosses an unresolved gap or obstacle. Try a smaller cell size."); yield break; }
            }
        }
        RouteLengthMetres = 0;
        for (int i = 1; i < path.Count; i++) RouteLengthMetres += Vector3.Distance(path[i-1], path[i]);
        // Save the sampled, non-sensitive terrain request and approved route for reproducible checks.
        try
        {
            File.WriteAllText(Path.Combine(Application.dataPath, "../../LastTerrainGrid.json"), json);
            var report = new RouteReport { waypoints = path.ToArray(), lengthMetres = RouteLengthMetres,
                blockedCells = BlockedCellCount, directRouteBlocked = DirectRouteBlocked,
                energyWh = EstimatedWh, playbackMultiplier = playbackMultiplier };
            File.WriteAllText(Path.Combine(Application.dataPath, "../../LastRouteReport.json"), JsonUtility.ToJson(report, true));
        }
        catch (Exception error) { Debug.LogWarning("Route report: " + error.Message); }
        // Align the saved spawn to the loaded terrain before driving; do not drive a vertical segment.
        rover.position = path[0];
        if (ConsumedWh == 0) initialPosition = path[0];
        Physics.SyncTransforms();
        line.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++) line.SetPosition(i, path[i] + Vector3.up * 0.1f);
        waypoint = path.Count > 1 ? 1 : path.Count;
        State = waypoint == path.Count ? "ARRIVED" : "MOVING";
        Detail = waypoint == path.Count ? "Already at destination." : $"Following A* route • {playbackMultiplier:F0}× playback";
        Debug.Log($"AStar: {path.Count} waypoints to {DestinationName}, {RouteLengthMetres:F1} m, {EstimatedWh:F4} Wh, {BlockedCellCount} blocked cells, direct route blocked={DirectRouteBlocked}, playback={playbackMultiplier}x (simulation model).");
    }

    [Serializable] class RouteReport
    {
        public Vector3[] waypoints;
        public float lengthMetres, energyWh, playbackMultiplier;
        public int blockedCells;
        public bool directRouteBlocked;
    }

    static string RunPython(string executable, string script, string json)
    {
        var info = new ProcessStartInfo(executable) {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true
        };
        info.ArgumentList.Add(script);
        info.Environment["PYTHONDONTWRITEBYTECODE"] = "1";
        using (var process = Process.Start(info))
        {
            var output = process.StandardOutput.ReadToEndAsync();
            var errors = process.StandardError.ReadToEndAsync();
            process.StandardInput.Write(json); process.StandardInput.Close();
            if (!process.WaitForExit(15000)) { process.Kill(); throw new TimeoutException("Planner exceeded 15 seconds."); }
            if (process.ExitCode != 0) throw new Exception(errors.GetAwaiter().GetResult());
            return output.GetAwaiter().GetResult();
        }
    }

    void FixedUpdate()
    {
        CurrentSpeed = 0;
        if (State != "MOVING" || rover == null || waypoint >= path.Count) return;
        if (!FrameUnchanged()) { Fail("Cesium origin changed. Plan again before moving."); return; }
        if (speed != plannedSpeed) { path.Clear(); Fail("Speed changed. Plan again to update the energy estimate."); return; }
        Vector3 from = rover.position;
        Vector3 to = Vector3.MoveTowards(from, path[waypoint], speed * Time.fixedDeltaTime * Mathf.Max(1, playbackMultiplier));
        if (!Surface(to, out var hit) || !ClearAt(hit.point) || Vector3.Angle(hit.normal, Vector3.up) > maxSlope ||
            Mathf.Abs(to.y - (hit.point.y + offsetY)) > 0.5f)
        { Fail("Terrain or obstacle changed. Rover stopped; plan again."); return; }
        currentSlope = Vector3.Angle(hit.normal, Vector3.up);
        float distance = Vector3.Distance(from, to);
        // Use segment costs from Python so the simulation always shares the original battery model.
        float segmentLength = Vector3.Distance(path[waypoint-1], path[waypoint]);
        float energy = segmentLength > 0 ? segmentEnergy[waypoint-1] * distance / segmentLength : 0;
        if (ConsumedWh + energy > batteryWh) { Fail("Battery depleted. Rover stopped."); return; }
        Vector3 direction = to - from; direction.y = 0;
        if (direction.sqrMagnitude > 0.000001f) rover.rotation = Quaternion.LookRotation(direction);
        rover.position = to;
        ConsumedWh += energy; CurrentSpeed = distance / (Time.fixedDeltaTime * Mathf.Max(1, playbackMultiplier));
        if (Vector3.Distance(to, path[waypoint]) < 0.0001f)
        {
            waypoint++;
            if (waypoint >= path.Count) { State = "ARRIVED"; Detail = "Destination reached."; CurrentSpeed = 0; }
        }
    }

    void Update()
    {
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null && mouse.rightButton.wasPressedThisFrame && State != "MOVING" && State != "PLANNING")
        {
            var camera = viewCamera != null ? viewCamera : (rover == null ? Camera.main : rover.GetComponentInChildren<Camera>());
            Detail = "Point at visible loaded terrain to choose a destination.";
            if (camera != null && Physics.Raycast(camera.ScreenPointToRay(mouse.position.ReadValue()), out var hit,
                rayHeight * 2, ~0, QueryTriggerInteraction.Ignore) && GroundCollider(hit.collider))
            {
                generation++; path.Clear(); waypoint = 0; EstimatedWh = 0;
                if (line != null) line.positionCount = 0;
                if (pickedDestination == null) pickedDestination = new GameObject("Selected terrain destination").transform;
                pickedDestination.position = hit.point; destination = pickedDestination;
                State = "STANDBY"; Detail = "Destination selected. Press Start.";
            }
        }
        RefreshLabels();
    }
    void RefreshLabels()
    {
        if (slopeText != null) slopeText.text = $"SLOPE: {currentSlope:F1}°";
        if (obstacleText != null) obstacleText.text = path.Count > 0 ? "OBSTACLES: ROUTE CHECKED" : "OBSTACLES: NOT CHECKED";
        if (routeText != null) routeText.text = $"ROUTE: {DestinationName} ({waypoint}/{path.Count})";
        if (feasibilityText != null) feasibilityText.text = "FEASIBILITY: " + State;
        if (estimateText != null) estimateText.text = $"TRIP: {RouteLengthMetres/1000:F2} km | {EstimatedWh:F2} Wh (SIM)";
        if (batteryText != null) batteryText.text = $"BATTERY: {Mathf.Max(0, batteryWh - ConsumedWh):F3} Wh (SIM)";
        if (warningText != null) warningText.text = Detail;
    }
    void Fail(string reason) { State = "BLOCKED"; Detail = reason; CurrentSpeed = 0; Debug.LogWarning("AStar: " + reason); }
    void OnDisable() { generation++; StopAllCoroutines(); CurrentSpeed = 0; }
    void OnDestroy()
    {
        if (routeMaterial != null) Destroy(routeMaterial);
        if (pickedDestination != null) Destroy(pickedDestination.gameObject);
        if (body != null) { body.isKinematic = originalKinematic; body.useGravity = originalGravity; }
        if (manualDriver != null) manualDriver.enabled = originalManual;
    }
}
