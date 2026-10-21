using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CesiumForUnity;
using Unity.Mathematics;
using UnityEngine;

// Runtime placement for the demonstration scene. The team's source scene stays intact.
[DefaultExecutionOrder(-50)]
public class LunarEnvironmentSetup : MonoBehaviour
{
    public bool Ready { get; private set; }
    public string Status { get; private set; } = "Loading detailed Moon terrain…";
    public Camera ActiveCamera => overview && siteCamera != null ? siteCamera : roverCamera;
    public bool overview;
    public bool longObstacleDemo = true;
    public bool autoStartObstacleDemo = true;
    Bounds testRockBounds;
    Vector3 testStart, testGoal;
    string testRockName;
    float testDistance;
    LunarAStarMission mission;
    CesiumGeoreference reference;
    Cesium3DTileset tiles;
    Camera roverCamera, siteCamera;
    readonly List<Transform> placements = new List<Transform>();
    readonly List<Bounds> bounds = new List<Bounds>();
    Material markerMaterial;
    Vector3 sampledStart;
    float radius;
    Vector3 siteCentre;

    IEnumerator Start()
    {
        mission = GetComponent<LunarAStarMission>();
        reference = FindAnyObjectByType<CesiumGeoreference>();
        tiles = GameObject.Find("Cesium Moon")?.GetComponent<Cesium3DTileset>();
        if (mission == null || mission.rover == null || reference == null || tiles == null)
        { Status = "Missing rover or Cesium terrain."; yield break; }
        roverCamera = mission.rover.GetComponentInChildren<Camera>();
        if (roverCamera == null) { Status = "Rover camera is missing."; yield break; }
        var manager = reference.GetComponent<CesiumCameraManager>();
        manager.useMainCamera = false;
        if (!manager.additionalCameras.Contains(roverCamera)) manager.additionalCameras.Add(roverCamera);
        roverCamera.farClipPlane = 30000;
        roverCamera.clearFlags = CameraClearFlags.SolidColor;
        roverCamera.backgroundColor = new Color(0.005f, 0.008f, 0.015f);
        // The fixed camera at the reference origin is not a view of the rover's site.
        var fixedCamera = GameObject.Find("Main Camera")?.GetComponent<Camera>();
        if (fixedCamera != null && fixedCamera != roverCamera) fixedCamera.enabled = false;

        var group = GameObject.Find("BaseCampObjects")?.transform;
        if (group != null)
            foreach (Transform child in group)
            {
                if (!child.gameObject.activeInHierarchy) continue;
                if (child.name == "Rocks")
                { foreach (Transform part in child) AddPlacement(part); }
                else AddPlacement(child);
            }
        // Request exact heights rather than using whichever coarse collision mesh loaded first.
        var locations = new double3[placements.Count + 2];
        locations[0] = Cartographic(mission.rover.position);
        locations[1] = Cartographic(mission.destination.position);
        for (int i = 0; i < placements.Count; i++) locations[i+2] = Cartographic(bounds[i].center);
        CesiumSampleHeightResult result = null;
        float deadline = Time.realtimeSinceStartup + 150;
        yield return null;
        // The native tileset initializes in Start; a query in the same frame can return no samples.
        while (Time.realtimeSinceStartup < deadline)
        {
            Status = $"Loading detailed Moon heights… ({tiles.ComputeLoadProgress():F0}%)";
            yield return new WaitForSeconds(2);
            Task<CesiumSampleHeightResult> query = tiles.SampleHeightMostDetailed(locations);
            while (!query.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
            if (!query.IsCompleted) break;
            if (query.IsFaulted) { Debug.LogWarning("Detailed heights: " + query.Exception.GetBaseException().Message); continue; }
            result = query.Result;
            if (result.sampleSuccess[0] && result.sampleSuccess[1]) break;
            if (result.warnings != null) foreach (var warning in result.warnings) Debug.LogWarning("Detailed heights: " + warning);
            Debug.LogWarning($"Detailed heights: rover={result.sampleSuccess[0]}, marker={result.sampleSuccess[1]}");
        }
        if (result == null || !result.sampleSuccess[0] || !result.sampleSuccess[1])
        { Status = "Detailed terrain unavailable at rover or destination. Check Console for Cesium response."; yield break; }
        var report = new StringBuilder("Detailed Moon placement report (runtime demo)\n");
        sampledStart = World(result.longitudeLatitudeHeightPositions[0]);
        Vector3 spawn = sampledStart;
        var collider = mission.rover.GetComponent<Collider>();
        spawn.y += collider == null ? 0.3f : Mathf.Max(0.15f, mission.rover.position.y - collider.bounds.min.y + 0.1f);
        report.AppendLine($"Rover: {mission.rover.position:F3} -> {spawn:F3}");
        mission.rover.position = spawn;
        mission.destination.position = World(result.longitudeLatitudeHeightPositions[1]);
        ShowDestinationRing();
        int aligned = 0;
        for (int i = 0; i < placements.Count; i++)
        {
            if (!result.sampleSuccess[i+2]) { report.AppendLine("Unavailable: " + placements[i].name); continue; }
            Vector3 ground = World(result.longitudeLatitudeHeightPositions[i+2]);
            float delta = ground.y - bounds[i].min.y;
            // A small embed keeps imported rock bases from appearing to float.
            if (placements[i].name.StartsWith("Rocks_")) delta -= bounds[i].size.y * 0.04f;
            placements[i].position += Vector3.up * delta;
            report.AppendLine($"{placements[i].name}: bottom {bounds[i].min.y:F2} -> {ground.y:F2}, shift {delta:F2} m");
            aligned++;
        }
        Physics.SyncTransforms();
        mission.rayHeight = 250;
        mission.SetSurfaceStart();
        if (longObstacleDemo)
        {
            float closest = float.PositiveInfinity;
            foreach (var part in placements)
            {
                if (!part.name.StartsWith("Rocks_")) continue;
                var renderer = part.GetComponentInChildren<Renderer>();
                if (renderer == null) continue;
                Bounds rock = renderer.bounds;
                float distance = Vector2.Distance(new Vector2(rock.center.x, rock.center.z),
                    new Vector2(mission.rover.position.x, mission.rover.position.z));
                if (distance < closest) { closest = distance; testRockBounds = rock; testRockName = part.name; }
            }
            if (testRockName == null) { Status = "No existing rock found for the obstacle test."; yield break; }
            testStart = mission.rover.position;
            Vector3 direction = testRockBounds.center - testStart; direction.y = 0; direction.Normalize();
            float extent = Mathf.Abs(direction.x) * testRockBounds.extents.x + Mathf.Abs(direction.z) * testRockBounds.extents.z;
            testGoal = testRockBounds.center + direction * (extent + 150);
            var goalQuery = tiles.SampleHeightMostDetailed(Cartographic(testGoal));
            while (!goalQuery.IsCompleted) { Status = "Loading terrain beyond the rock field…"; yield return null; }
            if (goalQuery.IsFaulted || !goalQuery.Result.sampleSuccess[0])
            { Status = "Terrain unavailable at the longer destination."; yield break; }
            testGoal = World(goalQuery.Result.longitudeLatitudeHeightPositions[0]);
            testDistance = Vector2.Distance(new Vector2(testGoal.x, testGoal.z), new Vector2(testStart.x, testStart.z));
            float span = Mathf.Max(Mathf.Abs(testGoal.x-testStart.x), Mathf.Abs(testGoal.z-testStart.z));
            float spacing = Mathf.Max(10, Mathf.Ceil(span / 110f / 2f) * 2);
            mission.ConfigureDemoRoute(testGoal, 250, spacing, 20, 50);
            ShowDestinationRing();
            report.AppendLine($"Obstacle test: {testRockName}, direct distance {testDistance:F1} m, grid spacing {spacing:F1} m, goal {testGoal:F3}, 250 Wh SIM, 50x playback");
        }
        CreateOverview(manager, group);
        // Persist diagnostics beside the project, not in Assets or in the team's source scene.
        try { File.WriteAllText(Path.Combine(Application.dataPath, "../../EnvironmentPlacementReport.txt"), report.ToString()); }
        catch (Exception error) { Debug.LogWarning("Placement report: " + error.Message); }
        Debug.Log($"Lunar site: aligned {aligned}/{placements.Count} imported objects. Detailed start: {sampledStart:F3}");
        // Give rendering/physics meshes time to converge around the corrected camera.
        float stableSince = -1;
        deadline = Time.realtimeSinceStartup + 120;
        while (Time.realtimeSinceStartup < deadline)
        {
            float progress = tiles.ComputeLoadProgress();
            bool nearGround = PhysicsGround(mission.rover.position, out float height) && Mathf.Abs(height - sampledStart.y) < 0.5f;
            if (nearGround && progress >= 99.9f)
            {
                if (stableSince < 0) stableSince = Time.realtimeSinceStartup;
                if (Time.realtimeSinceStartup - stableSince >= 2) break;
            }
            else stableSince = -1;
            Status = $"Refining terrain around the site… {progress:F0}%";
            yield return null;
        }
        if (!PhysicsGround(mission.rover.position, out float finalHeight) || Mathf.Abs(finalHeight - sampledStart.y) >= 0.5f)
        { Status = "Terrain collision meshes have not caught up with the detailed surface. Wait or restart Play."; yield break; }
        Ready = true;
        Status = longObstacleDemo ? $"Rock-field test • {testDistance/1000:F2} km direct • 50× playback • 250 Wh SIM" :
            $"Site ready • {aligned} original objects aligned • Start runs A*";
        if (longObstacleDemo && autoStartObstacleDemo) { yield return null; mission.StartMission(); }
    }

    void ShowDestinationRing()
    {
        // BaseCampMarker is a location indicator, not a wall to drive through.
        var oldRing = mission.destination.Find("Base Camp Destination Ring");
        if (oldRing != null) Destroy(oldRing.gameObject);
        if (markerMaterial != null) Destroy(markerMaterial);
        foreach (var renderer in mission.destination.GetComponentsInChildren<Renderer>()) renderer.enabled = false;
        foreach (var collider in mission.destination.GetComponentsInChildren<Collider>()) collider.enabled = false;
        var ringObject = new GameObject("Base Camp Destination Ring");
        ringObject.transform.SetParent(mission.destination, false);
        var ring = ringObject.AddComponent<LineRenderer>();
        ring.useWorldSpace = true; ring.loop = true; ring.positionCount = 48; ring.widthMultiplier = 0.15f;
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader != null)
        {
            markerMaterial = new Material(shader);
            markerMaterial.SetColor("_BaseColor", Color.cyan);
            ring.sharedMaterial = markerMaterial;
        }
        Vector3 centre = mission.destination.position + Vector3.up * 0.2f;
        for (int i = 0; i < ring.positionCount; i++)
        {
            float a = 2 * Mathf.PI * i / ring.positionCount;
            ring.SetPosition(i, centre + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (longObstacleDemo ? 12f : 2.8f));
        }
    }

