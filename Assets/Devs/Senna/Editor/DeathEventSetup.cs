using UnityEngine;
using UnityEditor;
using UnityEditor.Events;

// One-click idempotent re-wire of the player's death event to the death screen.
// Adds SennaPlayerHealth.onDeath -> GameStateManager.OnPlayerDied as a persistent
// listener in the active scene. Safe to run repeatedly — skips if already wired.
public static class DeathEventSetup
{
    [MenuItem("Tools/Senna/Wire Death Screen")]
    public static void WireDeathScreen()
    {
        var health = Object.FindFirstObjectByType<SennaPlayerHealth>(FindObjectsInactive.Include);
        if (health == null)
        {
            Debug.LogError("[DeathEventSetup] No SennaPlayerHealth in the scene. Open MainScene first.");
            return;
        }

        var gsm = Object.FindFirstObjectByType<GameStateManager>(FindObjectsInactive.Include);
        if (gsm == null)
        {
            Debug.LogError("[DeathEventSetup] No GameStateManager in the scene.");
            return;
        }

        // Skip if onDeath already calls OnPlayerDied on this GameStateManager.
        int count = health.onDeath.GetPersistentEventCount();
        for (int i = 0; i < count; i++)
        {
            if (health.onDeath.GetPersistentTarget(i) == gsm &&
                health.onDeath.GetPersistentMethodName(i) == nameof(GameStateManager.OnPlayerDied))
            {
                Debug.Log("[DeathEventSetup] Death screen already wired — nothing to do.");
                return;
            }
        }

        UnityEventTools.AddPersistentListener(health.onDeath, gsm.OnPlayerDied);

        EditorUtility.SetDirty(health);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log($"[DeathEventSetup] Wired {health.name}.onDeath -> {gsm.name}.OnPlayerDied. Save the scene (Ctrl+S).");
    }
}
