using UnityEngine;

/// <summary>
/// Used as data storage for Accessory data. This script is added to any child object
/// of the Character Prefab (see in Bundles/Characters for sample characters and their accessories).
/// </summary>
public class CharacterAccessories : MonoBehaviour
{
    public string accessoryName;
    public int cost;
	public int premiumCost;
	public Sprite accessoryIcon;
}
