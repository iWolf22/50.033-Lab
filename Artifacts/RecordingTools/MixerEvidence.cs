using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEditor;
using UnityEditor.Media;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Audio;

[InitializeOnLoad]
public static class MixerEvidence
{
    [DllImport("user32.dll")] static extern IntPtr GetActiveWindow();
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr window, IntPtr order, int x, int y, int w, int h, uint flags);
    delegate bool WindowCallback(IntPtr window, IntPtr data);
    [DllImport("user32.dll")] static extern bool EnumWindows(WindowCallback callback, IntPtr data);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr window);
    static void Raise()
    {
        uint ownProcess = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
        EnumWindows((window, data) => {
            GetWindowThreadProcessId(window, out uint process);
            if (process == ownProcess && IsWindowVisible(window)) {
                SetWindowPos(window, new IntPtr(-1), 0, 0, 0, 0, 3);
                SetForegroundWindow(window);
            }
            return true;
        }, IntPtr.Zero);
    }
    static EditorWindow mixer, inspector;
    static MediaEncoder encoder;
    static Texture2D image;
    static double next;
    static int frames;
    static bool capturedInspector;
    static MixerEvidence() { EditorApplication.playModeStateChanged += Changed; }

    public static void Capture()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        SessionState.SetBool("MixerEvidence", true);
        EditorApplication.EnterPlaymode();
    }

    static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool("MixerEvidence", false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            var asset = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/InputActions/SuperMarioBros.mixer");
            Selection.activeObject = asset.FindMatchingGroups("Enemies").First();
            mixer = ScriptableObject.CreateInstance(typeof(Editor).Assembly.GetType("UnityEditor.AudioMixerWindow")) as EditorWindow;
            mixer.ShowUtility(); mixer.position = new Rect(30, 70, 1100, 650);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            mixer.GetType().GetMethod("Init", flags).Invoke(mixer, null);
            mixer.GetType().GetField("m_Controller", flags).SetValue(mixer, asset);
            mixer.GetType().GetMethod("OnMixerControllerChanged", flags).Invoke(mixer, null);
            inspector = ScriptableObject.CreateInstance(typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow")) as EditorWindow;
            inspector.ShowUtility(); inspector.position = new Rect(1140, 70, 360, 650);
            inspector.Focus(); Raise();
            mixer.Focus(); Raise();
            Time.captureFramerate = 30;
            AudioRenderer.Start();
            image = new Texture2D(1460, 650, TextureFormat.RGBA32, false);
            encoder = new MediaEncoder("Artifacts/audio-mixer.mp4",
                new VideoTrackAttributes { width = 1460, height = 650, frameRate = new MediaRational(30) },
                new AudioTrackAttributes { channelCount = 2, sampleRate = new MediaRational(AudioSettings.outputSampleRate), language = "" });
            next = EditorApplication.timeSinceStartup + 3;
            EditorApplication.update += Update;
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool("MixerEvidence", false);
            EditorApplication.Exit(0);
        }
    }

    static void Update()
    {
        Raise();
        if (EditorApplication.timeSinceStartup < next) return;
        next = EditorApplication.timeSinceStartup + 1d / 30;
        if (!capturedInspector)
        {
            var picture = new Texture2D(360, 650, TextureFormat.RGBA32, false);
            picture.SetPixels(InternalEditorUtility.ReadScreenPixel(inspector.position.position, 360, 650));
            picture.Apply();
            File.WriteAllBytes("Artifacts/enemies-inspector.png", picture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(picture);
            capturedInspector = true;
        }
        if (frames == 30 || frames == 90 || frames == 150)
            UnityEngine.Object.FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None).First(e => e.gameObject.activeSelf).Stomp();
        image.SetPixels(0, 0, 1100, 650, InternalEditorUtility.ReadScreenPixel(mixer.position.position, 1100, 650));
        image.SetPixels(1100, 0, 360, 650, InternalEditorUtility.ReadScreenPixel(inspector.position.position, 360, 650));
        image.Apply();
        encoder.AddFrame(image);
        if (frames == 33) File.WriteAllBytes("Artifacts/audio-mixer-playing.png", image.EncodeToPNG());
        using var audio = new NativeArray<float>(AudioRenderer.GetSampleCountForCaptureFrame() * 2, Allocator.Temp);
        AudioRenderer.Render(audio);
        if (audio.Length > 0) encoder.AddSamples(audio);
        if (++frames < 240) return;
        File.WriteAllBytes("Artifacts/audio-mixer.png", image.EncodeToPNG());
        EditorApplication.update -= Update;
        AudioRenderer.Stop(); encoder.Dispose(); Time.captureFramerate = 0;
        EditorApplication.ExitPlaymode();
    }
}
