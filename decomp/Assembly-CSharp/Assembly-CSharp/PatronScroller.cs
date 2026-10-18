using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PatronScroller : MonoBehaviour
{
	[SerializeField]
	private PatronListData patronData;

	[SerializeField]
	private RectTransform viewport;

	[SerializeField]
	private float scrollSpeed = 40f;

	[SerializeField]
	private float nameSpacing = 40f;

	[SerializeField]
	private TMP_FontAsset font;

	[SerializeField]
	private float fontSize = 24f;

	[SerializeField]
	private Color textColor = Color.white;

	[SerializeField]
	private TextAlignmentOptions textAlignment = TextAlignmentOptions.Center;

	[Header("Dungeon Master Pulse")]
	[SerializeField]
	private Color dungeonMasterColorBase = new Color(1f, 0.85f, 0.3f);

	[SerializeField]
	private Color dungeonMasterColorPeak = Color.white;

	[SerializeField]
	private float pulseSpeed = 2f;

	private RectTransform content;

	private List<RectTransform> nameSlots = new List<RectTransform>();

	private List<TextMeshProUGUI> dungeonMasterTexts = new List<TextMeshProUGUI>();

	private float totalLoopHeight;

	private float viewportHeight;

	private string[] cachedNames;

	private int dungeonMasterCount;

	private void Start()
	{
		if (patronData == null)
		{
			enabled = false;
			return;
		}
		cachedNames = patronData.GetCombinedNames();
		dungeonMasterCount = ((patronData.dungeonMasters != null) ? patronData.dungeonMasters.Length : 0);
		if (cachedNames.Length == 0)
		{
			enabled = false;
			return;
		}
		viewportHeight = viewport.rect.height;
		GameObject gameObject = new GameObject("Content", typeof(RectTransform));
		content = gameObject.GetComponent<RectTransform>();
		content.SetParent(viewport, worldPositionStays: false);
		content.anchorMin = new Vector2(0f, 1f);
		content.anchorMax = new Vector2(1f, 1f);
		content.pivot = new Vector2(0.5f, 1f);
		content.anchoredPosition = Vector2.zero;
		content.sizeDelta = new Vector2(0f, 0f);
		SpawnNames();
	}

	private void SpawnNames()
	{
		string[] array = cachedNames;
		float num = (float)array.Length * nameSpacing;
		int a = Mathf.CeilToInt(viewportHeight * 2f / num);
		a = Mathf.Max(a, 2);
		int num2 = array.Length * a;
		totalLoopHeight = num;
		for (int i = 0; i < num2; i++)
		{
			int num3 = i % array.Length;
			string text = array[num3];
			bool flag = num3 < dungeonMasterCount;
			GameObject gameObject = new GameObject("PatronName_" + i, typeof(RectTransform));
			RectTransform component = gameObject.GetComponent<RectTransform>();
			component.SetParent(content, worldPositionStays: false);
			component.anchorMin = new Vector2(0f, 1f);
			component.anchorMax = new Vector2(1f, 1f);
			component.pivot = new Vector2(0.5f, 1f);
			component.sizeDelta = new Vector2(0f, nameSpacing);
			component.anchoredPosition = new Vector2(0f, (float)(-i) * nameSpacing);
			TextMeshProUGUI textMeshProUGUI = gameObject.AddComponent<TextMeshProUGUI>();
			textMeshProUGUI.text = text;
			textMeshProUGUI.fontSize = fontSize;
			textMeshProUGUI.color = (flag ? dungeonMasterColorBase : textColor);
			textMeshProUGUI.alignment = textAlignment;
			textMeshProUGUI.enableWordWrapping = false;
			textMeshProUGUI.overflowMode = TextOverflowModes.Ellipsis;
			textMeshProUGUI.raycastTarget = false;
			if (font != null)
			{
				textMeshProUGUI.font = font;
			}
			nameSlots.Add(component);
			if (flag)
			{
				dungeonMasterTexts.Add(textMeshProUGUI);
			}
		}
	}

	private void Update()
	{
		if (content == null)
		{
			return;
		}
		Vector2 anchoredPosition = content.anchoredPosition;
		anchoredPosition.y += scrollSpeed * Time.deltaTime;
		content.anchoredPosition = anchoredPosition;
		if (anchoredPosition.y >= totalLoopHeight)
		{
			anchoredPosition.y -= totalLoopHeight;
			content.anchoredPosition = anchoredPosition;
		}
		if (dungeonMasterTexts.Count <= 0)
		{
			return;
		}
		float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
		Color color = Color.Lerp(dungeonMasterColorBase, dungeonMasterColorPeak, t);
		for (int i = 0; i < dungeonMasterTexts.Count; i++)
		{
			if (dungeonMasterTexts[i] != null)
			{
				dungeonMasterTexts[i].color = color;
			}
		}
	}
}
