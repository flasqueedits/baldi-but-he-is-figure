using UnityEngine;

public class NearExitTriggerScript : MonoBehaviour
{
	private void OnTriggerEnter(Collider other)
	{
		if (gc.exitsReached < 3 & gc.finaleMode & other.tag == "Player")
		{
			if (NetworkWorld.Instance != null && Mirror.NetworkClient.active)
			{
				NetworkWorld.Instance.RequestExit(es != null ? es.gameObject.name : "");
			}
			else
			{
				gc.ExitReached();
				es.Lower();
			}
			if (gc.baldiScrpt.isActiveAndEnabled) gc.baldiScrpt.Hear(transform.position, 8f);
		}
	}
	public GameControllerScript gc;
	public EntranceScript es;
}
