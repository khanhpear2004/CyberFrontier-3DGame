# Repository Guidelines

## Project Structure & Module Organization

This is a Unity 6 project (Editor `6000.0.44f1`). Gameplay code belongs in `Assets/Scripts/`, grouped by feature: `Combat/`, `GunLogic/`, `Skills/`, and `UI/`. Project-owned scenes are in `Assets/Scenes/`; `SampleScene.unity` is currently the only enabled build scene. Shared materials, prefabs, animations, and generated models live in their matching `Assets/` folders. Treat folders such as `StarterAssets/`, `EffectCore/`, `_DLNK/`, and vendor-named directories as third-party content; avoid modifying them unless the change specifically targets that package. Package dependencies and Unity-wide configuration are tracked in `Packages/` and `ProjectSettings/`.

## Build, Test, and Development Commands

- Open the repository with Unity Hub using Unity `6000.0.44f1`, then press Play for local development.
- `"C:\Program Files\Unity\Hub\Editor\6000.0.44f1\Editor\Unity.exe" -projectPath .` opens the project directly when Unity is installed at the default location.
- Build through **File > Build Profiles**; verify the intended scenes in Build Settings before building.
- Run tests from **Window > General > Test Runner**, or use `Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults TestResults.xml -quit` for CI.

Do not commit generated `Library/`, `Temp/`, `Logs/`, `obj/`, or `Builds/` directories.

## Coding Style & Naming Conventions

Use C# with four-space indentation and braces on separate lines. Name classes and methods in `PascalCase`; use `camelCase` for fields and parameters, matching the existing gameplay scripts. Keep each `MonoBehaviour` or `ScriptableObject` in a file with the same name (for example, `PlayerEnergy.cs`). Prefer `[SerializeField] private` for Inspector configuration over adding new public mutable fields. Keep editor-only utilities under an `Editor/` directory.

## Testing Guidelines

The Unity Test Framework is installed, but the repository currently has no automated tests. Add Edit Mode tests for pure logic and Play Mode tests for scene/component behavior under `Assets/Tests/EditMode/` and `Assets/Tests/PlayMode/`. Name fixtures `FeatureNameTests` and tests after behavior, such as `SpendEnergy_InsufficientEnergy_ReturnsFalse`. Manually smoke-test affected scenes and record the scene used in the pull request.

## Commit & Pull Request Guidelines

Recent commits use concise, task-focused Vietnamese descriptions. Follow that style: one imperative summary per commit, such as `thêm thanh năng lượng cho HUD`; avoid vague messages like `update`. Pull requests should explain gameplay impact, list tested scenes and steps, link relevant issues, and include screenshots or short clips for visual changes. Commit Unity `.meta` files alongside their assets. Git LFS tracks `.png`, `.tif`, and `.asset` files, so ensure LFS is installed before committing binary assets.
