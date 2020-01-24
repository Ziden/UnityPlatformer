using UnityEngine;
using System.Collections;

public class SamuraiCamera : MonoBehaviour
{
	//offset from the viewport center to fix damping
	public float m_DampTime = 0;
	public PlayerBhv m_Target;

	public bool floor = true;

    private static float vertExtent;
	private static float horzExtent;

	private static Bounds? roomBounds;

	void Start()
	{
		if (m_Target == null)
		{
			m_Target = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerBhv>();
		}
		vertExtent = Camera.main.orthographicSize;
		horzExtent = vertExtent * Screen.width / Screen.height;
	}

    public static void SetRoomBounds(Bounds bounds) {
		roomBounds = bounds;
	}

	void Update()
	{
		if (m_Target)
		{
			float targetX = m_Target.transform.position.x;
			float targetY = m_Target.transform.position.y;

			var minViewY = targetY - vertExtent;
			var maxViewY = targetY + vertExtent;
			var minViewX = targetX - horzExtent;
			var maxViewX = targetX + horzExtent;

            if(roomBounds.HasValue)
			{
				var room = roomBounds.Value;

				if (minViewY < room.min.y)
					targetY += (room.min.y - minViewY);
				if (maxViewY > room.max.y)
					targetY -= (maxViewY - room.max.y);
				if (minViewX < room.min.x)
					targetX += (room.min.x - minViewX);
				if (maxViewX > room.max.x)
					targetX -= (maxViewX - room.max.x);
			}
			transform.position = new Vector3(targetX, targetY, transform.position.z);
		}
	}
}
