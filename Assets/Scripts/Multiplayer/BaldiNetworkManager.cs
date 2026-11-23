using kcp2k;
using Mirror;
using Mirror.SimpleWeb;
using UnityEngine;

/// <summary>
/// Mirror NetworkManager for the Figure co-op prototype.
/// Host and clients all run the same "School" scene, so no network scene management is used.
/// </summary>
public class BaldiNetworkManager : NetworkManager
{
    public const ushort DefaultPort = 7777;

    public static BaldiNetworkManager Instance { get; private set; }

    private int playerCounter;

    public override void Awake()
    {
        base.Awake();
        Instance = this;

        if (transport == null)
        {
            KcpTransport kcp = GetComponent<KcpTransport>();
            if (kcp == null) kcp = gameObject.AddComponent<KcpTransport>();
            kcp.port = DefaultPort;
            transport = kcp;
        }
    }

    public void StartHostGame()
    {
        networkAddress = "127.0.0.1";
        playerCounter = 0;
        StartHost();
    }

    public void StartClientGame(string address)
    {
        networkAddress = string.IsNullOrWhiteSpace(address) ? "127.0.0.1" : address.Trim();
        StartClient();
    }

    /// <summary>Connect to a hosted server (Render) through WebSocket / TLS.</summary>
    public void StartOnlineClient(string url)
    {
        SimpleWebTransport web = GetComponent<SimpleWebTransport>();
        if (web == null) web = gameObject.AddComponent<SimpleWebTransport>();
        web.port = 443;
        transport = web;
        networkAddress = url;
        StartClient(new System.Uri(url));
    }

    /// <summary>Starts a headless dedicated server that listens with WebSocket.</summary>
    public void StartDedicatedServer()
    {
        SimpleWebTransport web = GetComponent<SimpleWebTransport>();
        if (web == null) web = gameObject.AddComponent<SimpleWebTransport>();

        string portValue = System.Environment.GetEnvironmentVariable("PORT");
        ushort port;
        if (!ushort.TryParse(portValue, out port)) port = 10000;

        web.port = port;
        web.sslEnabled = false; // Render proxy terminates TLS (wss on the outside)
        transport = web;
        StartServer();
        Debug.Log("[Server] Dedicated server listening on port " + port);
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);

        NetworkPlayer player = conn.identity != null ? conn.identity.GetComponent<NetworkPlayer>() : null;
        if (player == null) return;

        playerCounter++;
        player.playerNumber = playerCounter;
        player.playerName = "Student " + playerCounter;

        // Spawn everyone slightly apart so they don't start inside each other.
        player.transform.position += new Vector3((playerCounter - 1) * 1.5f, 0f, 0f);

        Debug.Log("[Figure Co-op] " + player.playerName + " joined (id=" + player.netId + ")");
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("[Figure Co-op] Connected to host.");
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        Debug.LogWarning("[Figure Co-op] Disconnected from host.");
    }
}
