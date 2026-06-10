# Lessons

Project-specific corrections. Newest at the bottom. These override CLAUDE.md defaults.

- Project conventions (observed, not corrections): no namespaces, scripts prefixed `Senna`, no asmdefs, UI built via editor tools in `Assets/Devs/Senna/Editor`, Unity 6000.3.11f1 + URP + Input System.
- Scene work happens on feature branches (e.g. `Player/HealthSystem`); keep `.unity` changes minimal and put UI in `UICanvas.prefab` via applied overrides to avoid scene merge conflicts.
