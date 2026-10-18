using System;
using UnityEngine;

[Serializable]
public class SECTR_ULong
{
	[SerializeField]
	private int first;

	[SerializeField]
	private int second;

	public ulong value
	{
		get
		{
			long num = (long)second << 32;
			ulong num2 = (ulong)first;
			return (ulong)num | num2;
		}
		set
		{
			first = (int)(value & 0xFFFFFFFFu);
			second = (int)(value >> 32);
		}
	}

	public SECTR_ULong(ulong newValue)
	{
		value = newValue;
	}

	public SECTR_ULong()
	{
		value = 0uL;
	}

	public override string ToString()
	{
		return $"[ULong: value={value}, firstHalf={first}, secondHalf={second}]";
	}

	public static bool operator >(SECTR_ULong a, ulong b)
	{
		return a.value > b;
	}

	public static bool operator >(ulong a, SECTR_ULong b)
	{
		return a > b.value;
	}

	public static bool operator <(SECTR_ULong a, ulong b)
	{
		return a.value < b;
	}

	public static bool operator <(ulong a, SECTR_ULong b)
	{
		return a < b.value;
	}
}
