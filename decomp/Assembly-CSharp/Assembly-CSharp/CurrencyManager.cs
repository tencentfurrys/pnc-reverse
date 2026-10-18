using UnityEngine;
using UnityEngine.Events;

public class CurrencyManager : MonoBehaviour
{
	[Header("Currency Settings")]
	[SerializeField]
	private int currentCurrency;

	[SerializeField]
	private string currencyPrefsKey = "PlayerCurrency";

	[Header("Events")]
	public UnityEvent<int> OnCurrencyChanged;

	public UnityEvent<int> OnCurrencyGained;

	public UnityEvent<int> OnCurrencySpent;

	public UnityEvent OnInsufficientFunds;

	public static CurrencyManager Instance { get; private set; }

	public int CurrentCurrency => currentCurrency;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			Object.DontDestroyOnLoad(gameObject);
			LoadCurrency();
		}
		else
		{
			Object.Destroy(gameObject);
		}
	}

	private void Start()
	{
		OnCurrencyChanged?.Invoke(currentCurrency);
	}

	public void AddCurrency(int amount)
	{
		if (amount > 0)
		{
			currentCurrency += amount;
			SaveCurrency();
			OnCurrencyGained?.Invoke(amount);
			OnCurrencyChanged?.Invoke(currentCurrency);
		}
	}

	public bool SpendCurrency(int amount)
	{
		if (amount <= 0)
		{
			return false;
		}
		if (!CanAfford(amount))
		{
			OnInsufficientFunds?.Invoke();
			return false;
		}
		currentCurrency -= amount;
		SaveCurrency();
		OnCurrencySpent?.Invoke(amount);
		OnCurrencyChanged?.Invoke(currentCurrency);
		return true;
	}

	public void SetCurrency(int amount)
	{
		amount = Mathf.Max(0, amount);
		_ = currentCurrency;
		currentCurrency = amount;
		SaveCurrency();
		OnCurrencyChanged?.Invoke(currentCurrency);
	}

	public bool CanAfford(int amount)
	{
		return currentCurrency >= amount;
	}

	public int GetCurrency()
	{
		return currentCurrency;
	}

	private void SaveCurrency()
	{
		PlayerPrefs.SetInt(currencyPrefsKey, currentCurrency);
		PlayerPrefs.Save();
	}

	private void LoadCurrency()
	{
		currentCurrency = PlayerPrefs.GetInt(currencyPrefsKey, 0);
	}

	public void ResetCurrency()
	{
		currentCurrency = 0;
		SaveCurrency();
		OnCurrencyChanged?.Invoke(currentCurrency);
	}

	public void SetCurrencyPrefsKey(string newKey)
	{
		if (!string.IsNullOrEmpty(newKey))
		{
			currencyPrefsKey = newKey;
			LoadCurrency();
		}
	}

	public string GetCurrencyString()
	{
		return $"{currentCurrency:N0}";
	}

	public string GetCurrencyString(string prefix, string suffix = "")
	{
		return $"{prefix}{currentCurrency:N0}{suffix}";
	}

	[ContextMenu("Add 100 Currency")]
	private void Debug_Add100Currency()
	{
		AddCurrency(100);
	}

	[ContextMenu("Spend 50 Currency")]
	private void Debug_Spend50Currency()
	{
		SpendCurrency(50);
	}

	[ContextMenu("Reset Currency")]
	private void Debug_ResetCurrency()
	{
		ResetCurrency();
	}
}
