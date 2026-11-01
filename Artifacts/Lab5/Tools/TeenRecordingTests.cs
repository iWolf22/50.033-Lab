#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using Unity.Collections;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Focused demonstrations use setup positions to show each checkoff behavior clearly.
public class TeenRecordingTests
{
    Video video;
    TextMeshProUGUI caption;
    PlayerMovement player;
    Keyboard keyboard;
    void Caption(string text)
    {
        if (caption == null)
        {
            var go = new GameObject("Recording Caption", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(Object.FindFirstObjectByType<Canvas>().transform, false);
            caption = go.GetComponent<TextMeshProUGUI>(); caption.font = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(t => t != caption).font;
            caption.fontSize = 20; caption.alignment = TextAlignmentOptions.Center; caption.color = Color.white;
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0); rect.pivot = new Vector2(0.5f, 0); rect.anchoredPosition = new Vector2(0, 14); rect.sizeDelta = new Vector2(1000, 55);
        }
        caption.text = text;
        video?.MarkCaption(text);
    }
    IEnumerator Frames(int count)
    {
        for (int i = 0; i < count; i++) { yield return null; video.Frame(); }
    }
    void Move(Vector2 position, Vector2 velocity)
    {
        player.transform.position = position;
        var body = player.GetComponent<Rigidbody2D>(); body.position = position; body.linearVelocity = velocity; Physics2D.SyncTransforms();
    }
    IEnumerator Collect<T>(string title) where T : MovingPowerup
    {
        Caption(title);
        var powerup = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
        var box = powerup.transform.parent.GetComponentInChildren<BlockBounce>();
        Move(box.transform.position + Vector3.down * (player.IsSuperMario ? 1.7f : 1.1f), Vector2.up * 10);
        yield return Frames(23);
        Assert.IsTrue(powerup.hasSpawned);
        Move(powerup.transform.position, Vector2.zero);
        yield return Frames(12); Assert.IsFalse(powerup.gameObject.activeSelf, $"{typeof(T).Name}: player alive={player.alive}, position={player.transform.position}, powerup={powerup.transform.position}");
        Move(new Vector2(-4, -2), Vector2.zero);
        yield return Frames(35);
    }
    void Snapshot(string name)
    {
        Directory.CreateDirectory("Artifacts/Lab5/Screenshots");
        video.Snapshot("Artifacts/Lab5/Screenshots/" + name + ".png");
    }
    [UnityTest]
    public IEnumerator RecordPowerupsRestartAndSceneCarryover()
    {
        Time.timeScale = 1;
        var oldInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        var oldBackground = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        yield return SceneManager.LoadSceneAsync("World-1-1");
        var manager = Object.FindFirstObjectByType<GameManager>();
        player = Object.FindFirstObjectByType<PlayerMovement>();
        manager.ReturnToMenu();
        foreach (var enemy in Object.FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None)) enemy.enabled = false;
        keyboard = InputSystem.AddDevice<Keyboard>(); InputUser.PerformPairingWithDevice(keyboard, player.GetComponent<PlayerInput>().user);
        video = new Video("Artifacts/Lab5/Videos/powerups-and-restart.mp4");
        try
        {
            Caption("Teens checkoff : focused gameplay demonstrations"); yield return Frames(30); Snapshot("menu");
            manager.GameRestart();
            foreach (var enemy in Object.FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None)) { enemy.enabled = false; enemy.GetComponent<Collider2D>().enabled = false; }
            Caption("Start game"); yield return Frames(15);
            yield return Collect<MagicMushroomPowerup>("Mushroom: Small Mario to Big Mario");
            Assert.AreEqual(MarioForm.Super, player.GetComponent<MarioStateController>().Form); Snapshot("big-mario");
            yield return Collect<FireFlowerPowerup>("Fire Flower: Big Mario to Fire Mario");
            Assert.AreEqual(MarioForm.Fire, player.GetComponent<MarioStateController>().Form); Snapshot("fire-mario");
            Caption("Z fires in Mario's facing direction; maximum 3 fireballs"); player.MoveCheck(1); player.MoveCheck(0);
            for (int i = 0; i < 3; i++) { InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Z)); yield return Frames(4); InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return Frames(5); }
            yield return Frames(15); Snapshot("fireballs");
            Caption("Fireballs disappear after 2.5 seconds"); yield return Frames(90); Assert.AreEqual(0, Object.FindObjectsByType<FireballController>(FindObjectsSortMode.None).Length);
            yield return Collect<StarPowerup>("Star: timed invincibility; Fire Mario form stays active");
            Assert.IsTrue(player.GetComponent<BuffStateController>().IsStarActive); Snapshot("star-mario");
            var target = Object.FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None).OrderBy(e => e.transform.position.x).First();
            target.GetComponent<Collider2D>().enabled = true;
            Caption("Star contact defeats a Goomba and keeps Mario safe"); Move(target.transform.position, Vector2.zero); yield return Frames(18);
            Assert.IsTrue(player.alive); Assert.IsFalse(target.GetComponent<Collider2D>().enabled);
            Move(new Vector2(-4, -2), Vector2.zero); yield return Frames(30);
            Caption("The star buff expires; Mario keeps the Fire form"); yield return Frames(220);
            Assert.IsFalse(player.GetComponent<BuffStateController>().IsStarActive); Snapshot("star-expired");
            // Restore only the target enemy for the focused damage demonstration.
            target.GameRestart(); target.enabled = false;
            Caption("Enemy side hit: Fire to Small, with brief damage protection"); Move(target.transform.position, Vector2.zero); yield return Frames(6);
            Assert.AreEqual(MarioForm.Hurt, player.GetComponent<MarioStateController>().Form);
            Move(new Vector2(-4, -2), Vector2.zero); yield return Frames(70); Snapshot("small-after-damage");
            Caption("After protection expires, another side hit causes death"); Move(target.transform.position, Vector2.zero); yield return Frames(50);
            Assert.IsFalse(player.alive); Assert.AreEqual(0, Time.timeScale); Snapshot("game-over");
            Caption("Restart resets Mario, buffs, boxes, enemies, score and music");
            Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(b => b.name == "RestartButton-GameOver").onClick.Invoke();
            yield return Frames(30); Assert.IsTrue(player.alive); Assert.IsFalse(player.IsSuperMario); Assert.AreEqual(0, manager.Score); Snapshot("restart");
            foreach (var enemy in Object.FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None)) enemy.enabled = false;
            Caption("A stomp earns a point before entering the next world"); Move((Vector2)target.transform.position + Vector2.up * 1.1f, Vector2.down * 5); yield return Frames(20); int carriedScore = manager.Score; Assert.GreaterOrEqual(carriedScore, 1);
            var castle = Object.FindFirstObjectByType<NextScene>().GetComponent<Collider2D>(); Move(castle.bounds.center, Vector2.zero);
            for (int i = 0; i < 150 && SceneManager.GetActiveScene().name != "World-1-2"; i++) yield return Frames(1);
            Assert.AreEqual("World-1-2", SceneManager.GetActiveScene().name);
            player = Object.FindFirstObjectByType<PlayerMovement>(); manager = Object.FindFirstObjectByType<GameManager>();
            Caption("World 1-2: score carries over through shared ScriptableObject data"); yield return Frames(40); Assert.AreEqual(carriedScore, manager.Score); Snapshot("world-1-2-score");
            manager.GameRestart(); Caption("Restart also works independently in World 1-2"); yield return Frames(35); Assert.AreEqual(0, manager.Score);
        }
        finally
        {
            video.Dispose(); video = null; Time.timeScale = 1; AudioListener.pause = false;
            InputSystem.RemoveDevice(keyboard); InputSystem.settings.editorInputBehaviorInPlayMode = oldInput; InputSystem.settings.backgroundBehavior = oldBackground;
        }
    }
    [UnityTearDown]
    public IEnumerator CleanupRecording()
    {
        video?.Dispose(); video = null;
        Time.timeScale = 1; AudioListener.pause = false;
        if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        yield return null;
    }

    sealed class Video : IDisposable
    {
        readonly MediaEncoder encoder;
        readonly Texture2D image = new Texture2D(960, 540, TextureFormat.RGBA32, false);
        readonly RenderTexture target = new RenderTexture(960, 540, 24);
        readonly string path;
        int frames, samples; double energy; float peak;
        readonly List<(int frame, string text)> captions = new();
        public void MarkCaption(string text) => captions.Add((frames, text));
        public Video(string path)
        {
            this.path = path; Directory.CreateDirectory(Path.GetDirectoryName(path)); Time.captureFramerate = 30;
            Directory.CreateDirectory("Logs/TeenFrames");
            foreach (string old in Directory.GetFiles("Logs/TeenFrames", "frame-*.png")) File.Delete(old);
            encoder = new MediaEncoder(path, new VideoTrackAttributes { frameRate = new MediaRational(30), width = 960, height = 540 }, new AudioTrackAttributes { sampleRate = new MediaRational(AudioSettings.outputSampleRate), channelCount = 2, language = "" });
            if (!AudioRenderer.Start()) throw new Exception("Audio capture failed.");
        }
        public void Frame()
        {
            var camera = Camera.main; var canvas = Object.FindFirstObjectByType<Canvas>(); if (!camera || !canvas) return;
            var mode = canvas.renderMode; var oldCamera = canvas.worldCamera;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1; Canvas.ForceUpdateCanvases();
            var previous = RenderTexture.active;
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target }); RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); image.Apply();
            File.WriteAllBytes("Logs/TeenFrames/frame-" + frames.ToString("D4") + ".png", image.EncodeToPNG());
            if (!encoder.AddFrame(image)) throw new Exception("Video frame failed.");
            RenderTexture.active = previous; canvas.renderMode = mode; canvas.worldCamera = oldCamera;
            using var buffer = new NativeArray<float>(AudioRenderer.GetSampleCountForCaptureFrame() * 2, Allocator.Temp);
            if (!AudioRenderer.Render(buffer) || (buffer.Length > 0 && !encoder.AddSamples(buffer))) throw new Exception("Audio frame failed.");
            foreach (float value in buffer) { energy += value * value; peak = Mathf.Max(peak, Mathf.Abs(value)); } samples += buffer.Length; frames++;
        }
        public void Snapshot(string file) => File.WriteAllBytes(file, image.EncodeToPNG());
        public void Dispose()
        {
            AudioRenderer.Stop(); encoder.Dispose(); Time.captureFramerate = 0; Object.Destroy(image); target.Release(); Object.Destroy(target);
            string subtitles = "WEBVTT\n\n";
            for (int i = 0; i < captions.Count; i++)
            {
                string begin = TimeSpan.FromSeconds(captions[i].frame / 30d).ToString(@"hh\:mm\:ss\.fff");
                string end = TimeSpan.FromSeconds((i + 1 < captions.Count ? captions[i + 1].frame : frames) / 30d).ToString(@"hh\:mm\:ss\.fff");
                subtitles += begin + " --> " + end + "\n" + captions[i].text + "\n\n";
            }
            File.WriteAllText(path + ".vtt", subtitles);
            File.WriteAllText(path + ".json", JsonUtility.ToJson(new Evidence { frames = frames, seconds = frames / 30f, samples = samples, rms = (float)Math.Sqrt(energy / Math.Max(samples, 1)), peak = peak }, true));
        }
        [Serializable] class Evidence { public int frames, samples; public float seconds, rms, peak; }
    }
}
#endif
