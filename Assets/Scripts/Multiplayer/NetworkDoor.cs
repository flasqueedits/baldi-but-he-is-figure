using UnityEngine;

/// <summary>Replicates DoorScript state changes to every client.</summary>
public class NetworkDoor : NetworkSceneObject
{
    private DoorScript door;

    protected override void Awake()
    {
        base.Awake();
        door = GetComponent<DoorScript>();
    }

    public override void ApplyAction(int action, float value)
    {
        if (door == null) return;
        switch (action)
        {
            case 0: door.OpenDoorInternal(); break;
            case 1: door.LockDoorInternal(value); break;
            case 2: door.UnlockDoorInternal(); break;
            case 3: door.SilenceDoorInternal(); break;
        }
    }
}
