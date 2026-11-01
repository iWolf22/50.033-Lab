# Teens completion audit

Sources: [Teens checkoff](https://natalieagus.github.io/50033/docs/teen/checkoff/), [SOGA](https://natalieagus.github.io/50033/docs/teen/scriptobjgamearch/), [FSM](https://natalieagus.github.io/50033/docs/teen/fsm/). The Children singleton, C# tour, ScriptableObjects, coroutine, building and checkoff guides were also read for prerequisite context. The supplied Teens checkoff video was sampled for visual comparison.

| Requirement | Current implementation and authoritative evidence |
| --- | --- |
| ScriptableObject game architecture | `Assets/Scripts/Events`: concrete no-argument, int and IPowerup channels/listeners; serialized `Response` bindings on shared prefabs. Native Inspector/source recording shows the channels. |
| No gameplay singleton or persistent object | `GameManager : MonoBehaviour`; no Singleton class or `DontDestroyOnLoad` call remains in runtime sources. `SceneChangeCarriesScoreWithNoPersistentGameplayObjectsOrStaleListeners` passes. Native Hierarchy clip shows both worlds. |
| Five form states and comparable transitions | Small, Super, Fire, protected Small and Dead State assets; mushroom/flower decisions, damage transitions and protection countdown. `ThreeBoxesProduceBigFireAndIndependentTimedStar` and `DamageShrinksProtectsExpiresAndThenKills` pass. Fatal falls also enter Dead and clear star music. |
| Separate buff FSM with expiry | `BuffStateController : StateController`; Default/Star states, eight-second timer, color cycling, protection and star music. Expiry preserves Fire form. The timed-star and contact/restart tests pass and the recording shows expiry. |
| Three Level 1 powerups, one in each box | Existing mushroom at x = -2 and star at x = 2; one added flower at x = -4. Reusable flat block prefabs with one selected powerup. Collision-based collection, single-use and emergence/restart tests pass. |
| Fire input and attacks | Z action through PlayerInput → ActionManager → Fire state action; shared facing variable, three-shot limit, 2.5-second lifetime and enemy hit. `FireUsesKeyboardFacingCapLifetimeAndEnemyHit` passes. Wrapper regenerated from the input asset. |
| Existing sound and state-change feedback | All eight AudioSources remain under one scene-local Scene Audio prefab, with mixer groups assigned. Growth, shrink, fire, death and star music use supplied clips. Shared-audio tests pass; both clips contain captured audio with nonzero RMS. Final combined media is decoded and checked for audio level. |
| Complete in-place restart | Player/input/animation/collider, both FSMs/timers/visuals, fireballs, enemies, spent blocks/powerups/coroutines, score, camera, UI and music reset. Dedicated integration and existing reset tests pass; recording demonstrates restart in both worlds. |
| Multiple scenes and persistent values | Castle transition recreates player/HUD/manager; shared score carries. Native clip shows score 7 in World 1-2. Castle, standalone World 1-2, score/high-score and stale-listener tests pass. |
| Children menu/loading/pause prerequisites | Navigation tests pass, including loading cancellation, Home, post-death Menu, pause/resume, high-score retention/reset and scene return. Current menu and gameplay screenshots are supplied. |
| Preserve user layout/Inspector settings | `layout-preservation.json`: every existing saved transform and prefab override retained in both worlds. `prefab-layout-preservation.json`: all 23 existing prefab layouts retained. No course builder ran. |
| Final validation | `playmode-results.xml`: 30/30 selected checks passed. `test-selection.txt` lists the selection. `asset-hashes.json`: 280 checked gameplay files exactly match the isolated validation copy. |
| Reviewable recording under five minutes | Combined native Inspector/Hierarchy and captioned gameplay video, with mixed audio; separate clips and PNG screenshots are linked from `../review.html`. Focused gameplay demonstrations use setup positions. |

The historical `CourseCanBeCompletedWithKeyboardMovementAndJumps` fixture targets an older layout and was excluded from the selected run; it was already documented as failing before this work. No claim is made that its old route completes the user's edited course. The current checkoff's individual gameplay requirements were tested directly.

The native Inspector capture temporarily disables Unity's optional URP rendering debugger, which otherwise creates an engine-owned persistent helper. Gameplay has no persistent objects with or without that debugger. Captures read Unity's own window framebuffers; no desktop capture is distributed.

Recording/setup utilities stay outside gameplay in `Artifacts/Lab5/Tools` and run only in the ignored validation copy. Existing font and recovery-scene edits were preserved.
