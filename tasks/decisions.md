# Decisions (ADR-lite)

## 2026-06-10 — Health bar polls instead of subscribing to health events
`GameStateManager.Awake` deactivates the player until Start is pressed, so `SennaPlayerHealth.Awake` (which sets `currentHealth = maxHealth`) runs after the UI is already enabled. Event-driven UI would show a stale/empty bar until the first damage. Polling two floats per frame is allocation-free and immune to init order. Event listeners (`onDamaged`) are still used where reacting to the *event* matters: camera shake + damage flash in `SennaDamageFeedback`.

## 2026-06-10 — Camera shake skips frames where deltaTime == 0
`SennaCameraShake` adds offsets every `LateUpdate` and relies on `SennaPlayerMovement`'s rest-position lerp to pull the camera back. With `Time.timeScale = 0` (death/pause) that lerp stops but `LateUpdate` keeps running, so an active shake would drift the camera unboundedly. Guard: early-out when `Time.deltaTime <= 0`.

## 2026-06-10 — Damage feedback lives on UICanvas, not the player
The flash image is a canvas child, and the player prefab can't hold a serialized reference into the canvas prefab. Putting `SennaDamageFeedback` next to the flash image keeps the cross-prefab wiring (player health, camera shake) at scene level, done automatically by `Tools > Senna > Setup Health HUD`.
