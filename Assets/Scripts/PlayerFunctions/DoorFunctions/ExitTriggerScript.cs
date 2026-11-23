using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitTriggerScript : MonoBehaviour
{
	private void OnTriggerEnter(Collider other)
	{
		if (gc.notebooks >= 7 & other.tag == "Player")
		{
			if (NetworkWorld.Instance != null && Mirror.NetworkClient.active)
			{
				NetworkWorld.Instance.RequestWin();
			}
			else if (gc.failedNotebooks >= 7)
			{
				SceneManager.LoadScene(SecretScene);
			}
			else
			{
				SceneManager.LoadScene(ResultsScene);
			}
		}
	}
	public GameControllerScript gc;
	public string ResultsScene;
	public string SecretScene;
}
