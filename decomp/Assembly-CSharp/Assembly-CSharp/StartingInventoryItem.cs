using System;
using PixelCrushers.GridController;
using UnityEngine;

[Serializable]
public class StartingInventoryItem
{
	[Tooltip("The item to add to inventory")]
	public InventoryItem item;

	[Tooltip("Grid position where item should be placed (x: 0-7, y: 0-3)")]
	public Vector2Int gridPosition;

	[Tooltip("If true and position is occupied, find next available slot")]
	public bool autoPlaceIfOccupied = true;
}
