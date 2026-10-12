# Lab coverage and verification

Source guides:

- [Newborns: installation](https://natalieagus.github.io/50033/docs/newborns/installation/), [Mario](https://natalieagus.github.io/50033/docs/newborns/basics/), [UI](https://natalieagus.github.io/50033/docs/newborns/ui/), [checkoff](https://natalieagus.github.io/50033/docs/newborns/checkoff/)
- [Babies: animation](https://natalieagus.github.io/50033/docs/babies/animation/), [camera](https://natalieagus.github.io/50033/docs/babies/camera/), [physics](https://natalieagus.github.io/50033/docs/babies/physics/), [checkoff](https://natalieagus.github.io/50033/docs/babies/checkoff/)
- [Toddlers: input](https://natalieagus.github.io/50033/docs/toddlers/management/), [observer](https://natalieagus.github.io/50033/docs/toddlers/observer-pattern/), [audio](https://natalieagus.github.io/50033/docs/toddlers/audio-management/), [checkoff](https://natalieagus.github.io/50033/docs/toddlers/checkoff/)

## Saved course

Compact course from spawn x = -5 to finish x = 20 with flat terrain, one 3-unit pit, a corrected pipe, plain/coin/question blocks, two patrols, a one-way platform, a spring question block, a bouncy cloud, short finish steps, flagpole and castle. Ground extends left to support the camera-edge boundary at different aspect ratios. The original red Game Over title, score and restart layout are retained; the top controls text is removed. Background hills, bushes and clouds use the supplied sprites. No gameplay objects are generated at startup.

## Starter scope

The essential setup, movement/UI, animation/camera/Physics2D, Input System, observer and audio work through Toddlers is included. Unity 6.3, the sprite/Input System/editor packages, Force Text serialization and the existing folder structure are configured; personal IDE preferences remain yours.

`GameManager` exposes the tutorial's four UnityEvents. The saved scene binds them to `HUDManager`, `EnemyManager`, player, camera, blocks, finish trigger and music. Enemy observers subscribe in OnEnable and unsubscribe in OnDisable. Coin rewards use `AnimationEventIntTool` at completion of the existing coin animation. Restart restores objects in place, including inactive enemies and spent spring blocks. Prefab reward targets are bound to the scene's manager when the editor builder places instances.

Optional mouse-position diagnostics and standalone event demonstrations are omitted. The Children C# Static Variable section is implemented in the scripts; interfaces, powerups and ScriptableObjects remain outside this change.

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

## Children: static instance and interactive buttons

Only `GameManager : Singleton<GameManager>` persists. Scene components subscribe/unsubscribe to its UnityEvents in OnEnable/OnDisable; rewards and restart buttons use `GameManager.instance`. Scene changes restore gameplay UI and publish the carried score. Scene audio stays outside the persistent manager.

The short World-1-2 demo has flat ground, one Goomba, clouds, a hill, bushes and a decorative castle with no outgoing transition. Open `Assets/Scenes/World-1-2.unity` directly or enter the castle in World-1-1.

**10 selected Unity PlayMode tests passed, 0 failed**: castle transition and score carryover, World-1-2 standalone scoring, World-1-2 restart including inactive Goomba/music, and seven movement/jump/stomp/coin/death regressions. Tests ran in `.unity-validation/` using the final script sources and the same demo scene installed in this project. The broader historical course tests were not rerun. [Results](Artifacts/static-playmode-results.xml), [demo preview](Artifacts/world-1-2-demo.png).

`IInteractiveButton` defines `ButtonClick`; restart controllers retain their original script GUID and pause controllers switch between matching pause/play sprites. Both scenes place pause beside restart. Restart clears pause state and the icon; hidden pause controls cannot resume Game Over.

**4 Unity PlayMode tests passed, 0 failed** after the button change: pause/resume and restart in both worlds, castle score carryover, World-1-2 restart/scoring and standalone World-1-2. [Results](Artifacts/buttons-playmode-results.xml). Icons adapt Colorosse’s [CC0 Sandbox Menu Icons](https://colorosse.com/assets/2d/ui/sandbox-menu-icons-16); SVG sources and provenance are in `Artifacts/AssetSources/ButtonIcons/`.

## Historical verification state

**13 Unity PlayMode tests passed, 0 failed**, using Unity 6000.3.24f1. The full course run uses keyboard input to cross the compact course and its three-block gap. An isolated project avoided the open-editor lock; the verified scene and prefabs were copied byte-for-byte back into this project.

Evidence: [test results](Artifacts/playmode-results.xml), [asset hashes](Artifacts/verification.json), [overview](Artifacts/overview.png), [gameplay HUD](Artifacts/gameplay-start.png), [course clear](Artifacts/gameplay-clear.png).

Ground and finish-step colliders use Auto Tiling. Ground, Enemies and Obstacles have named layers; irrelevant enemy/terrain/obstacle interactions are disabled. The mixer includes All Audio and SFX Only views. Gameplay has no runtime scene repair, name-based sprite searches, legacy input, debug logging, or jump-over scoring.

Recordings from the previous longer layout are available in [the local review page](Artifacts/review.html): a continuous keyboard course run, individual behavior demonstrations, and a native AudioMixer/Inspector recording with live stomp audio. Captions explain the Enemies lowpass effect. The short behavior clips use test setup positions; the full course run does not teleport Mario. Those 12 recorded demonstrations passed before the course was shortened; they are historical evidence, not previews of the current layout. Audio tracks contain nonzero mixed output; this is a numeric audio check, not a human listening review.

Recording helpers live outside Assets in Artifacts/RecordingTools and run only in the isolated validation copy. They add no recording code or objects to the shipped scene.

## Manual presentation checklist

Record with sound: walk/reverse/stop; tap and held jump; climb a pipe; pass through and land on the one-way platform; bounce on the cloud; hit each block type from below, then land on the question block and hit it again; stomp a Goomba and show the flattened sprite and score; jump over another without score; demonstrate death, Game Over and restart; reach the finish.

Show PlayerInput and its action asset, the dynamic MoveCheck UnityEvent binding, the stomp event subscriber, all audio routing groups, the Enemies Lowpass Inspector and the terminal-cue Send/Background Music Duck Volume settings. Explain that the lowpass makes a stomp sound like a dull impact. The Profiler walkthrough is optional practice, not a required checkoff item.
