using UnityEngine;

public class PickupScript : MonoBehaviour
{
	private void Update()
	{
		if (Singleton<InputManager>.Instance.GetActionKeyDown(InputAction.Interact) && Time.timeScale != 0f)
		{
			Ray ray = Camera.main.ScreenPointToRay(new Vector3((float)(Screen.width / 2), (float)(Screen.height / 2), 0f));
			RaycastHit raycastHit;
			if (Physics.Raycast(ray, out raycastHit))
			{
				string name = raycastHit.transform.name;
				int itemId = GetItemId(name);
				if (itemId > 0 && Vector3.Distance(player.position, transform.position) < 10f)
				{
					if (NetworkWorld.Instance != null && Mirror.NetworkClient.active)
					{
						NetworkWorld.Instance.RequestPickup(name, itemId);
					}
					else
					{
						raycastHit.transform.gameObject.SetActive(false);
						gc.CollectItem(itemId);
					}
				}
			}
		}
	}

	private static int GetItemId(string name)
	{
		if (name == "Pickup_EnergyFlavoredZestyBar") return 1;
		if (name == "Pickup_YellowDoorLock") return 2;
		if (name == "Pickup_Key") return 3;
		if (name == "Pickup_BSODA") return 4;
		if (name == "Pickup_Quarter") return 5;
		if (name == "Pickup_Tape") return 6;
		if (name == "Pickup_AlarmClock") return 7;
		if (name == "Pickup_WD-3D") return 8;
		if (name == "Pickup_SafetyScissors") return 9;
		if (name == "Pickup_BigBoots") return 10;
		if (name == "Pickup_Teleporter") return 11;
		return 0;
	}

	public GameControllerScript gc;
	public Transform player;
}
