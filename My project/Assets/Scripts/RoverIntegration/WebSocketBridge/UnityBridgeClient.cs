using UnityEngine;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

public class UnityBridgeClient : MonoBehaviour
{
    [Header("Bridge Connection")]
    public string serverUrl = "ws://127.0.0.1:8765";
    public bool connectOnStart = true;
    public bool autoReconnect = true;
    public float reconnectInterval = 3f;
    private float nextReconnectTime;
    private bool started;
    private bool shuttingDown;

    [Header("Connection Status")]
    public bool isConnected;
    public string connectionStatus = "Disconnected";

    [Header("Received Telemetry")]
    public string telemetrySource;
    public bool isLive;
    public bool physicalExecutionEnabled;
    public float batteryVoltage;

    public float telemetryTimeout = 3f;
    public Vector3 bridgePosition;
    public float bridgeSpeed;
    public float bridgeHeading;
    public float bridgeAngularSpeed;
    public bool hasOdometry;
    public bool hasBattery;
    private float lastTelemetryTime = -1f;
    public bool TelemetryActive => isConnected && lastTelemetryTime >= 0f &&
        Time.realtimeSinceStartup - lastTelemetryTime <= telemetryTimeout;

    [Header("Bridge LiDAR")]
    public bool hasLidar;
    public int lidarSampleCount;
    public int lidarValidCount;
    public float lidarClosestDistance;
    public float lidarClosestAngle;

    [Header("Bridge Mission")]
    public string missionId;
    public string missionStatus;
    public string missionSource;
    public int connectionGeneration { get; private set; }

    [Header("Debug Settings")]
    public bool logTelemetry = true;

    private ClientWebSocket socket;
    private CancellationTokenSource cancellation;
    private bool receivedFirstTelemetry;

    public string LatestTelemetryJson { get; private set; }

    public event Action<string> OnTelemetryReceived;
    public event Action<string> OnMissionAcknowledgment;

    [Serializable]
    public class BridgeMessage
    {
        public string type;
        public string source;
        public bool is_live;
        public bool physical_execution_enabled;
        public BatteryData battery;
        public OdometryData odometry;
        public LidarData lidar;
        public MissionTelemetry mission;
    }

    [Serializable]
    public class BatteryData
    {
        public float voltage_v;
    }

    [Serializable]
    public class OdometryData
    {
        public Vector3 position_m;
        public Vector3 linear_velocity_m_s;
        public Vector3 angular_velocity_rad_s;
        public Quaternion orientation;
    }

    [Serializable]
    public class LidarData
    {
        public float angle_min_rad;
        public float angle_increment_rad;
        public float range_min_m;
        public float range_max_m;
        public float[] ranges_m;
    }

    [Serializable]
    public class MissionTelemetry
    {
        public string mission_id;
        public string status;
        public string source;
    }

    private void ParseLidar(LidarData scan)
    {
        hasLidar = false;
        lidarSampleCount = scan?.ranges_m?.Length ?? 0;
        lidarValidCount = 0;
        lidarClosestDistance = float.PositiveInfinity;
        lidarClosestAngle = 0f;
        if (lidarSampleCount == 0 || scan.range_min_m <= 0f ||
            scan.range_max_m <= scan.range_min_m) return;
        for (int i = 0; i < lidarSampleCount; i++)
        {
            float r = scan.ranges_m[i];
            if (float.IsNaN(r) || float.IsInfinity(r) ||
                r < scan.range_min_m || r > scan.range_max_m) continue;
            lidarValidCount++;
            if (r < lidarClosestDistance)
            {
                lidarClosestDistance = r;
                lidarClosestAngle = (scan.angle_min_rad + i * scan.angle_increment_rad) * Mathf.Rad2Deg;
            }
        }
        hasLidar = lidarValidCount > 0;
    }

    private void InvalidateTelemetry()
    {
        lastTelemetryTime = -1f;
        hasOdometry = false;
        hasBattery = false;
        ParseLidar(null);
        LatestTelemetryJson = null;
        missionId = null;
        missionStatus = null;
        missionSource = null;
        isLive = false;
        physicalExecutionEnabled = false;
    }

    void Update()
    {
        if (lastTelemetryTime >= 0f && !TelemetryActive)
            InvalidateTelemetry();

        if (started && connectOnStart && autoReconnect && !shuttingDown &&
            socket == null && Time.realtimeSinceStartup >= nextReconnectTime)
        {
            _ = ConnectToBridge();
        }
    }

    async void Start()
    {
        started = true;
        if (connectOnStart)
        {
            await ConnectToBridge();
        }
    }

