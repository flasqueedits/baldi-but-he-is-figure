#if UNITY_EDITOR
using System;
using Mirror;
using kcp2k;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One-click setup for the co-op prototype. Opens the School scene, creates the
/// networked player prefab, wires the NetworkManager / Baldi / notebooks and saves.
/// Safe to run more than once.
/// </summary>
public static class MultiplayerSceneSetup
{
    private const string SchoolScenePath = "Assets/Scene/School.unity";
    private const string PrefabPath = "Assets/Multiplayer/NetworkPlayer.prefab";

    [MenuItem("Tools/Figure Multiplayer/Co-op Prototipini Kur")]
    public static void Setup()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[Figure Co-op] Setup must be run while the editor is not in Play mode.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(SchoolScenePath, OpenSceneMode.Single);
        Debug.Log("[Figure Co-op] Opened scene: " + scene.name + " (roots: " + scene.rootCount + ")");

        PlayerScript template = FindInScene<PlayerScript>(scene);
        BaldiScript baldi = FindInScene<BaldiScript>(scene);
        Debug.Log("[Figure Co-op] PlayerScript=" + (template != null) + " BaldiScript=" + (baldi != null));
        if (template == null || baldi == null)
        {
            Debug.LogError("[Figure Co-op] School scene must contain a PlayerScript and a BaldiScript.");
            return;
        }

        GameObject playerPrefab = CreatePlayerPrefab(template.gameObject);
        if (playerPrefab == null) return;

        GameObject managerGo = GameObject.Find("NetworkManager");
        if (managerGo == null) managerGo = new GameObject("NetworkManager");

        BaldiNetworkManager manager = GetOrAdd<BaldiNetworkManager>(managerGo);
        KcpTransport transport = GetOrAdd<KcpTransport>(managerGo);
        transport.port = BaldiNetworkManager.DefaultPort;
        manager.transport = transport;
        manager.playerPrefab = playerPrefab;
        manager.networkAddress = "127.0.0.1";

        GetOrAdd<NetworkGameState>(managerGo);
        GetOrAdd<MultiplayerBootstrap>(managerGo);
        EnsureSceneId(managerGo, 0x4E4D4101u);

        EnsureSceneId(baldi.gameObject, 0x42414C44u);
        NetworkBaldi networkBaldi = GetOrAdd<NetworkBaldi>(baldi.gameObject);
        baldi.netBaldi = networkBaldi;
        EditorUtility.SetDirty(baldi);

        int notebookCount = 0;
        foreach (NotebookScript notebook in FindAllInScene<NotebookScript>(scene))
        {
            NetworkNotebook netNotebook = GetOrAdd<NetworkNotebook>(notebook.gameObject);
            netNotebook.id = ++notebookCount;
            EditorUtility.SetDirty(netNotebook);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Figure Co-op] Setup complete. Player prefab, NetworkManager (port " +
                  BaldiNetworkManager.DefaultPort + "), Baldi sync and " + notebookCount +
                  " networked notebooks are ready.");
    }

    private static GameObject CreatePlayerPrefab(GameObject source)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Multiplayer"))
            AssetDatabase.CreateFolder("Assets", "Multiplayer");

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, PrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[Figure Co-op] Failed to create player prefab at " + PrefabPath);
            return null;
        }

        GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);
        // NetworkIdentity's OnValidate assigns the prefab assetId automatically.
        GetOrAdd<NetworkIdentity>(contents);
        GetOrAdd<NetworkPlayer>(contents);

        PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
        PrefabUtility.UnloadPrefabContents(contents);

        return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
    }

    private static void EnsureSceneId(GameObject go, uint fallback)
    {
        NetworkIdentity identity = GetOrAdd<NetworkIdentity>(go);
        if (identity.sceneId == 0) identity.sceneId = fallback;
        EditorUtility.SetDirty(identity);
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (T item in FindAllInScene<T>(scene)) return item;
        return null;
    }

    private static T[] FindAllInScene<T>(Scene scene) where T : Component
    {
        System.Collections.Generic.List<T> result = new System.Collections.Generic.List<T>();
        foreach (T item in Resources.FindObjectsOfTypeAll<T>())
        {
            if (item != null && item.gameObject.scene == scene) result.Add(item);
        }
        return result.ToArray();
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        if (component == null) component = go.AddComponent<T>();
        return component;
    }
}
#endif
