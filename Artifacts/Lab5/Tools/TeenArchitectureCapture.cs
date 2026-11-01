using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using UnityEditor;
using UnityEditor.Media;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Native Inspector and Hierarchy evidence, used only in the validation project.
[InitializeOnLoad]
public static class TeenArchitectureCapture
{
    static EditorWindow hierarchy, inspector, game;
    static TeenSourceWindow source;
    static MediaEncoder encoder;
    static Texture2D image;
    static PlayerMovement player;
    static int frames, samples;
    static double next, energy;
    static bool noPersistentGameplay = true;
    static TeenArchitectureCapture()
    {
        EditorApplication.playModeStateChanged += Changed;
        if (EditorApplication.isPlaying && SessionState.GetBool("TeenArchitectureCapture", false))
            EditorApplication.delayCall += () => Changed(PlayModeStateChange.EnteredPlayMode);
    }
    // URP's optional rendering debugger creates its own persistent engine helper.
    // Disable that debugger only for this editor evidence capture; gameplay is unchanged.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void DisableRenderingDebuggerForCapture()
    {
        if (SessionState.GetBool("TeenArchitectureCapture", false)) UnityEngine.Rendering.DebugManager.instance.enableRuntimeUI = false;
    }
    public static void Capture()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/World-1-1.unity");
        SessionState.SetBool("TeenArchitectureCapture", true);
        EditorApplication.EnterPlaymode();
    }
    static EditorWindow Window(string type, Rect rect)
    {
        var result = ScriptableObject.CreateInstance(typeof(Editor).Assembly.GetType(type)) as EditorWindow;
        result.ShowUtility(); result.position = rect; return result;
    }
    static void SelectMario()
    {
        player = Object.FindFirstObjectByType<PlayerMovement>();
        player.GetComponent<Rigidbody2D>().simulated = false;
        Selection.activeGameObject = player.gameObject;
        foreach (var component in player.GetComponents<Component>())
            InternalEditorUtility.SetIsInspectorExpanded(component, component is MarioStateController || component is BuffStateController);
        ActiveEditorTracker.sharedTracker.ForceRebuild();
        if (hierarchy != null) hierarchy.SendEvent(EditorGUIUtility.CommandEvent("FrameSelected"));
        noPersistentGameplay &= !Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).Any(c => c != null && c.GetType().Assembly == typeof(GameManager).Assembly && c.gameObject.scene.name == "DontDestroyOnLoad");
    }
    static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool("TeenArchitectureCapture", false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Object.FindFirstObjectByType<GameManager>().GameRestart(); SelectMario();
            hierarchy = Window("UnityEditor.SceneHierarchyWindow", new Rect(20, 60, 300, 800));
            game = Window("UnityEditor.GameView", new Rect(325, 60, 800, 480));
            inspector = Window("UnityEditor.InspectorWindow", new Rect(1130, 60, 520, 800));
            source = ScriptableObject.CreateInstance<TeenSourceWindow>(); source.ShowUtility(); source.position = new Rect(325, 545, 800, 315);
            source.code = File.ReadAllText("Assets/Scripts/Core/GameManager.cs");
            SelectMario();
            Directory.CreateDirectory("Artifacts/Lab5/Videos"); Directory.CreateDirectory("Artifacts/Lab5/Screenshots");
            image = new Texture2D(1640, 800, TextureFormat.RGBA32, false);
            Directory.CreateDirectory("Logs/ArchitectureFrames");
            foreach (string old in Directory.GetFiles("Logs/ArchitectureFrames", "frame-*.png")) File.Delete(old);
            Time.captureFramerate = 30; AudioRenderer.Start();
            encoder = new MediaEncoder("Artifacts/Lab5/Videos/architecture-inspector.mp4", new VideoTrackAttributes { width = 1640, height = 800, frameRate = new MediaRational(30) }, new AudioTrackAttributes { channelCount = 2, sampleRate = new MediaRational(AudioSettings.outputSampleRate), language = "" });
            next = EditorApplication.timeSinceStartup + 3; EditorApplication.update += Update;
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool("TeenArchitectureCapture", false); EditorApplication.Exit(0);
        }
    }
    static void CopyWindow(EditorWindow window, int x, int y)
    {
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        object view = typeof(EditorWindow).GetField("m_Parent", flags).GetValue(window);
        Type guiView = typeof(Editor).Assembly.GetType("UnityEditor.GUIView");
        guiView.GetMethod("RepaintImmediately", flags).Invoke(view, null);
        int width = (int)window.position.width, height = (int)window.position.height;
        float scale = (float)guiView.GetMethod("GetBackingScaleFactor", flags).Invoke(view, null);
        int pixelWidth = Mathf.RoundToInt(width * scale), pixelHeight = Mathf.RoundToInt(height * scale);
        var raw = RenderTexture.GetTemporary(pixelWidth, pixelHeight, 0, RenderTextureFormat.ARGB32);
        guiView.GetMethod("GrabPixels", flags).Invoke(view, new object[] { raw, new Rect(0, 0, pixelWidth, pixelHeight) });
        var target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(raw, target, new Vector2(1, -1), new Vector2(0, 1));
        var previous = RenderTexture.active; RenderTexture.active = target;
        var pixels = new Texture2D(width, height, TextureFormat.RGBA32, false);
        pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
        image.SetPixels(x, y, width, height, pixels.GetPixels());
        Object.DestroyImmediate(pixels); RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); RenderTexture.ReleaseTemporary(raw);
    }
    static T Powerup<T>() where T : MovingPowerup => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
    static void Update()
    {
        if (EditorApplication.timeSinceStartup < next) return;
        next = EditorApplication.timeSinceStartup + 1d / 30;
        if (frames == 60) { player.RequestPowerupEffect(Powerup<MagicMushroomPowerup>()); source.code = File.ReadAllText("Assets/Scripts/Events/GameEvent.cs"); }
        if (frames == 120) { player.RequestPowerupEffect(Powerup<FireFlowerPowerup>()); source.code = File.ReadAllText("Assets/Scripts/StateMachine/State.cs"); }
        if (frames == 180) { player.RequestPowerupEffect(Powerup<StarPowerup>()); source.code = File.ReadAllText("Assets/Scripts/StateMachine/BuffStateController.cs"); }
        if (frames == 250) { Object.FindFirstObjectByType<GameManager>().IncreaseScore(7); source.code = File.ReadAllText("Assets/Scripts/ScriptableObjects/GameScore.cs"); }
        if (frames == 280) SceneManager.LoadSceneAsync("World-1-2");
        if (frames == 310) SelectMario();
        hierarchy.Repaint(); inspector.Repaint(); game.Repaint(); source.Repaint();
        // Read Unity window framebuffers; never sample unrelated desktop windows.
        CopyWindow(hierarchy, 0, 0); CopyWindow(game, 305, 320); CopyWindow(source, 305, 0); CopyWindow(inspector, 1110, 0);
        image.Apply();
        File.WriteAllBytes("Logs/ArchitectureFrames/frame-" + frames.ToString("D4") + ".png", image.EncodeToPNG());
        encoder.AddFrame(image);
        using var buffer = new NativeArray<float>(AudioRenderer.GetSampleCountForCaptureFrame() * 2, Allocator.Temp);
        if (AudioRenderer.Render(buffer) && buffer.Length > 0) { encoder.AddSamples(buffer); foreach (float value in buffer) energy += value * value; samples += buffer.Length; }
        frames++;
        if (frames == 50 || frames == 160 || frames == 220 || frames == 330) File.WriteAllBytes("Artifacts/Lab5/Screenshots/inspector-" + frames + ".png", image.EncodeToPNG());
        if (frames < 360) return;
        EditorApplication.update -= Update;
        AudioRenderer.Stop(); encoder.Dispose(); Time.captureFramerate = 0;
        File.WriteAllText("Artifacts/Lab5/Videos/architecture-inspector.mp4.json", "{\"seconds\":12,\"frames\":" + frames + ",\"noPersistentGameplay\":" + noPersistentGameplay.ToString().ToLowerInvariant() + ",\"carriedScore\":" + Object.FindFirstObjectByType<GameManager>().Score + ",\"audioRms\":" + Math.Sqrt(energy / Math.Max(samples, 1)).ToString(System.Globalization.CultureInfo.InvariantCulture) + "}");
        hierarchy.Close(); inspector.Close(); game.Close(); source.Close(); Object.DestroyImmediate(image);
        EditorApplication.ExitPlaymode();
    }
}
public class TeenSourceWindow : EditorWindow
{
    public string code;
    Vector2 scroll;
    void OnGUI()
    {
        GUILayout.Label("Actual project source: ScriptableObject architecture", EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.SelectableLabel(code ?? "", new GUIStyle(EditorStyles.textArea) { fontSize = 13, wordWrap = false }, GUILayout.Height(1600));
        EditorGUILayout.EndScrollView();
    }
}
