using System.Collections;
using TMPro;
using UnityEngine;

public class AlertPanel : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI alertText;

	private Coroutine activeAlert;

	public void ShowAlert(string text, float typewriterSpeed, float displayDuration)
	{
		if (!(alertText == null))
		{
			if (activeAlert != null)
			{
				StopCoroutine(activeAlert);
			}
			activeAlert = StartCoroutine(TypewriterEffect(text, typewriterSpeed, displayDuration));
		}
	}

	private IEnumerator TypewriterEffect(string fullText, float typewriterSpeed, float displayDuration)
	{
		alertText.text = "";
		alertText.enabled = true;
		for (int i = 0; i <= fullText.Length; i++)
		{
			alertText.text = fullText.Substring(0, i);
			yield return new WaitForSeconds(typewriterSpeed);
		}
		yield return new WaitForSeconds(displayDuration);
		alertText.text = "";
		alertText.enabled = false;
		activeAlert = null;
	}
}
