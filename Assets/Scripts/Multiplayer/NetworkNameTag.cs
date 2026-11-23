using Mirror;
using UnityEngine;

/// <summary>Shows a floating name above remote students. Hidden on the local player.</summary>
[RequireComponent(typeof(NetworkIdentity))]
public class NetworkNameTag : NetworkBehaviour
{
    private TextMesh label;
    private NetworkPlayer playerScript;

    private void Awake()
    {
        playerScript = GetComponent<NetworkPlayer>();

        GameObject go = new GameObject("NameTag");
        label = go.AddComponent<TextMesh>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (label.font == null) label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.fontSize = 48;
        label.characterSize = 0.12f;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = new Color(1f, 0.92f, 0.4f);
        go.GetComponent<MeshRenderer>().sortingOrder = 10;
    }

    public override void OnStartLocalPlayer()
    {
        if (label != null) label.GetComponent<MeshRenderer>().enabled = false;
    }

    public override void OnStartClient()
    {
        if (!isLocalPlayer && label != null)
        {
            label.text = playerScript != null ? playerScript.playerName : "Student";
            label.GetComponent<MeshRenderer>().enabled = true;
        }
    }

    private void LateUpdate()
    {
        if (label == null || !label.GetComponent<MeshRenderer>().enabled) return;
        label.transform.position = transform.position + Vector3.up * 2.6f;
        if (Camera.main != null) label.transform.rotation = Camera.main.transform.rotation;
    }
}
