using UnityEngine;
using UnityEngine.AI;

public class BaldiScript : MonoBehaviour
{
    public NetworkBaldi netBaldi;

    private void Start()
    {
        baldiAudio = GetComponent<AudioSource>();
        agent = GetComponent<NavMeshAgent>();
        if (netBaldi == null) netBaldi = GetComponent<NetworkBaldi>();
        timeToMove = baseTime;
        Wander();

        if (PlayerPrefs.GetInt("Rumble") == 1)
        {
            rumble = true;
        }
    }

    private void Update()
    {
        if (timeToMove > 0f)
        {
            timeToMove -= 1f * Time.deltaTime;
        }
        else
        {
            Move();
        }

        if (coolDown > 0f)
        {
            coolDown -= 1f * Time.deltaTime;
        }

        if (baldiTempAnger > 0f)
        {
            baldiTempAnger -= 0.02f * Time.deltaTime;
        }
        else
        {
            baldiTempAnger = 0f;
        }

        if (antiHearingTime > 0f)
        {
            antiHearingTime -= Time.deltaTime;
        }
        else
        {
            antiHearing = false;
        }

        if (endless)
        {
            if (timeToAnger > 0f)
            {
                timeToAnger -= 1f * Time.deltaTime;
            }
            else
            {
                timeToAnger = angerFrequency;
                GetAngry(angerRate);
                angerRate += angerRateRate;
            }
        }
    }

    private void FixedUpdate()
    {
        if (player == null || playerScript == null) return;

        if (playerScript != null && !playerScript.isCrouching)
        {
            // Ayakta: Baldi oyuncuyu gorur ve kovalar
            agent.speed = speed;
            agent.SetDestination(player.position);

            if (!hasScreamed)
            {
                PlayBaldiSound(2);
                hasScreamed = true;
            }
        }
        else
        {
            // Egilince: Baldi kör, rastgele dolastir
            if (!isBlind)
            {
                isBlind = true;
                hasScreamed = false;
                Wander();
            }
            agent.speed = speed * 0.5f;

            // Yolda carpism kontrolu
            if (!hasCaught && Vector3.Distance(transform.position, player.position) <= 2.5f)
            {
                hasCaught = true;
                CatchPlayer();
            }
        }

        // Ayaktayken de carpism kontrolu
        if (!hasCaught && Vector3.Distance(transform.position, player.position) <= 2.5f)
        {
            hasCaught = true;
            CatchPlayer();
        }
    }

    private void Wander()
    {
        wanderer.GetNewTarget();
        agent.SetDestination(wanderTarget.position);
        coolDown = 1f;
        currentPriority = 0f;
    }

    private void Move()
    {
        // Hedefe ulaştıysa ve cooldown bittiyse tekrar rastgele gezinsin
        if (transform.position == previous && coolDown < 0f)
        {
            Wander();
        }

        moveFrames = 10f;
        timeToMove = baldiWait - baldiTempAnger;
        previous = transform.position;
        
        PlayBaldiSound(0);

        if (rumble)
        {
            float num = Vector3.Distance(transform.position, player.position);
            if (num < vibrationDistance)
            {
                float motorLevel = 1f - num / vibrationDistance;
            }
        }
    }

    // =============================================================
    // FIGURE SES ALGILAMA SİSTEMİ
    // =============================================================
    public void Hear(Vector3 soundLocation, float priority)
    {
        // Only a pure client forwards sounds to the server. In single player
        // (no network at all) the local AI path is used.
        if (netBaldi != null && netBaldi.isClient && !netBaldi.isServer)
        {
            netBaldi.CmdHear(soundLocation, priority);
            return;
        }
        HearLocal(soundLocation, priority);
    }

    public void HearLocal(Vector3 soundLocation, float priority)
    {
        if (!antiHearing && priority >= currentPriority)
        {
            agent.SetDestination(soundLocation);
            currentPriority = priority;
            coolDown = 1.5f;

            if (!baldiAudio.isPlaying)
            {
                PlayBaldiSound(1);
            }
        }
    }

    private void PlayBaldiSound(int id)
    {
        // RPC only works while a server is running. Single player must stay local.
        if (netBaldi != null && netBaldi.isServer)
        {
            netBaldi.RpcPlaySound(id);
            return;
        }
        if (netBaldi != null && netBaldi.isClient) return; // server RPC will play it

        if (baldiAudio == null) baldiAudio = GetComponent<AudioSource>();
        AudioClip clip = id == 0 ? slap : (id == 1 ? roar : scream);
        if (clip != null && baldiAudio != null) baldiAudio.PlayOneShot(clip);
        if (id != 2 && baldiAnimator != null) baldiAnimator.SetTrigger("slap");
    }

    public void ActivateAntiHearing(float t)
    {
        Wander();
        antiHearing = true;
        antiHearingTime = t;
    }

    public void GetAngry(float value)
    {
        baldiAnger += value;
        if (baldiAnger < 0.5f)
        {
            baldiAnger = 0.5f;
        }
    }

    public void GetTempAngry(float value)
    {
        baldiTempAnger += value;
    }

    private void CatchPlayer()
    {
        NetworkPlayer victim = playerScript != null ? playerScript.GetComponent<NetworkPlayer>() : null;
        if (netBaldi != null && victim != null)
        {
            netBaldi.ServerCatch(victim);
        }
        else
        {
            Debug.Log("Figure Baldi seni yakaladı!");
        }
    }

    public void ResetCatchState()
    {
        hasCaught = false;
        hasScreamed = false;
        isBlind = false;
    }

    // Değişkenler (Varsayılan değerler korundu)
    public bool db;
    public float baseTime;
    public float speed;
    public float timeToMove;
    public float baldiAnger;
    public float baldiTempAnger;
    public float baldiWait;
    public float baldiSpeedScale;
    private float moveFrames;
    private float currentPriority;
    public bool antiHearing;
    public float antiHearingTime;
    public float vibrationDistance;
    public float angerRate;
    public float angerRateRate;
    public float angerFrequency;
    public float timeToAnger;
    public bool endless;
    public Transform player;
    public PlayerScript playerScript;
    public Transform wanderTarget;
    public AILocationSelectorScript wanderer;
    private AudioSource baldiAudio;
    public AudioClip slap;
    public AudioClip roar;
    public AudioClip scream;
    public Animator baldiAnimator;
    public float coolDown;
    private Vector3 previous;
    private bool rumble;
    private bool hasScreamed;
    private bool hasCaught;
    private bool isBlind;
    private NavMeshAgent agent;
}