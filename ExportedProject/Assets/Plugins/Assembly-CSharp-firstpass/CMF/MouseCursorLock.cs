using UnityEngine;

namespace CMF
{
	public class MouseCursorLock : MonoBehaviour
	{
		public bool lockCursorAtGameStart = true;

		public KeyCode unlockKeyCode = KeyCode.Escape;

		public KeyCode lockKeyCode = KeyCode.Mouse0;

		private void Start()
		{
			if (lockCursorAtGameStart)
			{
				Cursor.lockState = CursorLockMode.Locked;
				Cursor.visible = false;
			}
		}

		private void Update()
		{
			if (Time.timeScale != 0f)
			{
				if (Input.GetKeyDown(unlockKeyCode))
				{
					Cursor.lockState = CursorLockMode.None;
					Cursor.visible = true;
				}
				if (Input.GetKeyDown(lockKeyCode))
				{
					Cursor.lockState = CursorLockMode.Locked;
					Cursor.visible = false;
				}
			}
		}
	}
}
