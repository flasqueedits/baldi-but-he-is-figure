using Mirror;
using UnityEngine;

/// <summary>
/// Plays the student model's walk / run / crouch animations on every OTHER client.
/// The local player's own model is hidden so it doesn't block the first-person camera.
/// </summary>
[RequireComponent(typeof(NetworkIdentity))]
public class NetworkCharacterAnimator : NetworkBehaviour
{
    private const float SendInterval = 0.066f;

    public Animator animator;
    public Renderer[] visualRenderers;
    public Transform visualRoot;

    [SyncVar] private float netSpeed;
    [SyncVar] private bool netCrouch;
    [SyncVar] private bool netRun;

    private PlayerScript playerScript;
    private float lastSend;
    private void Awake()
    {
        playerScript = GetComponent<PlayerScript>();

        if (animator == null) animator = GetComponentInChildren<Animator>(true);

        if (visualRenderers == null || visualRenderers.Length == 0)
        {
            Transform root = visualRoot != null ? visualRoot : (animator != null ? animator.transform : transform);
            visualRenderers = root.GetComponentsInChildren<Renderer>(true);
        }
    }

    public override void OnStartLocalPlayer()
    {
        SetVisualsVisible(false);
    }

    public override void OnStartClient()
    {
        if (!isLocalPlayer) SetVisualsVisible(true);
    }

    private void Update()
    {
        if (isLocalPlayer)
        {
            float speed = 0f;
            if (playerScript != null && playerScript.cc != null)
                speed = new Vector3(playerScript.cc.velocity.x, 0f, playerScript.cc.velocity.z).magnitude;

            bool crouch = playerScript != null && playerScript.isCrouching;
            bool run = !crouch && playerScript != null && speed > playerScript.walkSpeed + 0.4f;

            if (Time.time - lastSend >= SendInterval)
            {
                lastSend = Time.time;
                CmdAnimState(speed, crouch, run);
            }
            return;
        }

        if (animator == null) return;
        float smoothed = Mathf.Lerp(animator.GetFloat("Speed"), netSpeed, 12f * Time.deltaTime);
        animator.SetFloat("Speed", smoothed);
        animator.SetBool("Crouch", netCrouch);
        animator.SetBool("Run", netRun);
    }

    [Command]
    private void CmdAnimState(float speed, bool crouch, bool run)
    {
        netSpeed = speed;
        netCrouch = crouch;
        netRun = run;
    }

    private void SetVisualsVisible(bool visible)
    {
        if (visualRenderers != null)
        {
            foreach (Renderer renderer in visualRenderers)
                if (renderer != null) renderer.enabled = visible;
        }
    }
}
