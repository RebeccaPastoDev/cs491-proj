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

    [Header("Connection Status")]
    public bool isConnected;
    public string connectionStatus = "Disconnected";

    [Header("Received Telemetry")]
    public string telemetrySource;
    public bool isLive;
    public bool physicalExecutionEnabled;
    public float batteryVoltage;

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
    }

    [Serializable]
    public class BatteryData
    {
        public float voltage_v;
    }

    async void Start()
    {
        if (connectOnStart)
        {
            await ConnectToBridge();
        }
    }

    public async Task ConnectToBridge()
    {
        if (socket != null)
            return;

        ClientWebSocket client = new ClientWebSocket();
        CancellationTokenSource tokenSource =
            new CancellationTokenSource();

        socket = client;
        cancellation = tokenSource;
        receivedFirstTelemetry = false;

        try
        {
            connectionStatus = "Connecting";

            Debug.Log("[Unity Bridge] Connecting to " + serverUrl);

            await client.ConnectAsync(
                new Uri(serverUrl),
                tokenSource.Token
            );

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
                Debug.LogError("[Unity Bridge] " + e.Message);
            }
        }
        finally
        {
            isConnected = false;

            if (connectionStatus != "Connection Error")
                connectionStatus = "Disconnected";

            client.Dispose();
            tokenSource.Dispose();

            socket = null;
            cancellation = null;
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
        cancellation?.Cancel();
        socket?.Abort();
        isConnected = false;
    }
}
