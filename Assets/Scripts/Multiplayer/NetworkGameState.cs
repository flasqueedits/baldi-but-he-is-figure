using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Shared notebook state for co-op. A notebook collected by one student disappears
/// for everyone, and only the collector gets the notebook count / math question.
/// </summary>
public class NetworkGameState : NetworkBehaviour
{
    public static NetworkGameState Instance { get; private set; }

    private static readonly Dictionary<int, NetworkNotebook> Notebooks = new Dictionary<int, NetworkNotebook>();
    private readonly HashSet<int> collected = new HashSet<int>();

    [SyncVar(hook = nameof(OnCollectedDataChanged))] private string collectedData = "";
    [SyncVar(hook = nameof(OnTotalChanged))] private int totalNotebooks;

    private void Awake()
    {
        Instance = this;
    }

    public static void Register(NetworkNotebook notebook)
    {
        if (notebook != null) Notebooks[notebook.id] = notebook;
    }

    public static bool IsActive => Instance != null && NetworkClient.active;

    public bool IsTaken(int id)
    {
        return collected.Contains(id);
    }

    public void RequestCollect(NetworkNotebook notebook)
    {
        if (notebook == null) return;
        if (collected.Contains(notebook.id)) return;

        uint collectorNetId = NetworkClient.localPlayer != null ? NetworkClient.localPlayer.netId : 0u;

        if (isServer)
        {
            ServerCollect(notebook.id, collectorNetId);
        }
        else
        {
            CmdCollect(notebook.id, collectorNetId);
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdCollect(int id, uint collectorNetId)
    {
        ServerCollect(id, collectorNetId);
    }

    [Server]
    private void ServerCollect(int id, uint collectorNetId)
    {
        if (!collected.Add(id)) return;
        collectedData = string.Join(",", collected);
        totalNotebooks = collected.Count;
        RpcNotebookTaken(id, collectorNetId);
        Debug.Log("[Figure Co-op] Notebook #" + id + " collected (player " + collectorNetId + ")");
    }

    [ClientRpc]
    private void RpcNotebookTaken(int id, uint collectorNetId)
    {
        SyncFromData();

        NetworkNotebook notebook;
        if (Notebooks.TryGetValue(id, out notebook) && notebook != null)
        {
            notebook.SetTaken();
            if (NetworkClient.localPlayer != null && collectorNetId == NetworkClient.localPlayer.netId)
                notebook.CollectedByLocal();
        }
        ApplySharedScore();
    }

    private void OnCollectedDataChanged(string oldValue, string newValue)
    {
        SyncFromData();
        ApplySharedScore();
    }

    private void OnTotalChanged(int oldValue, int newValue)
    {
        ApplySharedScore();
    }

    private void ApplySharedScore()
    {
        GameControllerScript gc = FindObjectOfType<GameControllerScript>();
        if (gc != null && gc.notebooks != totalNotebooks)
        {
            gc.notebooks = totalNotebooks;
            gc.RefreshNotebookCount();
        }
    }

    private void SyncFromData()
    {
        collected.Clear();
        if (string.IsNullOrEmpty(collectedData)) return;
        string[] parts = collectedData.Split(',');
        foreach (string part in parts)
        {
            int id;
            if (int.TryParse(part, out id))
            {
                collected.Add(id);
                NetworkNotebook notebook;
                if (Notebooks.TryGetValue(id, out notebook) && notebook != null) notebook.SetTaken();
            }
        }
    }
}
