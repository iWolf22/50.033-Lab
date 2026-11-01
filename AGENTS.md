# Mario lab workflow

## User preferences

- Keep replies, progress updates and questions concise and direct.
- Make minimal, clean changes using Unity best practices; follow the requested tutorial section and skip temporary work that later sections remove.
- Complete authorized work without routine confirmations; preserve user layouts and Inspector settings. Use Git for recovery; do not create backup copies.
- Keep block hierarchies flat: root → Block visual + the selected powerup. Group assets by purpose; reuse interactive prefabs across scenes (including Mario and Canvas). Decorative objects need not be prefabs. Keep sounds in one scene-local Scene Audio prefab.
- Teens uses only mushroom growth, Fire Flower/fireballs and a timed star buff; avoid extra powerups or scoring effects. Earlier log-only collection behavior is superseded. Small + mushroom/flower → Big; Big + flower → Fire. Damage downgrades Fire → Big → protected Small → Small → Dead; keep two seconds of damage protection.
- Group artifacts by lab and purpose; keep showcase links working when moving files.
- Keep menus as simple panels in the existing Canvas, styled like Game Over over World-1-1. Include Home during play and Menu after death; loading is simulated.
- Match menu icon pixel art and spacing; dim the scene behind active menus. Verify menu and gameplay layouts with screenshots.
- Respect code-only requests; explain the Inspector steps needed to hook up changes.
- Add new preferences and reusable Unity development/testing/debugging insights here as they arise. Use short bullets, merge duplicates and remove stale guidance to save tokens.

## Scope

- This is a 50.033 lab project. Keep changes minimal and understandable for the student.
- Read `README.md` and `CHECKOFF.md` for architecture, tutorial links and verification. Current work covers Teens/Lab 5; keep later additions within the requested checkoff scope.
- Use existing components, Inspector references, UnityEvents and the Input System. Preserve `.meta` GUIDs and serialized bindings when renaming/refactoring.
- Group runtime scripts by responsibility under `Assets/Scripts`; move each script with its `.meta` and keep the gameplay assembly definition at that root.

## Runtime contracts

- Scene-local `GameManager` updates `GameScore` and raises ScriptableObject lifecycle/score channels. Inspect `GameEventListener.Response` bindings on shared prefabs when behavior is disconnected.
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
- No gameplay object persists or uses a singleton. Shared ScriptableObject data carries score/session state; event listeners register/unregister in OnEnable/OnDisable. SceneAudio is recreated with each scene.
- `RenderingDebugSetup` disables URP's optional runtime debugger before scene load so its persistent debug helper does not appear in the Hierarchy.
- Keep consumed powerups and broken bricks available for in-place restart; cancel powerup coroutines and disable physics before restoring their spawn positions.
- Unity's test runner also creates persistent objects; persistence tests should count gameplay objects rather than every DontDestroyOnLoad root.
- Input action GUID strings must use the canonical hyphenated format: PlayerInput resolves serialized ActionEvents by string ID. Regenerate the wrapper after changes.
- Validate recordings separately from realtime UI coroutines: frame-based capture can advance gameplay faster than WaitForSecondsRealtime.
- Gameplay tests live in `Assets/Tests/PlayMode`. Run Unity with `-batchmode -projectPath <copy> -runTests -testPlatform PlayMode -testResults <xml> -logFile <log>`; omit `-quit` for the test runner.
- Editor utilities use `-batchmode -quit -projectPath <copy> -executeMethod <method> -logFile <log>`. Run `LabLevelPreview.Capture` in the copy because it manipulates the scene to export images.
- Run checks appropriate to the change; documentation-only edits need no Unity run. Report actual results. Existing recordings in `Artifacts/Lab3/` demonstrate an older layout.
- Keep `Artifacts/Lab3/RecordingTools` outside gameplay and temporary scripts inside the validation copy. Update hashes/results only after verifying final assets.

## Git

- This is a linked worktree; check the branch. In WSL, translate the Windows path in `.git` and use `--git-dir` / `--work-tree` if normal Git fails. Do not rewrite `.git` or initialize another repository.
- Git metadata lives outside this workspace; request tool escalation when necessary. Keep Git LFS enabled for existing binary assets.
- When asked to commit, group related changes in a sensible order. Preserve the final project contents; intermediate commits need not compile if the user permits that. Push only when requested.
