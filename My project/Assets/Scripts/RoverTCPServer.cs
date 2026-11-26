using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

public class RoverTCPServer : MonoBehaviour
{
    public Transform rover;
    public int port = 5000;

    private TcpListener server;
    private TcpClient client;
    private NetworkStream stream;
    private bool running = false;

    async void Start()
    {
        try
        {
            server = new TcpListener(IPAddress.Loopback, port);
            server.Start();
            running = true;

            Debug.Log("Unity TCP server started on port " + port);

            while (running)
            {
                TcpClient newClient = await server.AcceptTcpClientAsync();

                client?.Close();
                client = newClient;
                stream = client.GetStream();

                Debug.Log("Qt connected to Unity!");
            }
        }
        catch (Exception e)
        {
            if (running)
                Debug.LogError("TCP server error: " + e.Message);
        }
    }

    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer < 0.1f)
            return;

        timer = 0f;

        if (rover == null || client == null || !client.Connected)
            return;

        Vector3 pos = rover.position;

        string message = string.Format(
            CultureInfo.InvariantCulture,
            "{0:F2},{1:F2},{2:F2}\n",
            pos.x, pos.y, pos.z
        );

        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            stream.Write(data, 0, data.Length);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Qt connection lost: " + e.Message);
            client?.Close();
            client = null;
            stream = null;
        }
    }

    void OnDestroy()
    {
        running = false;
        client?.Close();
        server?.Stop();
    }
}
