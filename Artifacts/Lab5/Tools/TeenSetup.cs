using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

// Run only in the validation project; never rebuilds the user's level.
public static class TeenSetup
{
    const string Data = "Assets/ScriptableObjects/Teens/";
    static SimpleGameEvent start, restart, over, restartRequest, overRequest, menuRequest;
    static IntGameEvent score, scoreRequest;
    static PowerupGameEvent collected;
    static BoolVariable facing;
    static GameScore gameScore;
    static State small, super, fire, hurt, dead, normalBuff, starBuff, remain;
    static RuntimeAnimatorController smallAnimator, superAnimator, fireAnimator;

    public static void Apply()
    {
        Directory.CreateDirectory(Data);
        Directory.CreateDirectory("Assets/Animation/Powerups");
        AssetDatabase.Refresh();
        start = Asset<SimpleGameEvent>("OnGameStart"); restart = Asset<SimpleGameEvent>("OnGameRestart"); over = Asset<SimpleGameEvent>("OnGameOver");
        restartRequest = Asset<SimpleGameEvent>("RestartRequested"); overRequest = Asset<SimpleGameEvent>("GameOverRequested"); menuRequest = Asset<SimpleGameEvent>("MenuRequested");
        score = Asset<IntGameEvent>("OnScoreChanged"); scoreRequest = Asset<IntGameEvent>("ScoreRequested"); collected = Asset<PowerupGameEvent>("OnPowerupCollected");
        gameScore = AssetDatabase.LoadAssetAtPath<GameScore>("Assets/ScriptableObjects/GameScore.asset");
        facing = Asset<BoolVariable>("MarioFacingRight"); facing.Value = true;
        CreateStates();
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            Configure(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }
        // Existing two powerup boxes keep their transforms. Add just the missing flower box.
        EditorSceneManager.OpenScene("Assets/Scenes/World-1-1.unity");
        if (!Object.FindObjectsByType<FireFlowerPowerup>(FindObjectsInactive.Include, FindObjectsSortMode.None).Any())
        {
            var mushroom = Object.FindObjectsByType<MagicMushroomPowerup>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
            var parent = mushroom.transform.parent.parent;
            var box = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Blocks/FireFlowerQuestionBlock.prefab"), parent);
            box.transform.position = new Vector3(-4, 0, 0);
        }
        foreach (var root in Object.FindObjectsByType<NextScene>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Listen(root.gameObject, restart, root.GameRestart);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        EditorSceneManager.OpenScene("Assets/Scenes/World-1-2.unity");
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        File.WriteAllText("Logs/teen-setup.txt", "Event channels, two FSMs, shared prefabs and three Level 1 boxes configured.\n");
    }
    public static void FinalizeLayout()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/World-1-1.unity");
        var flower = Object.FindObjectsByType<FireFlowerPowerup>(FindObjectsInactive.Include, FindObjectsSortMode.None).SingleOrDefault();
        if (flower == null)
        {
            var mushroom = Object.FindObjectsByType<MagicMushroomPowerup>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
            var box = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Blocks/FireFlowerQuestionBlock.prefab"), mushroom.transform.parent.parent);
            flower = box.GetComponentInChildren<FireFlowerPowerup>(true);
        }
        flower.transform.parent.position = new Vector3(-4, 0, 0);
        foreach (var trigger in Object.FindObjectsByType<NextScene>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            foreach (var old in trigger.GetComponents<SimpleGameEventListener>()) Object.DestroyImmediate(old);
            var channel = AssetDatabase.LoadAssetAtPath<SimpleGameEvent>(Data + "OnGameRestart.asset");
            Listen(trigger.gameObject, channel, trigger.GameRestart);
        }
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
    }
    public static void WireFatalState()
    {
        const string path = "Assets/Prefabs/Player/Mario.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        Set(root.GetComponent<MarioStateController>(), "deadState", AssetDatabase.LoadAssetAtPath<State>(Data + "DeadMario.asset"));
        PrefabUtility.SaveAsPrefabAsset(root, path); PrefabUtility.UnloadPrefabContents(root);
    }
    static T Asset<T>(string name) where T : ScriptableObject
    {
        string path = Data + name + ".asset";
        var result = AssetDatabase.LoadAssetAtPath<T>(path);
        if (result == null) { result = ScriptableObject.CreateInstance<T>(); result.name = name; AssetDatabase.CreateAsset(result, path); }
        return result;
    }
    static void Listen(GameObject root, SimpleGameEvent channel, UnityAction callback)
    {
        var listener = root.AddComponent<SimpleGameEventListener>(); listener.Event = channel;
        UnityEventTools.AddVoidPersistentListener(listener.Response, callback);
    }
    static void Listen(GameObject root, IntGameEvent channel, UnityAction<int> callback)
    {
        var listener = root.AddComponent<IntGameEventListener>(); listener.Event = channel;
        UnityEventTools.AddPersistentListener(listener.Response, callback);
    }
    static void Listen(GameObject root, PowerupGameEvent channel, UnityAction<IPowerup> callback)
    {
        var listener = root.AddComponent<PowerupGameEventListener>(); listener.Event = channel;
        UnityEventTools.AddPersistentListener(listener.Response, callback);
    }
    static void Configure(GameObject root)
    {
        foreach (var old in root.GetComponentsInChildren<MonoBehaviour>(true).Where(c => c is SimpleGameEventListener || c is IntGameEventListener || c is PowerupGameEventListener).ToArray()) Object.DestroyImmediate(old);
        foreach (var manager in root.GetComponentsInChildren<GameManager>(true))
        {
            manager.gameStart = start; manager.gameRestart = restart; manager.gameOver = over; manager.scoreChange = score;
            Listen(manager.gameObject, restartRequest, manager.GameRestart); Listen(manager.gameObject, overRequest, manager.GameOver);
            Listen(manager.gameObject, menuRequest, manager.ReturnToMenu); Listen(manager.gameObject, scoreRequest, manager.IncreaseScore);
        }
        foreach (var player in root.GetComponentsInChildren<PlayerMovement>(true))
        {
            Set(player, "gameOverRequested", overRequest); Set(player, "facingRight", facing);
            Listen(player.gameObject, restart, player.GameRestart);
            var mario = player.GetComponent<MarioStateController>() ?? player.gameObject.AddComponent<MarioStateController>();
            var buff = player.GetComponent<BuffStateController>() ?? player.gameObject.AddComponent<BuffStateController>();
            mario.startState = small; mario.remainState = remain; mario.currentState = null;
            buff.startState = normalBuff; buff.remainState = remain; buff.currentState = null;
            Set(mario, "deadState", dead);
            Set(mario, "facingRight", facing); Set(mario, "fireSound", Clip("smb_fireball.wav"));
            Listen(player.gameObject, restart, mario.GameRestart); Listen(player.gameObject, restart, buff.GameRestart);
            Listen(player.gameObject, collected, mario.ReceivePowerup); Listen(player.gameObject, collected, buff.ReceivePowerup);
            var bridge = player.GetComponent<ActionManager>();
            bridge.fire = new UnityEvent(); UnityEventTools.AddPersistentListener(bridge.fire, mario.Fire);
            var input = player.GetComponent<PlayerInput>();
            var fireAction = input.actions.FindAction("gameplay/fire");
            if (!input.actionEvents.Any(e => e.actionId == fireAction.id.ToString()))
            {
                var fireEvent = new PlayerInput.ActionEvent(fireAction);
                UnityEventTools.AddPersistentListener(fireEvent, bridge.OnFireAction);
                input.actionEvents = input.actionEvents.Append(fireEvent).ToArray();
            }
        }
        foreach (var hud in root.GetComponentsInChildren<HUDManager>(true))
        {
            Set(hud, "gameScore", gameScore); Set(hud, "restartRequested", restartRequest); Set(hud, "menuRequested", menuRequest);
            Listen(hud.gameObject, start, hud.GameStart); Listen(hud.gameObject, restart, hud.GameStart); Listen(hud.gameObject, over, hud.GameOver); Listen(hud.gameObject, score, hud.SetScore);
        }
        foreach (var button in root.GetComponentsInChildren<RestartButtonController>(true)) Set(button, "restartRequested", restartRequest);
        foreach (var pause in root.GetComponentsInChildren<PauseButtonController>(true)) { Listen(pause.gameObject, restart, pause.ResetPause); Listen(pause.gameObject, start, pause.ResetPause); }
        foreach (var camera in root.GetComponentsInChildren<CameraController>(true)) Listen(camera.gameObject, restart, camera.GameRestart);
        foreach (var manager in root.GetComponentsInChildren<EnemyManager>(true)) { Set(manager, "scoreRequested", scoreRequest); Listen(manager.gameObject, restart, manager.GameRestart); }
        foreach (var music in root.GetComponentsInChildren<BackgroundMusic>(true)) { Listen(music.gameObject, restart, music.GameRestart); Listen(music.gameObject, over, music.GameOver); }
        foreach (var block in root.GetComponentsInChildren<BlockBounce>(true)) Listen(block.gameObject, restart, block.GameRestart);
        foreach (var reward in root.GetComponentsInChildren<AnimationEventIntTool>(true)) { reward.useInt = new UnityEvent<int>(); UnityEventTools.AddPersistentListener(reward.useInt, scoreRequest.Raise); }
        foreach (var powerup in root.GetComponentsInChildren<MovingPowerup>(true)) Set(powerup, "collected", collected);
        foreach (var audio in root.GetComponentsInChildren<SceneAudio>(true))
        {
            audio.powerups = Source(audio, "Powerups", Clip("smb_powerup.wav"), false, audio.blocks.outputAudioMixerGroup);
            audio.starMusic = Source(audio, "Star Music", Clip("05-starman.mp3"), true, audio.GetComponentInChildren<BackgroundMusic>().GetComponent<AudioSource>().outputAudioMixerGroup);
        }
    }
    static AudioSource Source(SceneAudio audio, string name, AudioClip clip, bool loop, AudioMixerGroup group)
    {
        var child = audio.transform.Find(name);
        if (child != null) return child.GetComponent<AudioSource>();
        var go = new GameObject(name); go.transform.SetParent(audio.transform, false);
        var source = go.AddComponent<AudioSource>(); source.clip = clip; source.loop = loop; source.playOnAwake = false; source.outputAudioMixerGroup = group; source.volume = 0.6f;
        return source;
    }
    static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/" + name);
    static void Set(Object target, string field, Object value)
    {
        var serialized = new SerializedObject(target); serialized.FindProperty(field).objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    static Transition To(Decision decision, State target) => new Transition { decision = decision, trueState = target, falseState = remain };
    static void CreateStates()
    {
        remain = Asset<State>("RemainInState"); small = Asset<State>("SmallMario"); super = Asset<State>("SuperMario"); fire = Asset<State>("FireMario"); hurt = Asset<State>("InvincibleSmallMario"); dead = Asset<State>("DeadMario");
        normalBuff = Asset<State>("DefaultBuff"); starBuff = Asset<State>("StarInvincible");
        smallAnimator = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/Mario.controller");
        superAnimator = CreateAnimator("super"); fireAnimator = CreateAnimator("fire");
        var mushroom = Asset<PowerupDecision>("CollectedMushroom"); mushroom.powerup = PowerupType.MagicMushroom;
        var flower = Asset<PowerupDecision>("CollectedFlower"); flower.powerup = PowerupType.FireFlower;
        var damage = Asset<PowerupDecision>("Damaged"); damage.powerup = PowerupType.Damage;
        var star = Asset<PowerupDecision>("CollectedStar"); star.powerup = PowerupType.StarMan;
        var protectionTimer = Asset<CountdownDecision>("DamageProtectionTimer"); protectionTimer.duration = 2;
        var starTimer = Asset<CountdownDecision>("StarTimer"); starTimer.duration = 8;
        Setup(small, MarioForm.Small, smallAnimator, null); Setup(super, MarioForm.Super, superAnimator, Clip("smb_powerup.wav"));
        var bigDamageSetup = Asset<MarioFormAction>("SetupSuperMario"); bigDamageSetup.damageSound = Clip("smb_pipe_powerdown.wav"); bigDamageSetup.damageProtectionDuration = 2;
        Setup(fire, MarioForm.Fire, fireAnimator, Clip("smb_powerup.wav")); Setup(hurt, MarioForm.Hurt, smallAnimator, Clip("smb_pipe_powerdown.wav")); Setup(dead, MarioForm.Dead, smallAnimator, null);
        small.transitions = new[] { To(damage, dead), To(mushroom, super), To(flower, super) };
        super.transitions = new[] { To(damage, hurt), To(flower, fire) }; fire.transitions = new[] { To(damage, super) }; hurt.transitions = new[] { To(protectionTimer, small) };
        normalBuff.transitions = new[] { To(star, starBuff) }; starBuff.transitions = new[] { To(starTimer, normalBuff) };
        var startStar = Asset<StarAction>("StartStar"); startStar.active = true;
        var endStar = Asset<StarAction>("EndStar"); endStar.active = false;
        normalBuff.setupActions = new StateAction[] { endStar }; starBuff.setupActions = new StateAction[] { startStar }; starBuff.exitActions = new StateAction[] { endStar };
        var attack = Asset<FireAttackAction>("FireAttack"); attack.fireballPrefab = CreateFireball(); fire.attackActions = new StateAction[] { attack };
        CreateFlowerBox();
        foreach (var guid in AssetDatabase.FindAssets("", new[] { Data })) { var obj = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)); if (obj != null) EditorUtility.SetDirty(obj); }
    }
    static void Setup(State state, MarioForm form, RuntimeAnimatorController animator, AudioClip sound)
    {
        var setup = Asset<MarioFormAction>("Setup" + state.name); setup.form = form; setup.animatorController = animator; setup.sound = sound; state.setupActions = new StateAction[] { setup };
    }
    static RuntimeAnimatorController CreateAnimator(string form)
    {
        string path = "Assets/Animation/Powerups/" + form + ".overrideController";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
        if (controller == null) { controller = new AnimatorOverrideController(smallAnimator); AssetDatabase.CreateAsset(controller, path); }
        var sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/mario.png").OfType<Sprite>().ToArray();
        foreach (string action in new[] { "idle", "run", "jump", "skid" })
        {
            string clipPath = "Assets/Animation/Powerups/" + form + "-" + action + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null) { clip = Object.Instantiate(AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animation/mario-" + action + ".anim")); AssetDatabase.CreateAsset(clip, clipPath); }
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var keys = AnimationUtility.GetObjectReferenceCurve(AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animation/mario-" + action + ".anim"), binding);
            for (int i = 0; i < keys.Length; i++)
            {
                var old = (Sprite)keys[i].value;
                string label = action == "run" ? old.name.Replace("small-mario-", "") : action;
                keys[i].value = sprites.Single(s => s.name == form + "-mario-" + label);
            }
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys); controller["mario-" + action] = clip;
        }
        EditorUtility.SetDirty(controller); return controller;
    }
    static FireballController CreateFireball()
    {
        const string path = "Assets/Prefabs/Powerups/Fireball.prefab";
        Directory.CreateDirectory("Assets/Prefabs/Powerups");
        var go = new GameObject("Fireball");
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/misc-3.gif").OfType<Sprite>().Single(s => s.name == "fireball"); renderer.sortingOrder = 5;
        go.transform.localScale = Vector3.one;
        var body = go.AddComponent<Rigidbody2D>(); body.gravityScale = 1; body.freezeRotation = true; body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        go.AddComponent<CircleCollider2D>().isTrigger = true;
        go.AddComponent<FireballController>();
        var result = PrefabUtility.SaveAsPrefabAsset(go, path).GetComponent<FireballController>(); Object.DestroyImmediate(go); return result;
    }
    static void CreateFlowerBox()
    {
        const string path = "Assets/Prefabs/Blocks/FireFlowerQuestionBlock.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            var saved = PrefabUtility.LoadPrefabContents(path);
            saved.GetComponentInChildren<FireFlowerPowerup>(true).GetComponent<SpriteRenderer>().sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/misc-3.gif").OfType<Sprite>().Single(s => s.name == "fire-flower");
            PrefabUtility.SaveAsPrefabAsset(saved, path); PrefabUtility.UnloadPrefabContents(saved); return;
        }
        var root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Blocks/MushroomQuestionBlock.prefab"); root.name = "FireFlowerQuestionBlock";
        var mushroom = root.GetComponentInChildren<MagicMushroomPowerup>(true); var go = mushroom.gameObject; Object.DestroyImmediate(mushroom);
        go.name = "Fire Flower"; var flower = go.AddComponent<FireFlowerPowerup>(); Set(flower, "collected", collected);
        go.GetComponent<SpriteRenderer>().sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/misc-3.gif").OfType<Sprite>().Single(s => s.name == "fire-flower");
        Set(root.GetComponentInChildren<BlockBounce>(), "powerup", flower);
        PrefabUtility.SaveAsPrefabAsset(root, path); PrefabUtility.UnloadPrefabContents(root);
    }
}
