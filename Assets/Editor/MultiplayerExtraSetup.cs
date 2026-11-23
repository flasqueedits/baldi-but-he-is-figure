#if UNITY_EDITOR
using System.Collections.Generic;
using Mirror;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Wires the extended co-op systems: doors, pickups relay, chat/ping, NPC sync
/// and floating name tags. Safe to run more than once.
/// </summary>
public static class MultiplayerExtraSetup
{
    private const string SchoolPath = "Assets/Scene/School.unity";
    private const string PrefabPath = "Assets/Multiplayer/NetworkPlayer.prefab";

    [MenuItem("Tools/Figure Multiplayer/Ek Sistemleri Kur")]
    public static void Setup()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[Extra] Cannot run setup in Play mode.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(SchoolPath, OpenSceneMode.Single);

        GameObject managerGo = GameObject.Find("NetworkManager");
        if (managerGo == null)
        {
            Debug.LogError("[Extra] NetworkManager not found. Run the basic co-op setup first.");
            return;
        }

        NetworkWorld world = GetOrAdd<NetworkWorld>(managerGo);
        GameControllerScript gc = FindInScene<GameControllerScript>(scene);
        if (gc != null) world.sprayEffect = gc.bsodaSpray;
        EditorUtility.SetDirty(world);

        int nextId = 1000;

        foreach (DoorScript door in FindAllInScene<DoorScript>(scene))
        {
            NetworkDoor net = GetOrAdd<NetworkDoor>(door.gameObject);
            net.id = nextId++;
            EditorUtility.SetDirty(net);
        }
        foreach (SwingingDoorScript door in FindAllInScene<SwingingDoorScript>(scene))
        {
            NetworkSwingingDoor net = GetOrAdd<NetworkSwingingDoor>(door.gameObject);
            net.id = nextId++;
            EditorUtility.SetDirty(net);
        }

        int npcIndex = 0;
        AddNpc<PlaytimeScript>(scene, ref nextId, ref npcIndex);
        AddNpc<SweepScript>(scene, ref nextId, ref npcIndex);
        AddNpc<PrincipalScript>(scene, ref nextId, ref npcIndex);
        AddNpc<FirstPrizeScript>(scene, ref nextId, ref npcIndex);
        AddNpc<BullyScript>(scene, ref nextId, ref npcIndex);

        GameObject prefabContents = PrefabUtility.LoadPrefabContents(PrefabPath);
        GetOrAdd<NetworkNameTag>(prefabContents);
        PrefabUtility.SaveAsPrefabAsset(prefabContents, PrefabPath);
        PrefabUtility.UnloadPrefabContents(prefabContents);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Extra] Door sync, pickups, chat/ping, NPC sync and name tags configured.");
    }

    private static void AddNpc<T>(Scene scene, ref int nextId, ref int npcIndex) where T : MonoBehaviour
    {
        foreach (T ai in FindAllInScene<T>(scene))
        {
            GameObject go = ai.gameObject;
            NetworkIdentity identity = GetOrAdd<NetworkIdentity>(go);
            if (identity.sceneId == 0) identity.sceneId = (uint)(0xCE000000 + npcIndex++ + 1);

            NetworkNpc net = GetOrAdd<NetworkNpc>(go);
            nextId++;
            var scripts = new List<MonoBehaviour> { ai };
            foreach (MonoBehaviour other in go.GetComponents<MonoBehaviour>())
                if (other != null && other != net && other != identity && !scripts.Contains(other))
                    scripts.Add(other);
            net.aiScripts = scripts.ToArray();
            EditorUtility.SetDirty(identity);
            EditorUtility.SetDirty(net);
        }
    }

    private static T FindInScene<T>(Scene scene) where T : MonoBehaviour
    {
        foreach (T item in FindAllInScene<T>(scene)) return item;
        return null;
    }

    private static T[] FindAllInScene<T>(Scene scene) where T : MonoBehaviour
    {
        var result = new List<T>();
        foreach (T item in Resources.FindObjectsOfTypeAll<T>())
            if (item != null && item.gameObject.scene == scene) result.Add(item);
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
