# Todo — Gun Ammo System (GunReload scene)

## Task
Add an ammo system to the gun: track bullets, consume one per shot, block firing when the mag is
empty. Built to connect to the inventory/item system later.

## Plan (v1 — ammo count only, magazine + reserve model)
- [x] Create `Assets/Devs/Senna/Scripts/Player/SennaAmmoSystem.cs` (MonoBehaviour, no namespace).
- [x] Add optional `SennaAmmoSystem ammo` reference + ammo gate in `SchootingRaycast.Shoot()`.
- [ ] Unity Editor (manual, user): add `SennaAmmoSystem` to the gun in `GunReload.unity`, set
      `magazineSize`/`reserveAmmo`, drag it into `SchootingRaycast.ammo`.
- [ ] Play test (see Verification).

## Changed files
- `Assets/Devs/Senna/Scripts/Player/SennaAmmoSystem.cs` (new) — mag + reserve state, `TryConsume()`,
  `Reload()` and `AddReserve()` stubs for later, `onAmmoChanged` UnityEvent, inventory-integration TODO region.
- `Assets/Devs/Dominik/Old scripts/SchootingRaycast.cs` — added `[SerializeField] private SennaAmmoSystem ammo`
  and one gate line in `Shoot()`. Backward-compatible: null ammo = fires exactly as before.

## Verification (manual — no test asmdef in project)
- In `GunReload.unity`: fire `magazineSize` times → `currentInMag` decrements each shot; the next pull
  at 0 → no shot / no "Shot fired" console line. Tracer + feel still play while ammo remains.
- A gun/scene without a `SennaAmmoSystem` assigned still fires normally.
- No new Console warnings/null-refs.

## Open / future (by user's choice)
- Reload input binding + animation/sound (call `Reload()`).
- Inventory link: `ItemType.Ammo`, feed reserve from inventory stacks via `AddReserve` (TODO region in script).
- Ammo HUD subscribing to `onAmmoChanged`.
