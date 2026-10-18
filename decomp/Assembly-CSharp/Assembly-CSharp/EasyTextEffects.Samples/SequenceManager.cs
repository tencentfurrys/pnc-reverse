using System.Collections.Generic;
using UnityEngine;

namespace EasyTextEffects.Samples;

public class SequenceManager : MonoBehaviour
{
	public List<GameObject> slides;

	private int currentSlideIndex_ = -1;

	public List<float> slideDelays;

	private void Start()
	{
		for (int i = 0; i < slides.Count; i++)
		{
			slides[i].SetActive(value: false);
		}
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
		{
			NextSlide();
		}
	}

	public void NextSlide()
	{
		if (currentSlideIndex_ > -1 && currentSlideIndex_ < slides.Count)
		{
			StopEffect(currentSlideIndex_);
		}
		currentSlideIndex_++;
		if (currentSlideIndex_ > -1 && currentSlideIndex_ < slides.Count)
		{
			Invoke("StartCurrentEffect", slideDelays[currentSlideIndex_]);
		}
		else
		{
			currentSlideIndex_ = -1;
		}
	}

	private void StopEffect(int _index)
	{
		GameObject currentSlide = slides[_index];
		TextEffect componentInChildren = currentSlide.GetComponentInChildren<TextEffect>();
		if (componentInChildren == null)
		{
			currentSlide.SetActive(value: false);
			return;
		}
		componentInChildren.StopOnStartEffects();
		componentInChildren.StartManualEffect("exit");
		GlobalTextEffectEntry effect = componentInChildren.FindManualEffect("exit");
		if (effect == null)
		{
			currentSlide.SetActive(value: false);
			return;
		}
		effect.onEffectCompleted.AddListener(() =>
		{
			currentSlide.SetActive(value: false);
			effect.onEffectCompleted.RemoveAllListeners();
		});
	}

	private void StartEffect(int _index)
	{
		GameObject gameObject = slides[_index];
		gameObject.SetActive(value: true);
		TextEffect componentInChildren = gameObject.GetComponentInChildren<TextEffect>();
		if (componentInChildren != null)
		{
			componentInChildren.UpdateStyleInfos();
		}
	}

	private void StartCurrentEffect()
	{
		StartEffect(currentSlideIndex_);
	}
}
