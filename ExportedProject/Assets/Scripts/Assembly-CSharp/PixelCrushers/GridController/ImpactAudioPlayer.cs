using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace PixelCrushers.GridController
{
	public class ImpactAudioPlayer : MonoBehaviour
	{
		private AudioSource audioSource;

		public void Play(AudioClip clip, float volume = 1f, float spatialBlend = 1f, AudioMixerGroup mixerGroup = null)
		{
			if (clip == null)
			{
				Object.Destroy(base.gameObject);
				return;
			}
			audioSource = base.gameObject.AddComponent<AudioSource>();
			audioSource.clip = clip;
			audioSource.volume = volume;
			audioSource.spatialBlend = spatialBlend;
			audioSource.outputAudioMixerGroup = mixerGroup;
			audioSource.playOnAwake = false;
			audioSource.rolloffMode = AudioRolloffMode.Linear;
			audioSource.minDistance = 1f;
			audioSource.maxDistance = 20f;
			audioSource.Play();
			StartCoroutine(DestroyAfterAudioCompletes(clip.length));
		}

		private IEnumerator DestroyAfterAudioCompletes(float clipLength)
		{
			yield return new WaitForSeconds(clipLength + 0.1f);
			Object.Destroy(base.gameObject);
		}
	}
}
