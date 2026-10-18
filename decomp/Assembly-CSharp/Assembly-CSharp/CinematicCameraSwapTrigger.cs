using UnityEngine;

public class CinematicCameraSwapTrigger : CameraSwapTrigger
{
	[Header("Exit Animation")]
	[SerializeField]
	private string exitAnimationTriggerName = "ExitAnimation";

	[Header("Looping Cinematic Audio")]
	[SerializeField]
	private AudioClip loopingAudioClip;

	[SerializeField]
	private AudioSource loopingAudioSource;

	[SerializeField]
	[Range(0f, 1f)]
	private float loopingAudioVolume = 1f;

	[Header("Ambient Audio (muted during cinematic)")]
	[SerializeField]
	private AudioSource ambientAudioSource;

	[Header("Exit Prompt")]
	[Tooltip("UI shown while in the cinematic telling the player how to leave. Put an InputPromptLabel on it (Interact action).")]
	[SerializeField]
	private GameObject exitPromptUI;

	private static int activeCount;

	private bool isPlayingExitAnimation;

	public static bool IsAnyInCinematicView => activeCount > 0;

	public override bool CanInteract
	{
		get
		{
			if (!isSwapping)
			{
				return !isInCinematicView;
			}
			return false;
		}
	}

	private void OnEnable()
	{
		useAnimationEvents = true;
		OnSwapCompleted.AddListener(HandleEnteredCinematic);
		OnReturnedToFirstPerson.AddListener(HandleExitedCinematic);
		EnsureLoopingAudioSource();
	}

	private void OnDisable()
	{
		OnSwapCompleted.RemoveListener(HandleEnteredCinematic);
		OnReturnedToFirstPerson.RemoveListener(HandleExitedCinematic);
		StopLoopingAudio();
		if (isInCinematicView && activeCount > 0)
		{
			activeCount--;
		}
		if (isInCinematicView)
		{
			SetAmbientMuted(muted: false);
		}
	}

	public override void OnInteracted()
	{
		if (!isSwapping)
		{
			if (isInCinematicView)
			{
				TryStartExit();
			}
			else
			{
				ActivateTrigger();
			}
		}
	}

	protected override void Update()
	{
		if (isInCinematicView && !isSwapping && InputManager.Interact != null && InputManager.Interact.WasPressedThisFrame())
		{
			TryStartExit();
		}
	}

	private void TryStartExit()
	{
		if (!isPlayingExitAnimation)
		{
			isPlayingExitAnimation = true;
			ShowExitPrompt(show: false);
			if (animatorToTrigger != null && !string.IsNullOrEmpty(exitAnimationTriggerName))
			{
				animatorToTrigger.SetTrigger(exitAnimationTriggerName);
			}
		}
	}

	private void HandleEnteredCinematic()
	{
		activeCount++;
		StartLoopingAudio();
		SetAmbientMuted(muted: true);
		ShowExitPrompt(show: true);
	}

	private void HandleExitedCinematic()
	{
		if (activeCount > 0)
		{
			activeCount--;
		}
		isPlayingExitAnimation = false;
		StopLoopingAudio();
		SetAmbientMuted(muted: false);
		ShowExitPrompt(show: false);
	}

	private void ShowExitPrompt(bool show)
	{
		if (exitPromptUI != null)
		{
			exitPromptUI.SetActive(show);
		}
	}

	private void SetAmbientMuted(bool muted)
	{
		if (ambientAudioSource != null)
		{
			ambientAudioSource.mute = muted;
		}
	}

	private void EnsureLoopingAudioSource()
	{
		if (loopingAudioSource == null)
		{
			loopingAudioSource = gameObject.AddComponent<AudioSource>();
			loopingAudioSource.playOnAwake = false;
		}
	}

	private void StartLoopingAudio()
	{
		if (!(loopingAudioClip == null) && !(loopingAudioSource == null))
		{
			loopingAudioSource.clip = loopingAudioClip;
			loopingAudioSource.loop = true;
			loopingAudioSource.volume = loopingAudioVolume;
			loopingAudioSource.Play();
		}
	}

	private void StopLoopingAudio()
	{
		if (loopingAudioSource != null && loopingAudioSource.isPlaying)
		{
			loopingAudioSource.Stop();
		}
	}
}
