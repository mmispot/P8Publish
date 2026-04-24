using UnityEngine;

[CreateAssetMenu(fileName = "Inv Items", menuName = "ScriptableObjects/ItemData", order = 1)]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite sprite;
    public Vector2Int[] shape;
    public int width, height;

}