    void AddPlacement(Transform part)
    {
        if (!part.gameObject.activeInHierarchy) return;
        var renderers = part.GetComponentsInChildren<Renderer>();
        bool found = false; Bounds box = default;
        foreach (var renderer in renderers)
        {
            if (!renderer.enabled || renderer is LineRenderer) continue;
            if (!found) { box = renderer.bounds; found = true; } else box.Encapsulate(renderer.bounds);
        }
        if (found) { placements.Add(part); bounds.Add(box); }
    }
    double3 Cartographic(Vector3 world)
    {
        Vector3 local = reference.transform.InverseTransformPoint(world);
        return reference.ellipsoid.CenteredFixedToLongitudeLatitudeHeight(
            reference.TransformUnityPositionToEarthCenteredEarthFixed(new double3(local.x, local.y, local.z)));
    }
    Vector3 World(double3 llh)
    {
        double3 p = reference.TransformEarthCenteredEarthFixedPositionToUnity(
            reference.ellipsoid.LongitudeLatitudeHeightToCenteredFixed(llh));
        return reference.transform.TransformPoint(new Vector3((float)p.x, (float)p.y, (float)p.z));
    }
    bool PhysicsGround(Vector3 at, out float height)
    {
        height = float.NegativeInfinity;
        foreach (var hit in Physics.RaycastAll(at + Vector3.up * 250, Vector3.down, 500, ~0, QueryTriggerInteraction.Ignore))
            if (hit.collider.GetComponentInParent<Cesium3DTileset>() == tiles)
                height = Mathf.Max(height, hit.point.y);
        return !float.IsNegativeInfinity(height);
    }
    void CreateOverview(CesiumCameraManager manager, Transform group)
    {
        Bounds site = new Bounds(mission.rover.position, Vector3.one * 30);
        if (group != null)
            foreach (var renderer in group.GetComponentsInChildren<Renderer>())
                if (renderer.enabled && !(renderer is LineRenderer)) site.Encapsulate(renderer.bounds);
        if (longObstacleDemo && testRockName != null)
        {
            site = new Bounds(testStart, Vector3.one * 40);
            site.Encapsulate(testGoal); site.Encapsulate(testRockBounds); site.Expand(250);
        }
        siteCentre = site.center;
        radius = Mathf.Max(100, site.extents.magnitude * 1.55f);
        var go = new GameObject("Lunar Site Overview Camera");
        siteCamera = go.AddComponent<Camera>();
        siteCamera.CopyFrom(roverCamera);
        siteCamera.nearClipPlane = 1;
        siteCamera.farClipPlane = Mathf.Max(30000, radius * 4);
        siteCamera.transform.position = siteCentre + new Vector3(0, 0.8f, -0.6f) * radius;
        siteCamera.transform.LookAt(siteCentre);
        siteCamera.enabled = false;
        manager.additionalCameras.Add(siteCamera);
        mission.viewCamera = roverCamera;
        SwitchView(overview);
    }
    void OnGUI()
    {
        float scale = Mathf.Clamp(Screen.height / 850f, 0.7f, 1.5f);
        var previous = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
        GUI.Box(new Rect(12, 12, 470, 84), "");
        if (GUI.Button(new Rect(22, 22, 145, 30), "ROVER VIEW")) SwitchView(false);
        if (GUI.Button(new Rect(177, 22, 160, 30), "SITE OVERVIEW")) SwitchView(true);
        GUI.Label(new Rect(22, 59, 450, 30), Status);
        if (overview && Ready && longObstacleDemo)
        {
            MapLabel(testStart, "START", scale);
            MapLabel(mission.destination.position, "DESTINATION", scale);
            MapLabel(testRockBounds.center + Vector3.up * testRockBounds.extents.y, "ROCK FIELD", scale);
            MapLabel(mission.rover.position + Vector3.up * 5, "ROVER", scale);
        }
        GUI.matrix = previous;
    }
    void MapLabel(Vector3 world, string text, float scale)
    {
        Vector3 p = siteCamera.WorldToScreenPoint(world);
        if (p.z <= 0 || p.x < 0 || p.x > Screen.width * 0.65f || p.y < 0 || p.y > Screen.height) return;
        GUI.Box(new Rect(p.x / scale - 45, (Screen.height-p.y) / scale - 22, 100, 24), text);
    }
    void SwitchView(bool showOverview)
    {
        if (siteCamera == null || roverCamera == null) return;
        overview = showOverview;
        roverCamera.enabled = !overview; siteCamera.enabled = overview;
        mission.viewCamera = ActiveCamera;
        mission.SetRouteWidth(overview && longObstacleDemo ? 6 : 0.3f);
    }
    void OnDestroy()
    {
        if (siteCamera != null) Destroy(siteCamera.gameObject);
        if (markerMaterial != null) Destroy(markerMaterial);
    }
}
