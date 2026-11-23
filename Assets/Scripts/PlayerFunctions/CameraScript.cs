using UnityEngine;

public class CameraScript : MonoBehaviour
{
	private void Start()
	{
		if (player == null) player = gameObject;
		if (ps == null) ps = GetComponentInParent<PlayerScript>();
		if (baldi == null)
		{
			BaldiScript sceneBaldi = FindObjectOfType<BaldiScript>();
			if (sceneBaldi != null) baldi = sceneBaldi.transform;
		}
		offset = transform.position - player.transform.position; //Defines the offset
		curY = offset.y;
		if (crouchOffset <= 0f) crouchOffset = 1f; //Default crouch offset if not set
	}
	private void Update()
	{
		if (ps == null) return;
		if (ps.jumpRope) //If the player is jump roping
		{
			velocity -= gravity * Time.deltaTime; //Decrease the velocity using gravity
			jumpHeight += velocity * Time.deltaTime; //Increase the jump height based on the velocity
			if (jumpHeight <= 0f) //When the player is on the floor, prevent the player from falling through.
			{
				jumpHeight = 0f;
				if (Singleton<InputManager>.Instance.GetActionKey(InputAction.Jump))
				{
					velocity = initVelocity; //Start the jump
				}
			}
			jumpHeightV3 = new Vector3(0f, jumpHeight, 0f); //Turn the float into a vector
		}
		else if (Singleton<InputManager>.Instance.GetActionKey(InputAction.LookBehind))
		{
			lookBehind = 180; //Look behind you
		}
		else
		{
			lookBehind = 0; //Don't look behind you
		}
	}
	private void LateUpdate()
	{
		if (ps == null || player == null) return;
		float targetY = ps.isCrouching ? offset.y - crouchOffset : offset.y;
		float currentY = Mathf.Lerp(curY, targetY, Time.deltaTime * 10f);
		curY = currentY;
		Vector3 currentOffset = new Vector3(offset.x, currentY, offset.z);
		if (!ps.gameOver & !ps.jumpRope)
		{
			transform.position = player.transform.position + currentOffset;
			transform.rotation = player.transform.rotation * Quaternion.Euler(0f, (float)lookBehind, 0f);
		}
		else if (ps.gameOver)
		{
			if (baldi == null) return;
			transform.position = baldi.transform.position + baldi.transform.forward * BaldiOffset.z + new Vector3(0f, BaldiOffset.y, 0f);
			transform.LookAt(new Vector3(baldi.position.x, baldi.position.y + BaldiOffset.y, baldi.position.z));
		}
		else if (ps.jumpRope)
		{
			transform.position = player.transform.position + currentOffset + jumpHeightV3;
			transform.rotation = player.transform.rotation;
		}
	}
	public GameObject player;
	public PlayerScript ps;
	public Transform baldi;
	public Vector3 BaldiOffset;
	public float initVelocity;
	public float velocity;
	public float gravity;
	private int lookBehind;
	public float crouchOffset;
	public Vector3 offset;
	public float curY;
	public float jumpHeight;
	public Vector3 jumpHeightV3;
}
