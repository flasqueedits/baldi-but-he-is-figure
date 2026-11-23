using Mirror;
using UnityEngine;

/// <summary>
/// Client-authoritative player movement, replicated to the server with Commands
/// and back to other clients with SyncVars.
/// </summary>
[RequireComponent(typeof(NetworkIdentity))]
public class NetworkPlayer : NetworkBehaviour
{
    private const float SendInterval = 0.05f;

    [SyncVar(hook = nameof(OnPositionChanged))] private Vector3 netPosition;
    [SyncVar(hook = nameof(OnRotationChanged))] private Quaternion netRotation;

    [SyncVar] public int playerNumber;
    [SyncVar] public string playerName = "Student";
    public bool caught;

    public PlayerScript PlayerScript { get; private set; }

    /// <summary>Fired on the caught client. Subscribe from your custom GUI.</summary>
    public static event System.Action<NetworkPlayer> Caught;

    private Camera cam;
    private AudioListener audioListener;
    private CameraScript cameraScript;
    private Vector3 targetPosition;
    private Quaternion targetRotation = Quaternion.identity;
    private float lastSend;

    private void Awake()
    {
        PlayerScript = GetComponent<PlayerScript>();
        cam = GetComponentInChildren<Camera>(true);
        if (cam != null)
        {
            audioListener = cam.GetComponent<AudioListener>();
            cameraScript = cam.GetComponent<CameraScript>();
        }

        PlayerScript template = MultiplayerBootstrap.TemplatePlayer;
        if (template != null && PlayerScript != null)
        {
            PlayerScript.gc = template.gc;
            PlayerScript.baldi = template.baldi;
            PlayerScript.door = template.door;
            PlayerScript.playtime = template.playtime;
            PlayerScript.staminaBar = template.staminaBar;
            PlayerScript.blackSky = template.blackSky;
            PlayerScript.hud = template.hud;
            PlayerScript.jumpRopeScreen = template.jumpRopeScreen;
            PlayerScript.gottaSweep = template.gottaSweep;
            PlayerScript.firstPrize = template.firstPrize;
            PlayerScript.firstPrizeTransform = template.firstPrizeTransform;
            PlayerScript.walkSpeed = template.walkSpeed;
            PlayerScript.runSpeed = template.runSpeed;
            PlayerScript.crouchSpeed = template.crouchSpeed;
            PlayerScript.maxStamina = template.maxStamina;
            PlayerScript.staminaRate = template.staminaRate;
            PlayerScript.mouseSensitivity = template.mouseSensitivity;
            PlayerScript.height = template.height;

            if (cameraScript != null && template.GetComponentInChildren<CameraScript>(true) != null)
            {
                CameraScript templateCam = template.GetComponentInChildren<CameraScript>(true);
                cameraScript.offset = templateCam.offset;
                cameraScript.BaldiOffset = templateCam.BaldiOffset;
                cameraScript.crouchOffset = templateCam.crouchOffset;
                cameraScript.initVelocity = templateCam.initVelocity;
                cameraScript.gravity = templateCam.gravity;
                cameraScript.baldi = templateCam.baldi;
            }
        }

        if (cameraScript != null)
        {
            cameraScript.ps = PlayerScript;
            cameraScript.player = gameObject;
            if (cameraScript.baldi == null)
            {
                BaldiScript sceneBaldi = FindObjectOfType<BaldiScript>();
                if (sceneBaldi != null) cameraScript.baldi = sceneBaldi.transform;
            }
        }

        targetPosition = transform.position;
        targetRotation = transform.rotation;
    }

    public override void OnStartLocalPlayer()
    {
        if (cam != null)
        {
            cam.enabled = true;
            cam.tag = "MainCamera";
            if (audioListener != null) audioListener.enabled = true;
            if (cameraScript != null) cameraScript.enabled = true;
        }

        if (PlayerScript != null) PlayerScript.networkRemote = false;

        if (MultiplayerBootstrap.Instance != null)
            MultiplayerBootstrap.Instance.AttachLocalPlayer(this);

        NetworkHud.Create(this);
        Debug.Log("[Figure Co-op] You are " + playerName);
    }

    public override void OnStartClient()
    {
        if (isLocalPlayer) return;

        if (PlayerScript != null) PlayerScript.networkRemote = true;
        if (cam != null)
        {
            cam.enabled = false;
            if (audioListener != null) audioListener.enabled = false;
            if (cameraScript != null) cameraScript.enabled = false;
        }
    }

    private void Update()
    {
        if (isLocalPlayer)
        {
            if (Time.time - lastSend >= SendInterval)
            {
                lastSend = Time.time;
                CmdSyncState(transform.position, transform.rotation);
            }
            return;
        }

        // Remote players (on a client AND on the host's copy of them).
        float t = 14f * Time.deltaTime;
        transform.position = Vector3.Lerp(transform.position, targetPosition, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
    }

    [Command]
    private void CmdSyncState(Vector3 position, Quaternion rotation)
    {
        netPosition = position;
        netRotation = rotation;
        targetPosition = position;
        targetRotation = rotation;
    }

    private void OnPositionChanged(Vector3 oldValue, Vector3 newValue)
    {
        targetPosition = newValue;
    }

    private void OnRotationChanged(Quaternion oldValue, Quaternion newValue)
    {
        targetRotation = newValue;
    }

    [TargetRpc]
    public void TargetCaught()
    {
        caught = true;
        if (PlayerScript != null) PlayerScript.networkRemote = true;
        NetworkHud.ShowCaught();
        if (Caught != null) Caught(this);
    }
}
