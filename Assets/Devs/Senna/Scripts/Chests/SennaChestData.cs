using UnityEngine;

// Chest definition asset: name, prompt and contents. One asset per chest
// type — drop it on a SennaChest in the scene. Never mutated in play mode;
// opened-state lives on the SennaChest instance.
[CreateAssetMenu(menuName = "Senna/Chest")]
public class SennaChestData : ScriptableObject
{
    public string chestName;

    // Shown by SennaPromptUI; falls back to "[F] Open <chestName>" when empty.
    public string promptText;

    // When true the chest can only be opened once (one-shot loot chest).
    // Grid chests must stay reopenable, so leave this off for them.
    public bool openOnce;

    // Contents handed out on open (granting comes with the inventory integration).
    public ItemData[] lootItems;
}
