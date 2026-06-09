using UnityEngine;

[CreateAssetMenu]

public class ItemData : ScriptableObject
{
    public int width = 1;
    public int height = 1;

    public string itemName;

    public Sprite itemIcon;
    public ItemType itemType;

    public enum ItemType 
    {
        PrimaryWeapon,
        SecondaryWeapon,
        Throwable,
        Knife,
        VestArmor,
        Other
    }
}
