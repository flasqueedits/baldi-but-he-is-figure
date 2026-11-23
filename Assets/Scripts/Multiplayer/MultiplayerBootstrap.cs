using System.Reflection;
using UnityEngine;

/// <summary>
/// Lives on the NetworkManager object in the School scene.
/// Starts host/client and reconnects all scene references to the locally spawned player.
/// </summary>
public class MultiplayerBootstrap : MonoBehaviour
{
    public static MultiplayerBootstrap Instance { get; private set; }

    // The original single-player object in the scene. It keeps all serialized
    // references; at runtime we copy them to the networked player and then mute it.
    public static PlayerScript TemplatePlayer { get; private set; }

    private bool attached;

    private void Awake()
    {
        Instance = this;
        TemplatePlayer = FindObjectOfType<PlayerScript>();
    }

    private void Start()
    {
        BaldiNetworkManager manager = BaldiNetworkManager.Instance;
        if (manager == null) return;

        // Headless dedicated server (Render Docker) is started with -server.
        foreach (string arg in System.Environment.GetCommandLineArgs())
        {
            if (arg == "-server" || arg == "-dedicated")
            {
                Application.targetFrameRate = 60;
                manager.StartDedicatedServer();
                return;
            }
        }

        if (MultiplayerSession.Mode == MultiplayerSession.LaunchMode.None) return;

        if (MultiplayerSession.Mode == MultiplayerSession.LaunchMode.Host)
            manager.StartHostGame();
        else if (MultiplayerSession.UseWebSocket)
            manager.StartOnlineClient(MultiplayerSession.Address);
        else
            manager.StartClientGame(MultiplayerSession.Address);
    }

    /// <summary>Called by NetworkPlayer.OnStartLocalPlayer.</summary>
    public void AttachLocalPlayer(NetworkPlayer player)
    {
        if (attached || player == null) return;
        attached = true;

        PlayerScript source = TemplatePlayer;
        PlayerScript target = player.PlayerScript;
        if (source == null || target == null) return;

        // The prefab lost all scene references when it was saved, so copy them back.
        CopyObjectReferences(source, target);
        target.cc = player.GetComponent<CharacterController>();

        Camera cam = player.GetComponentInChildren<Camera>(true);
        RewireSceneReferences(source, player, cam);
        MuteTemplate(source, cam);

        target.networkRemote = false;
        Debug.Log("[Figure Co-op] Local player attached: " + player.playerName);
    }

    private static void CopyObjectReferences(PlayerScript source, PlayerScript target)
    {
        FieldInfo[] fields = typeof(PlayerScript).GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (FieldInfo field in fields)
        {
            if (!typeof(Object).IsAssignableFrom(field.FieldType)) continue;
            field.SetValue(target, field.GetValue(source));
        }
    }

    private static void RewireSceneReferences(PlayerScript template, NetworkPlayer player, Camera cam)
    {
        CharacterController newCc = player.GetComponent<CharacterController>();
        Collider newCollider = player.GetComponent<Collider>();

        MonoBehaviour[] behaviours = FindObjectsOfType<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null) continue;
            FieldInfo[] fields = behaviour.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                object current = field.GetValue(behaviour);
                object replacement = null;

                if (current is Transform t && t != null && t.IsChildOf(template.transform))
                    replacement = (cam != null && t == cam.transform) ? cam.transform : player.transform;
                else if (current is PlayerScript ps && ps == template)
                    replacement = player.PlayerScript;
                else if (current is GameObject go && go == template.gameObject)
                    replacement = player.gameObject;
                else if (current is Camera camera && camera != null && camera.transform.IsChildOf(template.transform))
                    replacement = cam;
                else if (current is CharacterController cc && cc == template.GetComponent<CharacterController>())
                    replacement = newCc;
                else if (current is Collider col && col.transform.IsChildOf(template.transform))
                    replacement = newCollider;

                if (replacement != null) field.SetValue(behaviour, replacement);
            }
        }

        GameControllerScript gc = FindObjectOfType<GameControllerScript>();
        if (gc != null)
        {
            gc.player = player.PlayerScript;
            gc.playerTransform = player.transform;
            gc.playerCharacter = newCc;
            gc.playerCollider = newCollider;
            gc.playerCamera = cam;
            gc.cameraTransform = cam != null ? cam.transform : null;
        }

        if (cam != null) cam.tag = "MainCamera";
    }

    private static void MuteTemplate(PlayerScript template, Camera localCam)
    {
        Camera templateCam = template.GetComponentInChildren<Camera>(true);
        if (templateCam != null)
        {
            templateCam.enabled = false;
            templateCam.tag = "Untagged";
            AudioListener listener = templateCam.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;
            CameraScript cameraScript = templateCam.GetComponent<CameraScript>();
            if (cameraScript != null) cameraScript.enabled = false;
        }

        template.enabled = false;

        CharacterController controller = template.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
    }
}
