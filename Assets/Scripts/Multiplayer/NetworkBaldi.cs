using Mirror;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Server-authoritative Figure/Baldi: picks the nearest alive student, syncs its
/// transform and effects to every client, and routes sound-based chasing.
/// </summary>
[DefaultExecutionOrder(100)]
public class NetworkBaldi : NetworkBehaviour
{
    public static NetworkBaldi Instance { get; private set; }

    [SyncVar] private Vector3 netPosition;
    [SyncVar] private Quaternion netRotation = Quaternion.identity;

    private BaldiScript baldi;
    private NavMeshAgent agent;

    private void Awake()
    {
        Instance = this;
        baldi = GetComponent<BaldiScript>();
        agent = GetComponent<NavMeshAgent>();
    }

    public override void OnStartClient()
    {
        // Only the server simulates the AI. Clients just render the synced transform.
        if (!isServer)
        {
            if (baldi != null) baldi.enabled = false;
            if (agent != null) agent.enabled = false;
        }
    }

    private void FixedUpdate()
    {
        // Single player: the scene Baldi has this component but no network is running.
        // Let the original AI fully own the transform.
        if (!NetworkServer.active && !NetworkClient.active) return;

        if (isServer) ServerPickTarget();

        if (!isServer)
        {
            transform.position = Vector3.Lerp(transform.position, netPosition, 12f * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, netRotation, 12f * Time.deltaTime);
        }
    }

    [Server]
    private void ServerPickTarget()
    {
        if (baldi == null) baldi = GetComponent<BaldiScript>();

        NetworkPlayer best = null;
        float bestDistance = float.MaxValue;
        NetworkPlayer[] players = FindObjectsOfType<NetworkPlayer>();
        foreach (NetworkPlayer player in players)
        {
            if (player == null || player.caught) continue;
            float distance = (player.transform.position - transform.position).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = player;
            }
        }

        if (best != null)
        {
            baldi.player = best.transform;
            baldi.playerScript = best.PlayerScript;
        }
        else
        {
            baldi.player = null;
            baldi.playerScript = null;
        }

        netPosition = transform.position;
        netRotation = transform.rotation;
    }

    /// <summary>Client -> server wrapper for the Figure's sound detection.</summary>
    [Command(requiresAuthority = false)]
    public void CmdHear(Vector3 soundLocation, float priority)
    {
        if (baldi != null && baldi.isActiveAndEnabled)
            baldi.HearLocal(soundLocation, priority);
    }

    [ClientRpc]
    public void RpcPlaySound(int id)
    {
        AudioSource source = GetComponent<AudioSource>();
        if (baldi == null) baldi = GetComponent<BaldiScript>();
        if (source == null || baldi == null) return;

        AudioClip clip = id == 0 ? baldi.slap : (id == 1 ? baldi.roar : baldi.scream);
        if (clip != null) source.PlayOneShot(clip);
        if (id != 2 && baldi.baldiAnimator != null) baldi.baldiAnimator.SetTrigger("slap");
    }

    [Server]
    public void ServerCatch(NetworkPlayer victim)
    {
        if (victim == null || victim.caught) return;
        victim.caught = true;
        victim.TargetCaught();
        if (baldi != null) baldi.ResetCatchState();
        Debug.Log("[Figure Co-op] " + victim.playerName + " was caught!");
    }
}
