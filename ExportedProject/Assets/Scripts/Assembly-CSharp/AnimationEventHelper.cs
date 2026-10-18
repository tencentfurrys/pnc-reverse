using UnityEngine;
using UnityEngine.Events;

public class AnimationEventHelper : MonoBehaviour
{
	[Header("References")]
	[SerializeField]
	private CameraSwapTrigger cameraSwapTrigger;

	[Header("Events")]
	public UnityEvent OnAnimationStarted;

	public UnityEvent OnAnimationMidPoint;

	public UnityEvent OnAnimationEnded;

	public UnityEvent OnReturnToFirstPerson;

	private void Awake()
	{
		if (cameraSwapTrigger == null)
		{
			cameraSwapTrigger = Object.FindAnyObjectByType<CameraSwapTrigger>();
		}
	}

	public void OnAnimationStart()
	{
		OnAnimationStarted?.Invoke();
		if (cameraSwapTrigger != null)
		{
			cameraSwapTrigger.OnAnimationStart();
		}
	}

	public void OnAnimationMid()
	{
		OnAnimationMidPoint?.Invoke();
		if (cameraSwapTrigger != null)
		{
			cameraSwapTrigger.OnAnimationMidPoint();
		}
	}

	public void OnAnimationEnd()
	{
		OnAnimationEnded?.Invoke();
		if (cameraSwapTrigger != null)
		{
			cameraSwapTrigger.OnAnimationComplete();
		}
	}

	public void ReturnToFirstPerson()
	{
		OnReturnToFirstPerson?.Invoke();
		if (cameraSwapTrigger != null)
		{
			cameraSwapTrigger.ReturnToFirstPersonFromAnimation();
		}
	}

	public void PlayAudioByIndex(int index)
	{
		if (cameraSwapTrigger != null)
		{
			cameraSwapTrigger.PlayAudioByIndex(index);
		}
	}

	public void PlayRandomAudio()
	{
		if (cameraSwapTrigger != null)
		{
			cameraSwapTrigger.PlayRandomAudio();
		}
	}

	public void StopAudio()
	{
		if (cameraSwapTrigger != null)
		{
			cameraSwapTrigger.StopAudio();
		}
	}

	public void OnAnimationStartWithAudio(int audioIndex)
	{
		PlayAudioByIndex(audioIndex);
		OnAnimationStart();
	}

	public void OnAnimationMidWithAudio(int audioIndex)
	{
		PlayAudioByIndex(audioIndex);
		OnAnimationMid();
	}

	public void OnAnimationEndWithAudio(int audioIndex)
	{
		PlayAudioByIndex(audioIndex);
		OnAnimationEnd();
	}

	public void ReturnToFirstPersonWithAudio(int audioIndex)
	{
		PlayAudioByIndex(audioIndex);
		ReturnToFirstPerson();
	}

	public void SetCameraSwapTrigger(CameraSwapTrigger trigger)
	{
		cameraSwapTrigger = trigger;
	}

	public void TriggerCustomEvent(string eventName)
	{
		if (cameraSwapTrigger != null)
		{
			cameraSwapTrigger.OnCustomAnimationEvent(eventName);
		}
	}

	public void TriggerCustomEventWithAudio(string eventName, int audioIndex)
	{
		PlayAudioByIndex(audioIndex);
		TriggerCustomEvent(eventName);
	}

	public bool HasValidCameraSwapTrigger()
	{
		return cameraSwapTrigger != null;
	}

	public int GetAvailableAudioClipCount()
	{
		if (cameraSwapTrigger != null)
		{
			return cameraSwapTrigger.GetAudioClipCount();
		}
		return 0;
	}
}
