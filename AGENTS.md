# Mario lab workflow

## User preferences

- Keep replies, progress updates and questions concise and direct.
- Make minimal, clean changes using Unity best practices; follow the requested tutorial section and skip temporary work that later sections remove.
- Complete authorized work without routine confirmations; preserve user layouts and Inspector settings. Use Git for recovery; do not create backup copies.
- Keep block hierarchies flat: root → Block visual + the selected powerup. Group assets by purpose; reuse interactive prefabs across scenes (including Mario and Canvas). Decorative objects need not be prefabs. Keep sounds in one scene-local Scene Audio prefab.
- Mushroom and star collection only log to the Console; do not add growth, invincibility, score or other effects unless requested.
- Respect code-only requests; explain the Inspector steps needed to hook up changes.
- Add new preferences and reusable Unity development/testing/debugging insights here as they arise. Use short bullets, merge duplicates and remove stale guidance to save tokens.

## Scope

- This is a 50.033 lab project. Keep changes minimal and understandable for the student.
- Read `README.md` and `CHECKOFF.md` for architecture, tutorial links and verification. Coverage ends at Toddlers; implement Children/Lab 4 or later work only when requested.
- Use existing components, Inspector references, UnityEvents and the Input System. Preserve `.meta` GUIDs and serialized bindings when renaming/refactoring.
- Group runtime scripts by responsibility under `Assets/Scripts`; move each script with its `.meta` and keep the gameplay assembly definition at that root.

## Runtime contracts

- `GameManager` owns score and `gameStart`, `gameRestart`, `scoreChange`, `gameOver`. Inspect scene UnityEvent bindings to `HUDManager` and reset methods when behavior is disconnected.
- Restart resets Mario/input/animation, camera, inactive enemies, spent blocks/coins, finish trigger, music, pause UI and powerups in place. Include new gameplay state in this reset.
- `EnemyManager` observes the C# event `EnemyMovement.OnGoombaStomped`, subscribes/unsubscribes in OnEnable/OnDisable, awards one point and plays stomp audio. Retain inactive enemies for restart.
- `CoinPowerup` invokes `AnimationEventIntTool.useInt` after its pop coroutine; `BlockBounce` binds it to `GameManager.IncreaseScore`. Rewards are single-use until restart.
- `ActionManager` bridges `PlayerInput` to UnityEvents. Preserve stopping/reversing and tap/held jump. Regenerate `MarioActions.cs` from `.inputactions` when changing actions.
- Mario's animation clips call `PlayJumpSound`, `PlayDeathImpulse` and `GameOverScene`. Renaming these methods requires updating clip events. Runtime scripts belong to `Mario.Gameplay`; serialized event targets must reference the correct assembly.

## Protect the user's scene

- `Assets/Scenes/World-1-1.unity` and `World-1-2.unity` are the working scenes. User edits to object positions, UI and Inspector settings take priority over the builder's layout.
- Prefer targeted edits. Never copy a generated validation scene over the working scene merely to apply script changes.
- `Tools → Lab → Rebuild Mario Course` / `LabLevelBuilder.Build` replaces the layout. Run it only when that replacement is explicitly requested.
- Never close the user's Unity editor, kill its process or delete its lock files to run validation.
- Generated layouts can overwrite user placements; verify saved scene transforms and serialized bindings after hierarchy or prefab changes.

## Course and asset details

- Preserve the short course: all block types, Goombas, pipe, three-unit pit, bouncy cloud, one-way platform, spring block and finish steps. Keep the original red ending UI; omit the controls banner.
- The camera stays at its initial position when Mario moves left; place the left wall at the viewport edge. Rightward following stops at the course end.
- Pipe sprites have different native widths: `pipe-top` includes the rim and upper body, while `pipe-mid` is narrower. Match sprite bounds instead of stretching both to identical rectangles.
- `Assets/InputActions/SuperMarioBros.mixer` routes gameplay audio. Special Event SFX sends duck Background Music; Enemies has a 1600 Hz lowpass. Preserve routing when adding or moving AudioSources.

## Work and validate

- Inspect the working tree first and preserve unrelated edits.
- Unity is **6000.3.24f1**, URP 2D. On this machine the Windows editor is under `C:\Program Files\Unity\Hub\Editor\6000.3.24f1\Editor\Unity.exe`.
- If the main project is open, validate in ignored `.unity-validation/`. Refresh relevant assets/sources first; the copy may contain stale files or temporary tools.
- Only GameManager persists; pair scene event subscriptions with unsubscriptions. SceneAudio provides shared sources and is recreated with each scene.
- Keep consumed powerups and broken bricks available for in-place restart; cancel powerup coroutines and disable physics before restoring their spawn positions.
- Unity's test runner also creates persistent objects; persistence tests should count gameplay objects rather than every DontDestroyOnLoad root.
- Gameplay tests live in `Assets/Tests/PlayMode`. Run Unity with `-batchmode -projectPath <copy> -runTests -testPlatform PlayMode -testResults <xml> -logFile <log>`; omit `-quit` for the test runner.
- Editor utilities use `-batchmode -quit -projectPath <copy> -executeMethod <method> -logFile <log>`. Run `LabLevelPreview.Capture` in the copy because it manipulates the scene to export images.
- Run checks appropriate to the change; documentation-only edits need no Unity run. Report actual results. Existing recordings in `Artifacts/` demonstrate an older layout.
- Keep `Artifacts/RecordingTools` outside gameplay and temporary scripts inside the validation copy. Update hashes/results only after verifying final assets.

## Git

- This is a linked worktree; check the branch. In WSL, translate the Windows path in `.git` and use `--git-dir` / `--work-tree` if normal Git fails. Do not rewrite `.git` or initialize another repository.
- Git metadata lives outside this workspace; request tool escalation when necessary. Keep Git LFS enabled for existing binary assets.
- When asked to commit, group related changes in a sensible order. Preserve the final project contents; intermediate commits need not compile if the user permits that. Push only when requested.
