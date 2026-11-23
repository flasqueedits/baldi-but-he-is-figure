using UnityEngine;

public class NotebookScript : MonoBehaviour
{
	private void Start()
	{
		up = true;
	}

	private void Update()
	{
		NetworkNotebook netNotebook = GetComponent<NetworkNotebook>();
		if (NetworkGameState.IsActive && netNotebook != null && NetworkGameState.Instance.IsTaken(netNotebook.id))
		{
			return; // Already collected by someone in the co-op session.
		}

		if (gc.mode == "endless")
		{
			if (respawnTime > 0f)
			{
				if ((transform.position - player.position).magnitude > 60f)
				{
					respawnTime -= Time.deltaTime;
				}
			}
			else if (!up)
			{
				transform.position = new Vector3(transform.position.x, 4f, transform.position.z);
				up = true;
				audioDevice.Play();
			}
		}
		if (Singleton<InputManager>.Instance.GetActionKey(InputAction.Interact) && Time.timeScale != 0f)
		{
			Ray ray = Camera.main.ScreenPointToRay(new Vector3((float)(Screen.width / 2), (float)(Screen.height / 2), 0f));
			RaycastHit raycastHit;
			if (Physics.Raycast(ray, out raycastHit) && (raycastHit.transform.tag == "Notebook" & Vector3.Distance(player.position, transform.position) < openingDistance))
			{
				if (NetworkGameState.IsActive && netNotebook != null)
				{
					NetworkGameState.Instance.RequestCollect(netNotebook);
				}
				else
				{
					CollectLocal();
				}
			}
		}
	}

	/// <summary>Local feedback for collecting a notebook (single player or the co-op collector).</summary>
	public void CollectLocal()
	{
		transform.position = new Vector3(transform.position.x, -20f, transform.position.z);
		up = false;
		respawnTime = 120f;
		if (!NetworkGameState.IsActive) gc.CollectNotebook(); // co-op uses the shared count
		GameObject gameObject = Instantiate<GameObject>(learningGame);
		gameObject.GetComponent<MathGameScript>().gc = gc;
		gameObject.GetComponent<MathGameScript>().baldiScript = bsc;
		gameObject.GetComponent<MathGameScript>().playerPosition = player.position;
	}

	public float openingDistance;
	public GameControllerScript gc;
	public BaldiScript bsc;
	public float respawnTime;
	public bool up;
	public Transform player;
	public GameObject learningGame;
	public AudioSource audioDevice;
}
