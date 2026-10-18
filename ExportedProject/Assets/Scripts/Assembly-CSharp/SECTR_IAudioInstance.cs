using UnityEngine;

public interface SECTR_IAudioInstance
{
	int Generation { get; }

	bool Active { get; }

	Vector3 Position { get; set; }

	Vector3 LocalPosition { get; set; }

	float Volume { get; set; }

	float Pitch { get; set; }

	bool Mute { get; set; }

	bool Pause { get; set; }

	int TimeSamples { get; set; }

	float TimeSeconds { get; set; }

	void Stop(bool stopImmediately);

	void ForceInfinite();

	void ForceOcclusion(bool occluded);

	void SetParameter(string param, float value);

	AudioSource GetInternalAudioSource();
}
