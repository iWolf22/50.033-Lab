using System.Linq;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class LabLevelBuilder
{
    static Sprite[] sprites;
    static AudioMixer mixer;
    static Material spriteMaterial;

    [MenuItem("Tools/Lab/Rebuild Mario Course")]
    public static void Build()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/World-1-1.unity");
        Directory.CreateDirectory("Assets/Materials");
        AssetDatabase.Refresh();
        var cloudImporter = (TextureImporter)AssetImporter.GetAtPath("Assets/Sprites/lakitus-cloud.png");
        cloudImporter.textureType = TextureImporterType.Sprite; cloudImporter.spritePixelsPerUnit = 64;
        cloudImporter.filterMode = FilterMode.Point; cloudImporter.SaveAndReimport();
        sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/misc-3.gif").OfType<Sprite>()
            .Concat(AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/characters.gif").OfType<Sprite>()).ToArray();
        mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/InputActions/SuperMarioBros.mixer");
        ConfigureMarioAnimations();
        var player = Object.FindFirstObjectByType<PlayerMovement>();
        spriteMaterial = player.GetComponent<SpriteRenderer>().sharedMaterial;
        var score = Find("ScoreText").GetComponent<TextMeshProUGUI>();
        var finalScore = Find("ScoreText-GameOver").GetComponent<TextMeshProUGUI>();
        var playUI = Find("GamePlay");
        var overUI = finalScore.transform.parent.gameObject;
        foreach (var root in player.gameObject.scene.GetRootGameObjects())
            if (new[] { "Scene", "Obstacles", "Enemies", "Sounds", "Course", "Game Session", "Game Manager", "Scene Audio" }.Contains(root.name))
                Object.DestroyImmediate(root);
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(player.gameObject);

        var course = new GameObject("Course").transform;
        var terrain = Group("Terrain", course);
        var scenery = Group("Scenery", course);
        var obstacles = Group("Blocks and Pipes", course);
        var enemies = ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/EnemyManager.prefab"), course)).transform;
        var session = ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Core/GameManager.prefab"))).GetComponent<GameManager>();
        session.gameObject.tag = "Manager";
        var hud = playUI.transform.parent.GetComponent<HUDManager>();
        if (hud == null) hud = playUI.transform.parent.gameObject.AddComponent<HUDManager>();
        var enemyManager = enemies.GetComponent<EnemyManager>();
        var sounds = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Audio/SceneAudio.prefab"));
        var sharedAudio = sounds.GetComponent<SceneAudio>();
        var stomp = sharedAudio.goombaStomp;
        var death = sharedAudio.playerDeath;
        player.marioAudio = sharedAudio.playerJump;
        Set(hud, "scoreText", score); Set(hud, "finalScoreText", finalScore);
        Set(hud, "gamePlayUI", playUI); Set(hud, "gameOverUI", overUI);
        Set(enemyManager, "stompAudio", stomp);
        Set(player, "deathAudio", death);
        Set(player, "groundLayers", (1 << 3) | (1 << 7));
        player.GetComponent<PlayerInput>().defaultControlScheme = "Keyboard";
        player.transform.position = new Vector3(-5, -2.45f, 0);
        var body = player.GetComponent<Rigidbody2D>();
        body.gravityScale = 3; body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        player.GetComponent<BoxCollider2D>().size = new Vector2(0.8f, 1);
        player.marioAudio.outputAudioMixerGroup = mixer.FindMatchingGroups("Player").First();
        player.marioAudio.playOnAwake = false;

        // A compact course with one three-block gap and every lab obstacle.
        Ground(-16, 9, terrain); Ground(12, 25, terrain);
        var leftWall = Group("Left boundary", terrain);
        leftWall.position = new Vector3(-10.916667f, 1, 0); leftWall.gameObject.layer = 7;
        leftWall.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(0.5f, 10);
        Pipe(5, 2, obstacles);
        var plain = BlockPrefab("brown-brick-no-coin", false, false);
        var coinBrick = BlockPrefab("brown-brick-coin", true, false);
        var question = BlockPrefab("question-mark-wrapper", true, true);
        Place(coinBrick, new Vector2(-1, 0), obstacles);
        Place(question, new Vector2(0, 0), obstacles);
        Place(plain, new Vector2(1, 0), obstacles);
        Place(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Blocks/MushroomQuestionBlock.prefab"), new Vector2(-3, 0), obstacles);
        Place(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Blocks/StarQuestionBlock.prefab"), new Vector2(2.5f, 0), obstacles);
        Platform(15, 0, obstacles);
        SpringBlock(17, 0, obstacles);
        BouncyCloud(12.5f, -0.5f, obstacles);
        for (int i = 0; i < 3; i++)
        {
            var step = Tile("Finish step", "question-mark-solid", new Vector2(18 + i, -3 + (i + 1) / 2f), obstacles, 1);
            step.drawMode = SpriteDrawMode.Tiled; step.size = new Vector2(1, i + 1);
            step.gameObject.layer = 7;
            var collider = step.gameObject.AddComponent<BoxCollider2D>();
            collider.size = step.size; collider.autoTiling = true;
        }
        var goomba = GoombaPrefab();
        foreach (float x in new[] { 3.5f, 14f })
        {
            var enemy = Place(goomba, new Vector2(x, -2.5f), enemies);
            var serialized = new SerializedObject(enemy.GetComponent<EnemyMovement>());
            serialized.FindProperty("patrolRadius").floatValue = 0.5f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        for (int i = 0; i < 3; i++)
        {
            float x = -5 + i * 10;
            Tile("Cloud", i % 2 == 0 ? "cloud-big" : "cloud-small", new Vector2(x, 3 + i % 3), scenery, -3);
            var hill = Sprite(i % 2 == 0 ? "hill-big" : "hill-small");
            Tile("Hill", hill.name, new Vector2(x + 4, -3 + hill.bounds.size.y * 0.5f), scenery, -2);
            Tile("Bush", i % 2 == 0 ? "bg_bush_3" : "bg_bush_1", new Vector2(x + 8, -2.5f), scenery, -1);
        }
        var end = new GameObject("Camera End").transform;
        end.SetParent(course); end.position = new Vector3(25, 0, 0);
        var camera = Object.FindFirstObjectByType<CameraController>();
        camera.player = player.transform; camera.endLimit = end; camera.leftBoundary = leftWall;
        camera.transform.position = new Vector3(0, 1, -10);
        camera.GetComponent<Camera>().orthographicSize = 6;
        camera.GetComponent<Camera>().backgroundColor = new Color32(92, 148, 252, 255);
        Finish(20, course, session);
        Tile("Castle left", "castle_small_left", new Vector2(21.75f, -0.5f), scenery, 0);
        Tile("Castle right", "castle_small_right", new Vector2(24.25f, -0.5f), scenery, 0);
        foreach (var button in playUI.transform.parent.GetComponentsInChildren<Button>(true))
        {
            if (button.TryGetComponent<PauseButtonController>(out _)) continue;
            button.onClick = new Button.ButtonClickedEvent();
            var restart = button.GetComponent<RestartButtonController>() ?? button.gameObject.AddComponent<RestartButtonController>();
            UnityEventTools.AddPersistentListener(button.onClick, restart.ButtonClick);
        }
        if (playUI.GetComponentInChildren<PauseButtonController>(true) == null)
        {
            var restart = playUI.GetComponentInChildren<RestartButtonController>(true);
            var pauseObject = Object.Instantiate(restart.gameObject, restart.transform.parent);
            pauseObject.name = "PauseButton";
            Object.DestroyImmediate(pauseObject.GetComponent<RestartButtonController>());
            var pause = pauseObject.AddComponent<PauseButtonController>();
            var pauseIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/pause-button.png");
            Set(pause, "pauseIcon", pauseIcon);
            Set(pause, "playIcon", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/play-button.png"));
            pauseObject.GetComponent<Image>().sprite = pauseIcon;
            pauseObject.GetComponent<RectTransform>().anchoredPosition += Vector2.left * 70;
            var button = pauseObject.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(button.onClick, pause.ButtonClick);
        }
        LayoutUI(playUI, overUI, score, finalScore);
        var eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem.TryGetComponent<StandaloneInputModule>(out var legacyUI)) Object.DestroyImmediate(legacyUI);
        if (!eventSystem.TryGetComponent<InputSystemUIInputModule>(out var uiInput))
        {
            uiInput = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            uiInput.AssignDefaultActions();
        }
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/World-1-1.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/World-1-2.unity", true)
        };
        foreach (var component in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (component == null) continue;
            var serialized = new SerializedObject(component);
            var property = serialized.GetIterator();
            while (property.Next(true))
                if (property.name == "m_TargetAssemblyTypeName" && property.propertyType == SerializedPropertyType.String &&
                    (property.stringValue.StartsWith("PlayerMovement,") || property.stringValue.StartsWith("ActionManager,")))
                    property.stringValue = property.stringValue.Replace("Assembly-CSharp", "Mario.Gameplay");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorSceneManager.SaveScene(player.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Mario course rebuilt and saved.");
    }

    static void ConfigureMarioAnimations()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animation/Mario.controller");
        var machine = controller.layers[0].stateMachine;
        var states = machine.states.Select(s => s.state).ToArray();
        var idle = states.First(s => s.name == "mario-idle");
        var run = states.First(s => s.name == "mario-run");
        var jump = states.First(s => s.name == "mario-jump");
        var skid = states.First(s => s.name == "mario-skid");
        Transition(run, jump, "onGround", AnimatorConditionMode.IfNot);
        Transition(run, skid, "onSkid", AnimatorConditionMode.If);
        Transition(idle, skid, "onSkid", AnimatorConditionMode.If);
        Transition(skid, jump, "onGround", AnimatorConditionMode.IfNot);
        if (!skid.transitions.Any(t => t.destinationState == run))
        {
            var transition = skid.AddTransition(run);
            transition.hasExitTime = true; transition.exitTime = 1; transition.duration = 0;
        }
        var clip = (AnimationClip)idle.motion;
        foreach (var binding in AnimationUtility.GetCurveBindings(clip)) AnimationUtility.SetEditorCurve(clip, binding, null);
        var standing = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/mario.png").OfType<Sprite>().First(s => s.name == "mario-right");
        AnimationUtility.SetObjectReferenceCurve(clip, new EditorCurveBinding { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" },
            new[] { new ObjectReferenceKeyframe { time = 0, value = standing }, new ObjectReferenceKeyframe { time = 1f / 6, value = standing } });
    }

    static void Transition(AnimatorState source, AnimatorState destination, string parameter, AnimatorConditionMode mode)
    {
        if (source.transitions.Any(t => t.destinationState == destination)) return;
        var transition = source.AddTransition(destination);
        transition.hasExitTime = false; transition.duration = 0;
        transition.AddCondition(mode, 0, parameter);
    }

    static GameObject Find(string name) => Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
        .First(t => t.name == name).gameObject;

    static Transform Group(string name, Transform parent)
    {
        var result = new GameObject(name).transform;
        result.SetParent(parent, false);
        return result;
    }

    static Sprite Sprite(string name) => sprites.First(s => s.name == name);

    static SpriteRenderer Tile(string name, string sprite, Vector2 position, Transform parent, int order)
    {
        var visual = new GameObject(name).AddComponent<SpriteRenderer>();
        visual.transform.SetParent(parent, false);
        visual.transform.position = position;
        visual.sprite = Sprite(sprite); visual.sortingOrder = order; visual.sharedMaterial = spriteMaterial;
        return visual;
    }

    static void Set(Object target, string field, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Set(Object target, string field, int value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).intValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Ground(float left, float right, Transform parent)
    {
        var ground = Tile("Ground", "brown-ground", new Vector2((left + right) / 2, -4), parent, 1);
        ground.drawMode = SpriteDrawMode.Tiled; ground.size = new Vector2(right - left, 2);
        ground.gameObject.layer = 3;
        var collider = ground.gameObject.AddComponent<BoxCollider2D>();
        collider.size = ground.size; collider.autoTiling = true;
    }

    static void Pipe(float x, int height, Transform parent)
    {
        var pipe = Group("Pipe", parent);
        pipe.position = new Vector3(x, -3, 0);
        Tile("Pipe rim", "pipe-top", new Vector2(x, -3 + height - 1), pipe, 2);
        if (height > 2)
        {
            var shaft = Tile("Pipe shaft", "pipe-mid", new Vector2(x, -3 + (height - 2) / 2f), pipe, 1);
            shaft.drawMode = SpriteDrawMode.Tiled;
            shaft.size = new Vector2(shaft.sprite.bounds.size.x, height - 2);
        }
        pipe.gameObject.layer = 7;
        var col = pipe.gameObject.AddComponent<BoxCollider2D>();
        col.size = new Vector2(2, height); col.offset = new Vector2(0, height / 2f);
    }

    static GameObject BlockPrefab(string name, bool reward, bool question)
    {
        var root = new GameObject(name);
        root.layer = 7;
        root.AddComponent<BoxCollider2D>().size = Vector2.one;
        var visual = Tile("Block visual", question ? "question-mark" : "brown-brick", Vector2.zero, root.transform, 2);
        var animator = visual.gameObject.AddComponent<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/Block.controller");
        SpriteRenderer coin = null;
        AnimationEventIntTool rewardEvent = null;
        if (reward)
        {
            coin = Tile("Coin", "coin", Vector2.zero, root.transform, 1);
            coin.gameObject.SetActive(false);
            rewardEvent = coin.gameObject.AddComponent<AnimationEventIntTool>();
        }
        var block = question ? (BlockBounce)root.AddComponent<QuestionBoxPowerupController>() : root.AddComponent<BrickPowerupController>();
        block.spawnCoin = reward; block.isQuestionBlock = question;
        block.flashSprite = Sprite("question-mark-flash"); block.solidSprite = Sprite("question-mark-solid");
        Set(block, "coinReward", rewardEvent);
        Set(block, "powerup", coin != null ? coin.gameObject.AddComponent<CoinPowerup>() : null);
        Set(block, "visual", visual); Set(block, "coin", coin); Set(block, "bounceAnimator", animator);
        Set(block, "coinSound", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/smb_coin.wav"));
        Set(block, "bumpSound", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/smb_bump.wav"));
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Blocks/" + (question ? "CoinQuestionBlock" : reward ? "CoinBrickBlock" : "BrickBlock") + ".prefab");
        Object.DestroyImmediate(root);
        return prefab;
    }

    static GameObject Place(GameObject prefab, Vector2 position, Transform parent)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.transform.position = position;
        return instance;
    }

    static GameObject GoombaPrefab()
    {
        var root = Tile("Goomba", "goombas-1", Vector2.zero, null, 3).gameObject;
        root.layer = 6; root.tag = "Enemy";
        var body = root.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic; body.interpolation = RigidbodyInterpolation2D.Interpolate;
        root.AddComponent<BoxCollider2D>().isTrigger = true;
        var enemy = root.AddComponent<EnemyMovement>(); Set(enemy, "stompedSprite", Sprite("dead-goombas"));
        var clip = new AnimationClip { name = "goomba-walk", frameRate = 6 };
        AnimationUtility.SetObjectReferenceCurve(clip, new EditorCurveBinding { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" },
            new[] { new ObjectReferenceKeyframe { time = 0, value = Sprite("goombas-1") },
                new ObjectReferenceKeyframe { time = 1f / 6, value = Sprite("goombas-2") },
                new ObjectReferenceKeyframe { time = 2f / 6, value = Sprite("goombas-1") } });
        var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        var existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animation/goomba-walk.anim");
        if (existingClip == null) AssetDatabase.CreateAsset(clip, "Assets/Animation/goomba-walk.anim");
        else { EditorUtility.CopySerialized(clip, existingClip); Object.DestroyImmediate(clip); clip = existingClip; }
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animation/Goomba.controller");
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath("Assets/Animation/Goomba.controller");
            controller.AddMotion(clip);
        }
        root.AddComponent<Animator>().runtimeAnimatorController = controller;
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Enemies/Goomba.prefab");
        Object.DestroyImmediate(root);
        return prefab;
    }

    static void Platform(float x, float y, Transform parent)
    {
        var visual = Tile("One-way platform", "moving_platform_6", new Vector2(x, y), parent, 2);
        visual.drawMode = SpriteDrawMode.Tiled; visual.size = new Vector2(4, 0.4f);
        visual.gameObject.layer = 7;
        var edge = visual.gameObject.AddComponent<EdgeCollider2D>();
        edge.points = new[] { new Vector2(-2, 0.2f), new Vector2(2, 0.2f) }; edge.usedByEffector = true;
        visual.gameObject.AddComponent<PlatformEffector2D>().useOneWay = true;
    }

    static void SpringBlock(float x, float y, Transform parent)
    {
        Place(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Blocks/SpringQuestionBlock.prefab"), new Vector2(x, y), parent);
    }

    static void BouncyCloud(float x, float y, Transform parent)
    {
        var material = new PhysicsMaterial2D("Bouncy cloud") { friction = 0, bounciness = 0.9f };
        var existingMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Materials/BouncyCloud.physicsMaterial2D");
        if (existingMaterial == null) AssetDatabase.CreateAsset(material, "Assets/Materials/BouncyCloud.physicsMaterial2D");
        else { Object.DestroyImmediate(material); material = existingMaterial; }
        var cloud = Tile("Bouncy cloud", "cloud-small", new Vector2(x, y), parent, 2);
        cloud.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/lakitus-cloud.png");
        cloud.gameObject.layer = 7;
        var col = cloud.gameObject.AddComponent<BoxCollider2D>(); col.sharedMaterial = material;
        PrefabUtility.SaveAsPrefabAsset(cloud.gameObject, "Assets/Prefabs/World/BouncyCloud.prefab");
    }

    static void Finish(float x, Transform parent, GameManager session)
    {
        var pole = Tile("Finish pole", "goal_pole_green", new Vector2(x, 1), parent, 2);
        Tile("Pole top", "goal_top_green", new Vector2(x, 5), parent, 2);
        var col = pole.gameObject.AddComponent<BoxCollider2D>(); col.isTrigger = true;
        col.size = new Vector2(1, 8);
        var flag = Tile("Finish flag", "goal_flag_green", new Vector2(x - 0.5f, 4), parent, 3);
        flag.color = new Color32(255, 222, 90, 255);
    }

    static void LayoutUI(GameObject play, GameObject over, TextMeshProUGUI score, TextMeshProUGUI finalScore)
    {
        var rect = play.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero; rect.localScale = Vector3.one;
        var backdrop = over.transform.Find("Backdrop");
        if (backdrop != null) Object.DestroyImmediate(backdrop.gameObject);
        var canvas = play.GetComponentInParent<Canvas>();
        canvas.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1280, 720);
        score.fontSize = 28;
        Anchor(score.rectTransform, new Vector2(0, 1), new Vector2(150, -35), new Vector2(260, 50));
        var previousControls = play.transform.Find("Controls");
        if (previousControls != null) Object.DestroyImmediate(previousControls.gameObject);
        var restart = play.GetComponentsInChildren<Button>(true).First();
        Anchor(restart.GetComponent<RectTransform>(), Vector2.one, new Vector2(-65, -35), new Vector2(55, 55));
        over.SetActive(false);
    }

    static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor; rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
    }
}
