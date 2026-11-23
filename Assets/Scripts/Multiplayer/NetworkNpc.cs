using Mirror;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Server-authoritative transform sync for the secondary NPCs. On clients the
/// original AI components are disabled and only the synced transform is applied.
/// </summary>
[DefaultExecutionOrder(100)]
public class NetworkNpc : NetworkBehaviour
{
    public MonoBehaviour[] aiScripts;
    public Animator animator;

    [SyncVar] private Vector3 netPosition;
    [SyncVar] private Quaternion netRotation = Quaternion.identity;
    [SyncVar] private float netSpeed;

    private NavMeshAgent agent;
    private bool initialised;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
    }

    public override void OnStartClient()
    {
        if (isServer) return;

        if (aiScripts != null)
            foreach (MonoBehaviour script in aiScripts)
                if (script != null) script.enabled = false;
        if (agent != null) agent.enabled = false;
    }

    private void FixedUpdate()
    {
        if (!NetworkServer.active && !NetworkClient.active) return;

        if (isServer)
        {
            netPosition = transform.position;
            netRotation = transform.rotation;
            netSpeed = agent != null ? agent.velocity.magnitude : 0f;
            return;
        }

        if (!initialised)
        {
            initialised = true;
            netPosition = transform.position;
            netRotation = transform.rotation;
        }

        float t = 12f * Time.deltaTime;
        transform.position = Vector3.Lerp(transform.position, netPosition, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, netRotation, t);
        if (animator != null) animator.SetFloat("Speed", netSpeed);
    }
}
