using UnityEngine;

/// <summary>
/// Base class for scene objects whose state changes must reach every client.
/// Ids are assigned by the setup tool and must be unique in the scene.
/// </summary>
public abstract class NetworkSceneObject : MonoBehaviour
{
    public int id;

    protected virtual void Awake()
    {
        NetworkWorld.Register(this);
    }

    public abstract void ApplyAction(int action, float value);

    public void Send(int action, float value = 0f)
    {
        if (NetworkWorld.Instance != null) NetworkWorld.Instance.RequestAction(id, action, value);
    }
}
