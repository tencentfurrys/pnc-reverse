using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelCrushers.GridController
{
	public class ItemUnlockPanel : MonoBehaviour
	{
		[Header("UI References")]
		[SerializeField]
		private Animator animator;

		[SerializeField]
		private Image itemIconImage;

		[SerializeField]
		private TextMeshProUGUI itemNameText;

		[SerializeField]
		private TextMeshProUGUI itemDescriptionText;

		[Header("Timing")]
		[SerializeField]
		private float displayDuration = 3f;

		[SerializeField]
		private float delayBetweenNotifications = 0.3f;

		private Queue<(string name, Sprite icon, string description, Color nameColor)> notificationQueue = new Queue<(string, Sprite, string, Color)>();

		private bool isShowingNotification;

		private Coroutine activeCoroutine;

		private void Awake()
		{
			if (animator == null)
			{
				animator = GetComponent<Animator>();
			}
			animator.updateMode = AnimatorUpdateMode.UnscaledTime;
			animator.Play("HiddenPanel", 0, 0f);
		}

		private void Start()
		{
			if (ItemUnlockManager.Instance != null)
			{
				SubscribeToEvents();
			}
			else
			{
				Invoke("TrySubscribe", 0.1f);
			}
		}

		private void TrySubscribe()
		{
			if (ItemUnlockManager.Instance != null)
			{
				SubscribeToEvents();
			}
		}

		private void SubscribeToEvents()
		{
			ItemUnlockManager.Instance.OnItemUnlocked.AddListener(OnItemUnlocked);
			ItemUnlockManager.Instance.OnClassUnlocked.AddListener(OnClassUnlocked);
		}

		private void OnDestroy()
		{
			if (ItemUnlockManager.Instance != null)
			{
				ItemUnlockManager.Instance.OnItemUnlocked.RemoveListener(OnItemUnlocked);
				ItemUnlockManager.Instance.OnClassUnlocked.RemoveListener(OnClassUnlocked);
			}
		}

		private void OnItemUnlocked(InventoryItem item)
		{
			notificationQueue.Enqueue((item.itemName, item.itemIcon, item.itemDescription, item.GetRarityColor()));
			if (!isShowingNotification)
			{
				activeCoroutine = StartCoroutine(ProcessNotificationQueue());
			}
		}

		private void OnClassUnlocked(PlayerClass playerClass)
		{
			notificationQueue.Enqueue((playerClass.className, playerClass.classIcon, playerClass.classDescription, Color.white));
			if (!isShowingNotification)
			{
				activeCoroutine = StartCoroutine(ProcessNotificationQueue());
			}
		}

		private IEnumerator ProcessNotificationQueue()
		{
			isShowingNotification = true;
			while (notificationQueue.Count > 0)
			{
				(string, Sprite, string, Color) tuple = notificationQueue.Dequeue();
				PopulatePanel(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
				animator.Play("ShowingPanel");
				yield return WaitForAnimationToEnd("ShowPanel");
				yield return new WaitForSecondsRealtime(displayDuration);
				animator.Play("HidingPanel");
				yield return WaitForAnimationToEnd("HidingPanel");
				if (notificationQueue.Count > 0)
				{
					yield return new WaitForSecondsRealtime(delayBetweenNotifications);
				}
			}
			isShowingNotification = false;
			activeCoroutine = null;
		}

		private IEnumerator WaitForAnimationToEnd(string stateName)
		{
			yield return null;
			while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
			{
				yield return null;
			}
		}

		private void PopulatePanel(string name, Sprite icon, string description, Color nameColor)
		{
			if (itemNameText != null)
			{
				itemNameText.text = name;
				itemNameText.color = nameColor;
			}
			if (itemIconImage != null)
			{
				if (icon != null)
				{
					itemIconImage.sprite = icon;
					itemIconImage.enabled = true;
				}
				else
				{
					itemIconImage.enabled = false;
				}
			}
			if (itemDescriptionText != null)
			{
				itemDescriptionText.text = description;
			}
		}
	}
}
