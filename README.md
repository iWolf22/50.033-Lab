# Mario lab: Newborns → Toddlers

Unity **6000.3.24f1**, URP 2D, Input System.

Open `Assets/Scenes/SampleScene.unity`. Move with **A/D** or **←/→**, jump with **Space**, and hold Space for a higher jump. Coins and stomped Goombas award points; touching enemy sides or falling into a pit ends the run. Restart uses Inspector events to restore coins, blocks, enemies, score, camera and input state in the same scene.

The course builder is `Tools → Lab → Rebuild Mario Course`. It saves the level and reusable prefabs in the editor. Rebuilding replaces the course layout, so save custom layouts separately first.

The compact course runs from Mario's spawn at x = -5 to the finish at x = 20, with one three-block gap and all lab obstacle types. It retains the original ending screen.

Gameplay stays in small components:

- `ActionManager`: Input System callbacks → Inspector UnityEvents.
- `PlayerMovement`: physics, facing, jumping, stomping and death animation callbacks.
- `GameManager`: owns score and the start, restart, score-change and game-over UnityEvents.
- `HUDManager`: listens to those events to update scores and toggle the existing UI.
- `EnemyManager`: subscribes to Goomba events, awards stomp points, plays stomp audio and resets enemies.
- `EnemyMovement`: physics-timestep patrol and one-time stomp event.
- `BlockBounce`: underside hits, blinking, coin pop and spent state.
- `AnimationEventIntTool`: forwards the completed coin animation's reward through an Inspector event.
- `CameraController`: bounded follow in LateUpdate.
- `LevelGoal`: finish trigger and clear sound.

See [CHECKOFF.md](CHECKOFF.md) for lab coverage and verification status. Automated gameplay checks are in `Assets/Tests/PlayMode` and run through Unity's Test Runner.

This starter covers Newborns, Babies and Toddlers. Children/Lab 4 scene transitions, persistent state and ScriptableObject architecture are left for your lab work.

Historical demonstrations of the previous longer layout with game audio: [review page](Artifacts/review.html).