    public async Task ConnectToBridge()
    {
        if (socket != null || shuttingDown)
            return;

        ClientWebSocket client = new ClientWebSocket();
        CancellationTokenSource tokenSource =
            new CancellationTokenSource();

        socket = client;
        cancellation = tokenSource;
        receivedFirstTelemetry = false;
        lastTelemetryTime = -1f;
        hasOdometry = false;
        hasBattery = false;
        ParseLidar(null);

        try
        {
            connectionStatus = "Connecting";

            Debug.Log("[Unity Bridge] Connecting to " + serverUrl);

            await client.ConnectAsync(
                new Uri(serverUrl),
                tokenSource.Token
            );

            connectionGeneration++;
            isConnected = true;
            connectionStatus = "Connected";

            Debug.Log("[Unity Bridge] Connected successfully!");

            await ReceiveMessages(client, tokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
        catch (Exception e)
        {
            if (!tokenSource.IsCancellationRequested)
            {
                connectionStatus = "Connection Error";
                Debug.LogWarning("[Unity Bridge] " + e.Message);
            }
        }
        finally
        {
            isConnected = false;
            InvalidateTelemetry();

            if (connectionStatus != "Connection Error")
                connectionStatus = "Disconnected";

            client.Dispose();
            tokenSource.Dispose();

            socket = null;
            cancellation = null;
            if (!shuttingDown)
                nextReconnectTime = Time.realtimeSinceStartup + Mathf.Max(0.5f, reconnectInterval);
        }
    }

    private async Task ReceiveMessages(
        ClientWebSocket client,
        CancellationToken token)
    {
        byte[] buffer = new byte[4096];

        while (client.State == WebSocketState.Open &&
               !token.IsCancellationRequested)
        {
            using (MemoryStream messageStream = new MemoryStream())
            {
                WebSocketReceiveResult result;

                do
                {
                    result = await client.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        token
                    );

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        return;
                    }

                    messageStream.Write(buffer, 0, result.Count);

                } while (!result.EndOfMessage);

                if (result.MessageType != WebSocketMessageType.Text)
                    continue;

                string json = Encoding.UTF8.GetString(
                    messageStream.ToArray()
                );

                ProcessMessage(json);
            }
        }
    }

    private void ProcessMessage(string json)
    {
        BridgeMessage message;

        try
        {
            message = JsonUtility.FromJson<BridgeMessage>(json);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Unity Bridge] Invalid JSON: " + e.Message);
            return;
        }

        if (message == null)
            return;

        switch (message.type)
        {
            case "telemetry":

                LatestTelemetryJson = json;

                telemetrySource = message.source;
                isLive = message.is_live;

                physicalExecutionEnabled =
                    message.physical_execution_enabled;

                lastTelemetryTime = Time.realtimeSinceStartup;
                ParseLidar(message.lidar);
                missionId = message.mission?.mission_id;
                missionStatus = message.mission?.status;
                missionSource = message.mission?.source;
                hasBattery = message.battery != null;
                hasOdometry = message.odometry != null;
                if (hasOdometry)
                {
                    var odom = message.odometry;
                    bridgePosition = odom.position_m;
                    bridgeSpeed = odom.linear_velocity_m_s.magnitude;
                    bridgeAngularSpeed = odom.angular_velocity_rad_s.magnitude;
                    Quaternion q = odom.orientation;
                    float sinYaw = 2f * (q.w * q.z + q.x * q.y);
                    float cosYaw = 1f - 2f * (q.y * q.y + q.z * q.z);
                    bridgeHeading = Mathf.Repeat(Mathf.Atan2(sinYaw, cosYaw) * Mathf.Rad2Deg, 360f);
                }

                if (message.battery != null)
                {
                    batteryVoltage = message.battery.voltage_v;
                }

                if (!receivedFirstTelemetry)
                {
                    receivedFirstTelemetry = true;

                    if (logTelemetry)
                    {
                        Debug.Log(
                            "[Unity Bridge] Telemetry received!" +
                            "\nSource: " + telemetrySource +
                            "\nBattery: " + batteryVoltage + " V" +
                            "\nLive Data: " + isLive +
                            "\nPhysical Execution: " +
                            physicalExecutionEnabled
                        );
                    }
                }

                OnTelemetryReceived?.Invoke(json);

                break;

            case "mission_ack":

                Debug.Log("[Unity Bridge] Mission acknowledgment: " + json);

                OnMissionAcknowledgment?.Invoke(json);

                break;

            default:

                Debug.LogWarning(
                    "[Unity Bridge] Unknown message type: " +
                    message.type
                );

                break;
        }
    }

    private bool isSending;

    public async Task SendMessageToBridge(string json)
    {
        if (socket == null ||
            socket.State != WebSocketState.Open)
        {
            Debug.LogWarning("[Unity Bridge] Not connected.");
            return;
        }

        if (isSending)
        {
            Debug.LogWarning("[Unity Bridge] Send already in progress.");
            return;
        }

        isSending = true;

        try
        {
            byte[] data = Encoding.UTF8.GetBytes(json);

            await socket.SendAsync(
                new ArraySegment<byte>(data),
                WebSocketMessageType.Text,
                true,
                cancellation.Token
            );

            Debug.Log("[Unity Bridge] Message sent.");
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Unity Bridge] Send failed: " + e.Message);
        }
        finally
        {
            isSending = false;
        }
    }

    private void OnDestroy()
    {
        shuttingDown = true;
        cancellation?.Cancel();
        socket?.Abort();
        isConnected = false;
    }
}
