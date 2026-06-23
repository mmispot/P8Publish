using UnityEngine;

[CreateAssetMenu(fileName = "GunData", menuName = "P8/Gun Data")]
public class SennaGunData : ScriptableObject
{
    [SerializeField] public string displayName = "Gun";
    [SerializeField] public Sprite icon;
}
