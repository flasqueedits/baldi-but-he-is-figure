using UnityEngine;

/// <summary>Replicates SwingingDoorScript state changes to every client.</summary>
public class NetworkSwingingDoor : NetworkSceneObject
{
    private SwingingDoorScript door;

    protected override void Awake()
    {
        base.Awake();
        door = GetComponent<SwingingDoorScript>();
    }

    public override void ApplyAction(int action, float value)
    {
        if (door == null) return;
        switch (action)
        {
            case 0:
                door.bDoorOpen = true;
                break;
            case 1: door.LockDoorInternal(value); break;
            case 2: door.UnlockDoorInternal(); break;
        }
    }
}
