# Session: Player health bar + damage feedback (branch Player/HealthSystem)

## Plan
- [x] `SennaPlayerHealth.ResetHealth()` — fire `onHealed` so listeners refresh on respawn
- [x] `SennaCameraShake` — skip shake while `Time.deltaTime == 0` (prevents camera drift when dying with active trauma, since timeScale freezes on death)
- [x] `Scripts/Player/SennaHealthBarUI.cs` — fill-image health bar, polls health each frame, smooth lerp
- [x] `Scripts/Player/SennaDamageFeedback.cs` — listens to `onDamaged`, triggers `SennaCameraShake.TriggerShake` + red full-screen flash that fades out (unscaled time)
- [x] `Editor/HealthHUDSetup.cs` — `Tools > Senna > Setup Health HUD` builds HealthBar (bottom-left) + DamageFlash under UICanvas, auto-wires refs, idempotent
- [ ] In Unity (Main Scene open): run `Tools > Senna > Setup Health HUD`
- [ ] Apply UICanvas overrides to `Final prefab/UICanvas.prefab`
- [ ] Play test: take damage → bar drops + shake + red flash; die → death panel; respawn → bar refills

## Affected assets
- Scripts: SennaPlayerHealth.cs (1 line), SennaCameraShake.cs (1 line), 2 new runtime scripts, 1 new editor script
- Scene/prefab: Main Scene (UICanvas instance gets HealthBar + DamageFlash children) → apply to UICanvas.prefab

## Design notes
- Art-open: bar = plain Background + Fill `Image`s (Fill is type Filled/Horizontal). Swap real art by assigning sprites in the inspector — no code change.
- Health bar polls `CurrentHealth/MaxHealth` instead of listening to events because the player object starts inactive until Start is pressed; polling avoids init-order issues. Damage flash/shake use the `onDamaged` event since they must react to damage only.
- UI fades/lerps use `Time.unscaledDeltaTime` so they behave while timeScale is 0 (death/pause).

## Review
- Code complete; Unity-side steps (run setup tool, apply prefab overrides, play test) still open — they need the editor.
