using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class CourseTests
{
    RecordingPump recorder;
    PlayerMovement player;
    Rigidbody2D body;
    Keyboard keyboard;
    InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
    InputSettings.BackgroundBehavior previousBackground;

    [UnitySetUp]
    public IEnumerator LoadCourse()
    {
        Time.timeScale = 1;
        previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        previousBackground = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        yield return SceneManager.LoadSceneAsync("SampleScene");
        player = Object.FindFirstObjectByType<PlayerMovement>();
        body = player.GetComponent<Rigidbody2D>();
        keyboard = InputSystem.AddDevice<Keyboard>();
        InputUser.PerformPairingWithDevice(keyboard, player.GetComponent<PlayerInput>().user);
        yield return new WaitForSeconds(0.3f);
        string testName = TestContext.CurrentContext.Test.Name;
        if (testName != "CourseCanBeCompletedWithKeyboardMovementAndJumps")
        {
            recorder = new GameObject("Recording evidence").AddComponent<RecordingPump>();
            Object.DontDestroyOnLoad(recorder.gameObject);
            recorder.recording = new CourseRecording("Artifacts/Lab3/Videos/" + testName + ".mp4");
        }
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (recorder)
        {
            recorder.enabled = false;
            recorder.recording.Dispose();
            Object.Destroy(recorder.gameObject);
        }
        Time.timeScale = 1;
        InputSystem.RemoveDevice(keyboard);
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
        InputSystem.settings.backgroundBehavior = previousBackground;
        yield return null;
    }

    void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));

    void Teleport(Vector2 position, Vector2 velocity)
    {
        player.transform.position = position;
        body.position = position;
        body.linearVelocity = velocity;
        Physics2D.SyncTransforms();
    }

    static void CaptureFrame(string name)
    {
        var camera = Camera.main;
        var canvas = Object.FindFirstObjectByType<Canvas>();
        var previousMode = canvas.renderMode;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera; canvas.planeDistance = 1;
        Canvas.ForceUpdateCanvases();
        var target = RenderTexture.GetTemporary(1280, 720, 24);
        var previousTarget = RenderTexture.active;
        camera.targetTexture = target;
        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
        RenderTexture.active = target;
        var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
        System.IO.Directory.CreateDirectory("Artifacts/Lab3/Screenshots");
        System.IO.File.WriteAllBytes("Artifacts/Lab3/Screenshots/" + name + ".png", image.EncodeToPNG());
        Object.Destroy(image); camera.targetTexture = null;
        RenderTexture.active = previousTarget; RenderTexture.ReleaseTemporary(target);
        canvas.renderMode = previousMode;
    }

    static void DisableEnemyContacts()
    {
        foreach (var enemy in Object.FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None))
            enemy.GetComponent<Collider2D>().enabled = false;
    }

    static TextMeshProUGUI Score() => Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None)
        .First(t => t.name == "ScoreText");

    [UnityTest]
    public IEnumerator KeyboardMovesReversesAndStops()
    {
        float start = body.position.x;
        Keys(Key.D);
        yield return new WaitForSeconds(0.4f);
        Assert.Greater(body.position.x, start + 1);
        Assert.LessOrEqual(body.linearVelocity.x, player.maxSpeed + 0.01f);
        Keys(Key.A);
        yield return new WaitForSeconds(0.25f);
        Assert.Less(body.linearVelocity.x, 0);
        Assert.IsTrue(player.GetComponent<SpriteRenderer>().flipX);
        Keys();
        yield return new WaitForSeconds(0.25f);
        Assert.That(body.linearVelocity.x, Is.EqualTo(0).Within(0.05f));
    }

    [UnityTest]
    public IEnumerator TapAndHoldJumpHaveDifferentHeightsWithoutDoubleJump()
    {
        float floor = body.position.y;
        float tapPeak = floor;
        Keys(Key.Space);
        float started = Time.time;
        bool released = false;
        while (Time.time - started < 1.2f)
        {
            if (!released && Time.time - started > 0.06f) { Keys(); released = true; }
            tapPeak = Mathf.Max(tapPeak, body.position.y);
            yield return null;
        }
        Assert.Greater(tapPeak, floor + 1);
        Keys(Key.Space);
        float holdPeak = floor;
        started = Time.time;
        while (Time.time - started < 1.2f)
        {
            holdPeak = Mathf.Max(holdPeak, body.position.y);
            yield return null;
        }
        Keys();
        Assert.Greater(holdPeak, tapPeak + 0.1f, "Held jump should rise higher than a tap.");
    }

    [UnityTest]
    public IEnumerator StompAnimatesScoresOnceAndBounces()
    {
        var enemy = Object.FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None).OrderBy(e => e.transform.position.x).First();
        enemy.enabled = false;
        int notifications = 0;
        enemy.OnGoombaStomped += () => notifications++;
        Teleport((Vector2)enemy.transform.position + Vector2.up * 1.1f, Vector2.down * 5);
        yield return new WaitForSeconds(0.08f);
        Assert.IsTrue(player.alive);
        Assert.Greater(body.linearVelocity.y, 0);
        Assert.AreEqual("Score: 1", Score().text);
        Assert.AreEqual("dead-goombas", enemy.GetComponent<SpriteRenderer>().sprite.name);
        enemy.Stomp();
        Assert.AreEqual(1, notifications);
        yield return new WaitForSeconds(0.6f);
        Assert.IsFalse(enemy.gameObject.activeSelf);
    }

    [UnityTest]
    public IEnumerator JumpingOverGoombaDoesNotScore()
    {
        var enemy = Object.FindFirstObjectByType<EnemyMovement>();
        Teleport((Vector2)enemy.transform.position + Vector2.up * 3, Vector2.zero);
        yield return new WaitForSeconds(0.1f);
        Assert.AreEqual("Score: 0", Score().text);
    }

    [UnityTest]
    public IEnumerator SideHitDiesThenRestartRestoresCourse()
    {
        var enemy = Object.FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None).OrderBy(e => e.transform.position.x).First();
        enemy.enabled = false;
        Teleport((Vector2)enemy.transform.position + Vector2.left * 0.9f, Vector2.right * 2);
        yield return new WaitForSecondsRealtime(1.1f);
        Assert.IsFalse(player.alive);
        Assert.AreEqual(0, Time.timeScale);
        var restart = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(b => b.name == "RestartButton-GameOver");
        Assert.IsTrue(restart.gameObject.activeInHierarchy);
        restart.onClick.Invoke();
        yield return null;
        yield return new WaitForSecondsRealtime(0.3f);
        player = Object.FindFirstObjectByType<PlayerMovement>();
        Assert.IsTrue(player.alive);
        Assert.AreEqual(1, Time.timeScale);
        Assert.AreEqual("Score: 0", Score().text);
        Assert.AreEqual(6, Object.FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None).Length);
    }

    [UnityTest]
    public IEnumerator QuestionBlockRejectsTopHitAndDispensesOnlyOnceFromBelow()
    {
        DisableEnemyContacts();
        var block = Object.FindObjectsByType<BlockBounce>(FindObjectsSortMode.None)
            .Where(b => b.isQuestionBlock && b.GetComponent<Rigidbody2D>() == null).OrderBy(b => b.transform.position.x).First();
        var probe = block.gameObject.AddComponent<BlockContactProbe>();
        var visual = block.transform.Find("Block visual").GetComponent<SpriteRenderer>();
        var coin = block.transform.Find("Coin").gameObject;
        Teleport((Vector2)block.transform.position + Vector2.up * 1.2f, Vector2.down * 5);
        yield return new WaitForSeconds(0.2f);
        Assert.IsFalse(coin.activeSelf);
        Assert.AreNotEqual("question-mark-solid", visual.sprite.name);
        Teleport((Vector2)block.transform.position + Vector2.down * 2.2f, Vector2.zero);
        yield return new WaitForFixedUpdate();
        probe.contacts = "";
        Teleport((Vector2)block.transform.position + Vector2.down * 1.1f, Vector2.up * 8);
        yield return new WaitForSeconds(0.15f);
        Assert.IsTrue(coin.activeSelf, $"Contacts: {probe.contacts}; player {body.position}, velocity {body.linearVelocity}, alive {player.alive}");
        Assert.AreEqual("question-mark-solid", visual.sprite.name);
        yield return new WaitForSeconds(0.55f);
        Assert.IsFalse(coin.activeSelf);
        Assert.That(coin.transform.localPosition.y, Is.EqualTo(0).Within(0.01f));
        Teleport((Vector2)block.transform.position + Vector2.down * 2.2f, Vector2.zero);
        yield return new WaitForFixedUpdate();
        probe.contacts = "";
        Teleport((Vector2)block.transform.position + Vector2.down * 1.1f, Vector2.up * 8);
        yield return new WaitForSeconds(0.15f);
        Assert.IsFalse(coin.activeSelf);
    }

    [UnityTest]
    public IEnumerator FallEndsGameAndFinishShowsCourseClear()
    {
        Teleport(new Vector2(27.5f, -13), Vector2.down);
        yield return new WaitForSecondsRealtime(1.1f);
        Assert.IsFalse(player.alive);
        Assert.AreEqual(0, Time.timeScale);
        Time.timeScale = 1;
        yield return SceneManager.LoadSceneAsync("SampleScene");
        player = Object.FindFirstObjectByType<PlayerMovement>(); body = player.GetComponent<Rigidbody2D>();
        yield return null;
        Teleport(new Vector2(101, -2.4f), Vector2.zero);
        yield return new WaitForSecondsRealtime(0.1f);
        Assert.AreEqual(0, Time.timeScale);
        Assert.IsTrue(Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(t => t.name == "ScoreText-GameOver").text.Contains("Course clear!"));
    }

    [UnityTest]
    public IEnumerator OneWayPlatformPassesUpwardAndSupportsLanding()
    {
        DisableEnemyContacts();
        Teleport(new Vector2(58, -1.3f), Vector2.up * 15);
        yield return new WaitForSeconds(0.2f);
        Assert.Greater(body.position.y, 0.3f, "The underside must allow ascent.");
        yield return new WaitForSeconds(0.9f);
        Assert.That(body.position.y, Is.EqualTo(0.7f).Within(0.12f));
        player.Jump();
        yield return new WaitForFixedUpdate();
        Assert.Greater(body.linearVelocity.y, 10, "Landing on the platform must restore jumping.");
    }

    [UnityTest]
    public IEnumerator SpringQuestionBlockIsStableOnTopAndLocksAfterReward()
    {
        DisableEnemyContacts();
        var block = Object.FindObjectsByType<BlockBounce>(FindObjectsSortMode.None)
            .First(b => b.GetComponent<SpringJoint2D>() != null);
        var springBody = block.GetComponent<Rigidbody2D>();
        var coin = block.transform.Find("Coin").gameObject;
        var start = springBody.position;
        Teleport(start + Vector2.up * 1.2f, Vector2.down * 5);
        yield return new WaitForSeconds(0.3f);
        Assert.IsFalse(coin.activeSelf);
        Assert.That(springBody.position.y, Is.EqualTo(start.y).Within(0.03f));
        Teleport(start + Vector2.down * 2.2f, Vector2.zero);
        yield return new WaitForFixedUpdate();
        Teleport(start + Vector2.down * 1.1f, Vector2.up * 8);
        yield return new WaitForSeconds(0.15f);
        Assert.IsTrue(coin.activeSelf);
        yield return new WaitForSeconds(0.6f);
        Assert.AreEqual(RigidbodyType2D.Static, springBody.bodyType);
    }

    [UnityTest]
    public IEnumerator BrickVariantsBounceWithoutBreakingAndCoinIsSingleUse()
    {
        DisableEnemyContacts();
        var blocks = Object.FindObjectsByType<BlockBounce>(FindObjectsSortMode.None)
            .Where(b => !b.isQuestionBlock).ToArray();
        foreach (bool reward in new[] { false, true })
        {
            var block = blocks.First(b => b.spawnCoin == reward);
            Teleport((Vector2)block.transform.position + Vector2.down * 2.2f, Vector2.zero);
            yield return new WaitForFixedUpdate();
            Teleport((Vector2)block.transform.position + Vector2.down * 1.1f, Vector2.up * 8);
            yield return new WaitForSeconds(0.15f);
            Assert.AreEqual(reward, block.transform.Find("Coin").gameObject.activeSelf);
            Assert.Greater(block.transform.Find("Block visual").localPosition.y, 0.05f);
            Assert.IsTrue(block.GetComponentInChildren<AudioSource>().isPlaying);
            yield return new WaitForSeconds(0.55f);
            Assert.IsTrue(block.gameObject.activeSelf);
            Assert.That(block.transform.Find("Block visual").localPosition.y, Is.EqualTo(0).Within(0.01f));
            Teleport((Vector2)block.transform.position + Vector2.down * 2.2f, Vector2.zero);
            yield return new WaitForFixedUpdate();
            Teleport((Vector2)block.transform.position + Vector2.down * 1.1f, Vector2.up * 8);
            yield return new WaitForSeconds(0.15f);
            Assert.IsFalse(block.transform.Find("Coin").gameObject.activeSelf);
        }
    }

    [UnityTest]
    public IEnumerator BouncyCloudReboundsAndCameraStopsAtCourseEnds()
    {
        DisableEnemyContacts();
        Teleport(new Vector2(77, 1.7f), Vector2.down * 8);
        yield return new WaitForSeconds(0.16f);
        Assert.Greater(body.linearVelocity.y, 1);
        var camera = Camera.main;
        foreach (float aspect in new[] { 4f / 3, 16f / 9, 21f / 9 })
        {
            camera.aspect = aspect;
            Teleport(new Vector2(-9, -2.5f), Vector2.zero);
            yield return null;
            Assert.That(camera.transform.position.x, Is.EqualTo(0).Within(0.01f));
            Teleport(new Vector2(98, -2.5f), Vector2.zero);
            yield return null;
            Assert.That(camera.ViewportToWorldPoint(new Vector3(1, 0, 0)).x, Is.EqualTo(110).Within(0.05f));
        }
    }


    [UnityTest]
    public IEnumerator CourseCanBeCompletedWithKeyboardMovementAndJumps()
    {
        using var recording = new CourseRecording("Artifacts/Lab3/Videos/course-run.mp4");
        float deadline = Time.realtimeSinceStartup + 120;
        float[] jumpPoints = { 0, 6, 10.5f, 18.5f, 24, 31, 40.5f, 46.5f, 59.5f, 66, 69.5f, 78.7f, 85, 91.5f, 98 };
        int nextJump = 0;
        float releaseAt = 0;
        bool holdingJump = false;
        Keys(Key.D);
        System.IO.Directory.CreateDirectory("Artifacts");
        CaptureFrame("gameplay-start");
        while (Time.timeScale > 0 && player.alive && Time.realtimeSinceStartup < deadline)
        {
            if (holdingJump && Time.time >= releaseAt)
            {
                holdingJump = false;
                Keys(Key.D);
            }
            if (!holdingJump && nextJump < jumpPoints.Length && body.position.x >= jumpPoints[nextJump] &&
                Mathf.Abs(body.linearVelocity.y) < 0.15f)
            {
                Keys(Key.D, Key.Space);
                holdingJump = true;
                releaseAt = Time.time + (nextJump == 4 || nextJump == 8 ? 0.08f : 0.3f);
                nextJump++;
            }
            yield return null;
            recording.Frame();
        }
        Keys();
        Assert.IsTrue(player.alive, $"The run died at {body.position}; next jump {nextJump}.");
        Assert.Greater(body.position.x, 100, $"The run stalled at {body.position}; next jump {nextJump}.");
        Assert.AreEqual(0, Time.timeScale);
        CaptureFrame("gameplay-clear");
        for (int i = 0; i < 240; i++) { yield return null; recording.Frame(); }
    }

}

public class BlockContactProbe : MonoBehaviour
{
    public string contacts;
    void OnCollisionEnter2D(Collision2D collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
            contacts += $"{collision.gameObject.name} {collision.GetContact(i).normal} at {collision.transform.position}; ";
    }
}
