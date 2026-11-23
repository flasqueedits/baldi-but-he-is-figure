using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Generic server relay for scene object actions, pickups, chat and pings.
/// Scene objects register with a stable integer id from the setup tool.
/// </summary>
public class NetworkWorld : NetworkBehaviour
{
    public static NetworkWorld Instance { get; private set; }

    private static readonly Dictionary<int, NetworkSceneObject> Objects = new Dictionary<int, NetworkSceneObject>();
    private readonly HashSet<string> pickedItems = new HashSet<string>();

    [SyncVar(hook = nameof(OnPickedChanged))] private string pickedData = "";

    public GameObject sprayEffect;

    [SyncVar] private int sharedExits;

    private void Awake()
    {
        Instance = this;
    }

    private static bool NetworkActive => NetworkServer.active || NetworkClient.active;

    public static void Register(NetworkSceneObject sceneObject)
    {
        if (sceneObject != null) Objects[sceneObject.id] = sceneObject;
    }

    // ---- Generic scene object actions -------------------------------------
    public void RequestAction(int id, int action, float value)
    {
        if (!NetworkActive) return;
        if (isServer) ServerAction(id, action, value);
        else CmdAction(id, action, value);
    }

    [Command(requiresAuthority = false)]
    private void CmdAction(int id, int action, float value)
    {
        ServerAction(id, action, value);
    }

    [Server]
    private void ServerAction(int id, int action, float value)
    {
        RpcAction(id, action, value);
    }

    [ClientRpc]
    private void RpcAction(int id, int action, float value)
    {
        NetworkSceneObject target;
        if (Objects.TryGetValue(id, out target) && target != null) target.ApplyAction(action, value);
    }

    // ---- Pickups -----------------------------------------------------------
    public void RequestPickup(string objectName, int itemId)
    {
        if (!NetworkActive) return;
        uint collector = NetworkClient.localPlayer != null ? NetworkClient.localPlayer.netId : 0u;
        if (isServer) ServerPickup(objectName, itemId, collector);
        else CmdPickup(objectName, itemId, collector);
    }

    [Command(requiresAuthority = false)]
    private void CmdPickup(string objectName, int itemId, uint collector)
    {
        ServerPickup(objectName, itemId, collector);
    }

    [Server]
    private void ServerPickup(string objectName, int itemId, uint collector)
    {
        if (!pickedItems.Add(objectName)) return;
        pickedData = string.Join("|", pickedItems);
        RpcPicked(objectName, itemId, collector);
    }

    [ClientRpc]
    private void RpcPicked(string objectName, int itemId, uint collector)
    {
        SyncPicked();
        GameObject pickup = FindByName(objectName);
        if (pickup != null) pickup.SetActive(false);

        if (NetworkClient.localPlayer != null && collector == NetworkClient.localPlayer.netId)
        {
            GameControllerScript gc = FindObjectOfType<GameControllerScript>();
            if (gc != null) gc.CollectItem(itemId);
        }
    }

    private void OnPickedChanged(string oldValue, string newValue)
    {
        SyncPicked();
    }

    private void SyncPicked()
    {
        if (string.IsNullOrEmpty(pickedData)) return;
        string[] parts = pickedData.Split('|');
        foreach (string part in parts)
        {
            pickedItems.Add(part);
            GameObject pickup = FindByName(part);
            if (pickup != null) pickup.SetActive(false);
        }
    }

    private static GameObject FindByName(string name)
    {
        GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject go in all)
            if (go.name == name && go.scene.IsValid()) return go;
        return null;
    }

    // ---- Effects -----------------------------------------------------------
    public void RequestEffect(int type, Vector3 position, Quaternion rotation)
    {
        if (!NetworkActive) return;
        if (isServer) RpcEffect(type, position, rotation);
        else CmdEffect(type, position, rotation);
    }

    [Command(requiresAuthority = false)]
    private void CmdEffect(int type, Vector3 position, Quaternion rotation)
    {
        RpcEffect(type, position, rotation);
    }

    [ClientRpc]
    private void RpcEffect(int type, Vector3 position, Quaternion rotation)
    {
        if (type == 0 && sprayEffect != null) Instantiate(sprayEffect, position, rotation);
    }

    // ---- Exits / Win -------------------------------------------------------
    public void RequestExit(string entranceName)
    {
        if (!NetworkActive) return;
        if (isServer) ServerExit(entranceName);
        else CmdExit(entranceName);
    }

    [Command(requiresAuthority = false)]
    private void CmdExit(string entranceName)
    {
        ServerExit(entranceName);
    }

    [Server]
    private void ServerExit(string entranceName)
    {
        sharedExits++;
        RpcExit(sharedExits, entranceName);
    }

    [ClientRpc]
    private void RpcExit(int count, string entranceName)
    {
        GameControllerScript gc = FindObjectOfType<GameControllerScript>();
        if (gc != null) gc.ApplyExitReached(count);
        if (!string.IsNullOrEmpty(entranceName))
        {
            EntranceScript entrance = null;
            foreach (EntranceScript candidate in FindObjectsOfType<EntranceScript>())
                if (candidate.name == entranceName) { entrance = candidate; break; }
            if (entrance != null) entrance.Lower();
        }
    }

    public void RequestWin()
    {
        if (!NetworkActive) return;
        if (isServer) RpcWin();
        else CmdWin();
    }

    [Command(requiresAuthority = false)]
    private void CmdWin()
    {
        RpcWin();
    }

    [ClientRpc]
    private void RpcWin()
    {
        NetworkHud.ShowWin();
    }

    // ---- Chat --------------------------------------------------------------
    public void RequestChat(string message)
    {
        if (!NetworkActive) return;
        if (string.IsNullOrWhiteSpace(message)) return;
        string author = "Player";
        if (NetworkClient.localPlayer != null)
        {
            NetworkPlayer np = NetworkClient.localPlayer.GetComponent<NetworkPlayer>();
            if (np != null) author = np.playerName;
        }
        if (isServer) RpcChat(author, message);
        else CmdChat(author, message);
    }

    [Command(requiresAuthority = false)]
    private void CmdChat(string author, string message)
    {
        RpcChat(author, message);
    }

    [ClientRpc]
    private void RpcChat(string author, string message)
    {
        NetworkHud.AddChat(author, message);
    }

    // ---- Ping --------------------------------------------------------------
    public void RequestPing(Vector3 position)
    {
        if (!NetworkActive) return;
        if (isServer) RpcPing(position);
        else CmdPing(position);
    }

    [Command(requiresAuthority = false)]
    private void CmdPing(Vector3 position)
    {
        RpcPing(position);
    }

    [ClientRpc]
    private void RpcPing(Vector3 position)
    {
        NetworkHud.ShowPing(position);
    }
}
