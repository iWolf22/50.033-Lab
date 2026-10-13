# Lab coverage and verification

Source guides:

- [Children: C# Quick Tour](https://natalieagus.github.io/50033/docs/children/static/)

- [Newborns: installation](https://natalieagus.github.io/50033/docs/newborns/installation/), [Mario](https://natalieagus.github.io/50033/docs/newborns/basics/), [UI](https://natalieagus.github.io/50033/docs/newborns/ui/), [checkoff](https://natalieagus.github.io/50033/docs/newborns/checkoff/)
- [Babies: animation](https://natalieagus.github.io/50033/docs/babies/animation/), [camera](https://natalieagus.github.io/50033/docs/babies/camera/), [physics](https://natalieagus.github.io/50033/docs/babies/physics/), [checkoff](https://natalieagus.github.io/50033/docs/babies/checkoff/)
- [Toddlers: input](https://natalieagus.github.io/50033/docs/toddlers/management/), [observer](https://natalieagus.github.io/50033/docs/toddlers/observer-pattern/), [audio](https://natalieagus.github.io/50033/docs/toddlers/audio-management/), [checkoff](https://natalieagus.github.io/50033/docs/toddlers/checkoff/)

## Saved course

Compact course from spawn x = -5 to finish x = 20 with flat terrain, one 3-unit pit, a corrected pipe, plain/coin/question blocks, two patrols, a one-way platform, a spring question block, a bouncy cloud, short finish steps, flagpole and castle. Ground extends left to support the camera-edge boundary at different aspect ratios. The original red Game Over title, score and restart layout are retained; the top controls text is removed. Background hills, bushes and clouds use the supplied sprites. No gameplay objects are generated at startup.

## Starter scope

The essential setup, movement/UI, animation/camera/Physics2D, Input System, observer and audio work through Toddlers is included. Unity 6.3, the sprite/Input System/editor packages, Force Text serialization and the existing folder structure are configured; personal IDE preferences remain yours.

`GameManager` exposes the tutorial's four UnityEvents. The saved scene binds them to `HUDManager`, `EnemyManager`, player, camera, blocks, finish trigger and music. Enemy observers subscribe in OnEnable and unsubscribe in OnDisable. Coin rewards use `AnimationEventIntTool` at completion of the existing coin animation. Restart restores objects in place, including inactive enemies and spent spring blocks. Prefab reward targets are bound to the scene's manager when the editor builder places instances.

Optional mouse-position diagnostics and standalone event demonstrations are omitted. The Children C# Quick Tour is implemented, including interactive buttons and powerups. ScriptableObjects remain outside this change.

## Requirements and evidence

| Requirement | Implementation | Evidence |
| --- | --- | --- |
| Dynamic player, colliders, bounded movement, stop, jump and facing | PlayerMovement; Rigidbody2D, contact-filter ground probe | KeyboardMovesReversesAndStops; TapAndHoldJumpHaveDifferentHeightsWithoutDoubleJump — passed |
| Ground and obstacle layout, hierarchy, reusable prefabs | Editor course builder; tiled renderers and collider sizes | Saved scene/prefabs; exported overview and section previews inspected |
| Game over with score and restart | GameManager lifecycle UnityEvents; HUDManager; existing Canvas/buttons | SideHitDiesThenRestartRestoresCourse — passed |
| Idle/run/skid/jump/death animation | Mario Animator; animation event callbacks | Animator transitions inspected and repaired; standing, running jump and skid states wired |
| Jump audio once; death hop then result screen | PlayJumpSound, PlayDeathImpulse, GameOverScene clip events | Death/restart test passed; animation events inspected; mixed audio captured in the review clips |
| Camera follows between bounds | CameraController.LateUpdate | BouncyCloudReboundsAndCameraStopsAtCourseEnds — passed at 4:3, 16:9 and 21:9 |
| One-way platform | EdgeCollider2D and PlatformEffector2D | OneWayPlatformPassesUpwardAndSupportsLanding — passed |
| Spring question block | SpringJoint2D, fixed top/side rails, dynamic underside, spent body becomes Static | SpringQuestionBlockIsStableOnTopAndLocksAfterReward — passed |
| Bouncy cloud | PhysicsMaterial2D | BouncyCloudReboundsAndCameraStopsAtCourseEnds — passed |
| Question block underside only, blink, coin animation/sound, spent sprite, no second bounce | BlockBounce and question prefab | QuestionBlockRejectsTopHitAndDispensesOnlyOnceFromBelow — passed, including return inside the box |
| Bricks with/without coin, underside bounce, no breaking | Two brick prefabs using BlockBounce | BrickVariantsBounceWithoutBreakingAndCoinIsSingleUse — passed; AudioSource playing checked |
| Input System meaningful use, tap/hold jump | MarioActions, PlayerInput, ActionManager UnityEvents | Keyboard scheme, Value Axis, tap/hold actions and UnityEvents inspected; input tests passed |
| Observer events/delegates; animated stomp increments score; jump-over disabled | EnemyMovement.Action event; EnemyManager subscribes/unsubscribes; obsolete jump-over script removed | StompAnimatesScoresOnceAndBounces; JumpingOverGoombaDoesNotScore — passed |
| Coin score, HUD updates and event-driven restart without scene reload | AnimationEventIntTool → GameManager.IncreaseScore; scoreChange → HUDManager; gameRestart → component resets | ManagerEventsRestoreSpentBlocksAndInactiveEnemiesWithoutReloading — passed, including repeated observer enable/disable |
| AudioMixer groups and death music ducking | Every AudioSource routed; Special Event SFX Send → Background Music Duck Volume | All source groups and Send/Duck routing inspected; mixed audio captured in the review clips |
| Additional meaningful mixer effect | Enemies Lowpass: 1600 Hz, resonance 1; softens Goomba impact while coin/jump cues stay bright | Mixer asset has Enemies Lowpass at 1600 Hz / resonance 1; mixed audio captured in the review clips |
| Nice semi-full level with fewer slopes | Course layout described above | Previews inspected; CourseCanBeCompletedWithKeyboardMovementAndJumps — passed without teleporting |

## Children: C# Quick Tour

Only `GameManager : Singleton<GameManager>` persists. Scene components subscribe/unsubscribe to its UnityEvents in OnEnable/OnDisable; rewards and restart buttons use `GameManager.instance`. Scene changes restore gameplay UI and publish the carried score. Scene audio stays outside the persistent manager.

The short World-1-2 demo has flat ground, one Goomba, clouds, a hill, bushes and a decorative castle with no outgoing transition. Open `Assets/Scenes/World-1-2.unity` directly or enter the castle in World-1-1.

**10 selected Unity PlayMode tests passed, 0 failed**: castle transition and score carryover, World-1-2 standalone scoring, World-1-2 restart including inactive Goomba/music, and seven movement/jump/stomp/coin/death regressions. Tests ran in `.unity-validation/` using the final script sources and the same demo scene installed in this project. The broader historical course tests were not rerun. [Results](Artifacts/Lab4/Verification/static-playmode-results.xml), [demo preview](Artifacts/Lab4/Previews/world-1-2-demo.png).

`IInteractiveButton` defines `ButtonClick`; restart controllers retain their original script GUID and pause controllers switch between matching pause/play sprites. Both scenes place pause beside restart. Restart clears pause state and the icon; hidden pause controls cannot resume Game Over.

**4 Unity PlayMode tests passed, 0 failed** after the button change: pause/resume and restart in both worlds, castle score carryover, World-1-2 restart/scoring and standalone World-1-2. [Results](Artifacts/Lab4/Verification/buttons-playmode-results.xml). Icons adapt Colorosse’s [CC0 Sandbox Menu Icons](https://colorosse.com/assets/2d/ui/sandbox-menu-icons-16); SVG sources and provenance are in `Artifacts/AssetSources/ButtonIcons/`.

The remaining [C# Quick Tour](https://natalieagus.github.io/50033/docs/children/static/) uses `IPowerup` / `IPowerupApplicable`, `PowerupType`, abstract `BasePowerup`, and coin/mushroom/star implementations. The mushroom question box extends the existing spring prefab; question and brick controllers share `BlockBounce`. Coins keep the original scoring event. Mushrooms and stars share emergence, movement and wall reversal. Collection only logs `mushroom collected` / `star collected`; neither changes Mario, grants invincibility, scores or plays a collection sound. Restart cancels spawns and restores consumed powerups. OneUpMushroom remains an enum entry.

Mushroom boxes sit at x = -3 in World-1-1 and x = -2 in World-1-2; star boxes sit at x = 2.5 and x = 0 respectively. Existing scene transforms and UI positions were preserved.

**Before the log-only change, 11 Unity PlayMode tests passed, 0 failed**: mushroom spawning/collection/wall reversal, coin scoring, brick breaking and restart, restart during emergence, spring/top-hit behavior, repeated observer reset, pause/restart and scene persistence. Tested in `.unity-validation/` with matching final scripts, prefabs and scenes. [Results](Artifacts/Lab4/Verification/powerups-playmode-results.xml).

**12 Unity PlayMode tests passed, 0 failed** after the log-only change: mushroom/star collection logs once without changing Mario or score, star boxes in both worlds, shared physics, restart during spawning, coin/block behavior, pause and scene persistence. Final assets match the isolated validation copy. [Results](Artifacts/Lab4/Verification/log-only-powerups-results.xml).

## Shared prefabs and hierarchy cleanup

Blocks now have a flat root with `Block visual` and the selected powerup. Mushroom/star blocks no longer contain an unused coin or question wrapper. Their spring rails and moving underside remain intact. Prefabs are grouped by responsibility and reused in both worlds, including Mario, Canvas, EventSystem, GameManager, camera, ground, left boundary and the enemy manager. Scene-specific transforms/settings stay as overrides. New powerup blocks are under Course → Blocks and Pipes.

One Scene Audio prefab per scene contains all six AudioSources; player, enemies and blocks share them. Existing clip/mixer/volume/pitch/loop settings were checked before and after saving. Only GameManager persists. Mario's existing GameConstants assignment is preserved.

**16 Unity PlayMode tests passed, 0 failed**: shared audio/hierarchy, input and jumps, coin/brick/spring behavior, mushrooms/stars, death/restart, pause and scene persistence. Unity also verified saved sprite placements, collider bounds, UI transforms, prefab links, missing scripts and camera/SO bindings against the original scenes. Final assets match the tested copy. [Results](Artifacts/Lab4/Verification/prefab-cleanup-results.xml), [layout verification](Artifacts/Lab4/Verification/prefab-cleanup-verification.txt).

## Historical verification state

**13 Unity PlayMode tests passed, 0 failed**, using Unity 6000.3.24f1. The full course run uses keyboard input to cross the compact course and its three-block gap. An isolated project avoided the open-editor lock; the verified scene and prefabs were copied byte-for-byte back into this project.

Evidence: [test results](Artifacts/Lab3/Verification/playmode-results.xml), [asset hashes](Artifacts/Lab3/Verification/verification.json), [overview](Artifacts/Lab3/Screenshots/overview.png), [gameplay HUD](Artifacts/Lab3/Screenshots/gameplay-start.png), [course clear](Artifacts/Lab3/Screenshots/gameplay-clear.png).

Ground and finish-step colliders use Auto Tiling. Ground, Enemies and Obstacles have named layers; irrelevant enemy/terrain/obstacle interactions are disabled. The mixer includes All Audio and SFX Only views. Gameplay has no runtime scene repair, name-based sprite searches, legacy input or jump-over scoring.

Recordings from the previous longer layout are available in [the local review page](Artifacts/Lab3/review.html): a continuous keyboard course run, individual behavior demonstrations, and a native AudioMixer/Inspector recording with live stomp audio. Captions explain the Enemies lowpass effect. The short behavior clips use test setup positions; the full course run does not teleport Mario. Those 12 recorded demonstrations passed before the course was shortened; they are historical evidence, not previews of the current layout. Audio tracks contain nonzero mixed output; this is a numeric audio check, not a human listening review.

Recording helpers live outside Assets in Artifacts/Lab3/RecordingTools and run only in the isolated validation copy. They add no recording code or objects to the shipped scene.

## Manual presentation checklist

Record with sound: walk/reverse/stop; tap and held jump; climb a pipe; pass through and land on the one-way platform; bounce on the cloud; hit each block type from below, then land on the question block and hit it again; stomp a Goomba and show the flattened sprite and score; jump over another without score; demonstrate death, Game Over and restart; reach the finish.

Show PlayerInput and its action asset, the dynamic MoveCheck UnityEvent binding, the stomp event subscriber, all audio routing groups, the Enemies Lowpass Inspector and the terminal-cue Send/Background Music Duck Volume settings. Explain that the lowpass makes a stomp sound like a dull impact. The Profiler walkthrough is optional practice, not a required checkoff item.

## Children: Navigate Between Scenes

Main Menu and Loading are panels in the existing Canvas prefab, styled like Game Over and shown over World-1-1. Start shows a brief simulated loading overlay; Back to Menu cancels it. Home during play and Menu after death return to the World-1-1 menu. Returning home resets the course in place. The separate menu scenes and extra Canvas prefab were removed.

The final run passed 21 of 24 PlayMode tests, including both new navigation tests, scene persistence, powerups and main course interactions. Three historical course tests use positions from the older layout and failed (cloud at x=12.5, full course route, finish at x=20). [Results](Artifacts/Lab4/Verification/overlay-navigation-results.xml), [main menu preview](Artifacts/Lab4/Previews/Overlay-MainMenu.png), [loading preview](Artifacts/Lab4/Previews/Overlay-Loading.png).
