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
        yield return SceneManager.LoadSceneAsync("World-1-1");
        player = Object.FindFirstObjectByType<PlayerMovement>();
        body = player.GetComponent<Rigidbody2D>();
        keyboard = InputSystem.AddDevice<Keyboard>();
        InputUser.PerformPairingWithDevice(keyboard, player.GetComponent<PlayerInput>().user);
        yield return new WaitForSeconds(0.3f);
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Time.timeScale = 1;
        InputSystem.RemoveDevice(keyboard);
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
        InputSystem.settings.backgroundBehavior = previousBackground;
        if (GameManager.instance != null) Object.Destroy(GameManager.instance.gameObject);
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
        System.IO.File.WriteAllBytes("Artifacts/" + name + ".png", image.EncodeToPNG());
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
        Keys(Key.Space);
        yield return new WaitForSeconds(0.06f);
        Keys();
        yield return new WaitForSeconds(0.16f);
        float tapVelocity = body.linearVelocity.y;
        Assert.Greater(body.position.y, floor + 1);
        player.Jump();
        yield return new WaitForFixedUpdate();
        Assert.Less(body.linearVelocity.y, tapVelocity);
        yield return new WaitForSeconds(1.2f);
        Keys(Key.Space);
        yield return new WaitForSeconds(0.22f);
        Assert.Greater(body.linearVelocity.y, tapVelocity + 2);
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
        Assert.AreEqual(2, Object.FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None).Length);
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
        Assert.AreEqual("Score: 1", Score().text);
        Assert.IsFalse(coin.activeSelf);
        Assert.That(coin.transform.localPosition.y, Is.EqualTo(0).Within(0.01f));
        Teleport((Vector2)block.transform.position + Vector2.down * 2.2f, Vector2.zero);
        yield return new WaitForFixedUpdate();
        probe.contacts = "";
        Teleport((Vector2)block.transform.position + Vector2.down * 1.1f, Vector2.up * 8);
        yield return new WaitForSeconds(0.15f);
        Assert.IsFalse(coin.activeSelf);
        Assert.AreEqual("Score: 1", Score().text);
    }

    [UnityTest]
    public IEnumerator FallEndsGameAndFinishShowsCourseClear()
    {
        Teleport(new Vector2(9.5f, -13), Vector2.down);
        yield return new WaitForSecondsRealtime(1.1f);
        Assert.IsFalse(player.alive);
        Assert.AreEqual(0, Time.timeScale);
        Time.timeScale = 1;
        yield return SceneManager.LoadSceneAsync("World-1-1");
        player = Object.FindFirstObjectByType<PlayerMovement>(); body = player.GetComponent<Rigidbody2D>();
        yield return null;
        Teleport(new Vector2(20, -2.4f), Vector2.zero);
        yield return new WaitForSecondsRealtime(0.1f);
        Assert.AreEqual(0, Time.timeScale);
        Assert.IsTrue(Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(t => t.name == "ScoreText-GameOver").text.StartsWith("Score:"));
    }

    [UnityTest]
    public IEnumerator OneWayPlatformPassesUpwardAndSupportsLanding()
    {
        DisableEnemyContacts();
        Teleport(new Vector2(15, -1.3f), Vector2.up * 15);
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
        var powerup = (BasePowerup)block.Powerup;
        var start = springBody.position;
        Teleport(start + Vector2.up * 1.2f, Vector2.down * 5);
        yield return new WaitForSeconds(0.3f);
        Assert.IsFalse(powerup.gameObject.activeSelf);
        Assert.That(springBody.position.y, Is.EqualTo(start.y).Within(0.03f));
        Teleport(start + Vector2.down * 2.2f, Vector2.zero);
        yield return new WaitForFixedUpdate();
        Teleport(start + Vector2.down * 1.1f, Vector2.up * 8);
        yield return new WaitForSeconds(0.15f);
        Assert.IsTrue(powerup.gameObject.activeSelf);
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
            Assert.AreEqual(reward, block.transform.Find("Coin") != null && block.transform.Find("Coin").gameObject.activeSelf);
            Assert.Greater(block.transform.Find("Block visual").localPosition.y, 0.05f);
            Assert.IsTrue(SceneAudio.instance.blocks.isPlaying);
            yield return new WaitForSeconds(0.55f);
            Assert.IsTrue(block.gameObject.activeSelf);
            Assert.That(block.transform.Find("Block visual").localPosition.y, Is.EqualTo(0).Within(0.01f));
            Teleport((Vector2)block.transform.position + Vector2.down * 2.2f, Vector2.zero);
            yield return new WaitForFixedUpdate();
            Teleport((Vector2)block.transform.position + Vector2.down * 1.1f, Vector2.up * 8);
            yield return new WaitForSeconds(0.15f);
            Assert.IsTrue(block.transform.Find("Coin") == null || !block.transform.Find("Coin").gameObject.activeSelf);
        }
    }

    [UnityTest]
    public IEnumerator BouncyCloudReboundsAndCameraStopsAtCourseEnds()
    {
        DisableEnemyContacts();
        Teleport(new Vector2(12.5f, 1.7f), Vector2.down * 8);
        yield return new WaitForSeconds(0.16f);
        Assert.Greater(body.linearVelocity.y, 1);
        var camera = Camera.main;
        foreach (float aspect in new[] { 4f / 3, 16f / 9, 21f / 9 })
        {
            camera.aspect = aspect;
            Teleport(new Vector2(-6, -2.5f), Vector2.zero);
            yield return null;
            Assert.That(camera.transform.position.x, Is.EqualTo(0).Within(0.01f));
            Teleport(new Vector2(23, -2.5f), Vector2.zero);
            yield return null;
            Assert.That(camera.ViewportToWorldPoint(new Vector3(1, 0, 0)).x, Is.EqualTo(25).Within(0.05f));
        }
    }


    [UnityTest]
    public IEnumerator ManagerEventsRestoreSpentBlocksAndInactiveEnemiesWithoutReloading()
    {
        var manager = Object.FindFirstObjectByType<GameManager>();
        var enemyManager = Object.FindFirstObjectByType<EnemyManager>();
        var enemy = enemyManager.GetComponentsInChildren<EnemyMovement>()[0];
        var blocks = Object.FindObjectsByType<BlockBounce>(FindObjectsSortMode.None);
        var spring = blocks.First(b => b.GetComponent<SpringJoint2D>() != null);
        var question = blocks.First(b => b.isQuestionBlock && b.GetComponent<SpringJoint2D>() == null);
        var originalScene = SceneManager.GetActiveScene();
        int restarts = 0;
        manager.gameRestart.AddListener(() => restarts++);
        DisableEnemyContacts();
        enemy.Stomp();
        yield return new WaitForSeconds(0.6f);
        Assert.IsFalse(enemy.gameObject.activeSelf);
        foreach (var block in new[] { question, spring })
        {
            Teleport((Vector2)block.transform.position + Vector2.down * 2.2f, Vector2.zero);
            yield return new WaitForFixedUpdate();
            Teleport((Vector2)block.transform.position + Vector2.down * 1.1f, Vector2.up * 8);
            yield return new WaitForSeconds(0.7f);
        }
        Assert.AreEqual(2 + (spring.Powerup.powerupType == PowerupType.Coin ? 1 : 0), manager.Score);
        Assert.AreEqual(RigidbodyType2D.Static, spring.GetComponent<Rigidbody2D>().bodyType);
        manager.GameOver();
        manager.GameRestart();
        yield return null;
        Assert.AreSame(manager, Object.FindFirstObjectByType<GameManager>());
        Assert.IsTrue(originalScene == SceneManager.GetActiveScene());
        Assert.AreEqual(1, restarts);
        Assert.AreEqual(0, manager.Score);
        Assert.AreEqual("Score: 0", Score().text);
        Assert.IsTrue(player.alive);
        Assert.That(body.position.x, Is.EqualTo(-5).Within(0.01f));
        Assert.That(Camera.main.transform.position.x, Is.EqualTo(0).Within(0.01f));
        Assert.IsTrue(enemy.gameObject.activeSelf);
        Assert.IsTrue(enemy.GetComponent<Collider2D>().enabled);
        Assert.AreEqual(RigidbodyType2D.Dynamic, spring.GetComponent<Rigidbody2D>().bodyType);
        foreach (var block in new[] { question, spring })
        {
            Assert.IsFalse(((BasePowerup)block.Powerup).gameObject.activeSelf);
            Assert.IsFalse(block.Powerup.hasSpawned);
            Assert.AreNotEqual("question-mark-solid", block.GetComponentInChildren<SpriteRenderer>().sprite.name);
        }
        // Re-enabling the observer must not accumulate duplicate score subscriptions.
        for (int i = 0; i < 2; i++) { enemyManager.enabled = false; enemyManager.enabled = true; }
        enemy.Stomp();
        Assert.AreEqual(1, manager.Score);
    }

    [UnityTest]
    public IEnumerator CourseCanBeCompletedWithKeyboardMovementAndJumps()
    {
        float deadline = Time.realtimeSinceStartup + 35;
        float[] jumpPoints = { -2, 1.8f, 6.5f, 13.5f, 17.5f };
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
                releaseAt = Time.time + (nextJump <= 1 ? 0.08f : 0.3f);
                nextJump++;
            }
            yield return null;
        }
        Keys();
        Assert.IsTrue(player.alive, $"The run died at {body.position}; next jump {nextJump}.");
        Assert.Greater(body.position.x, 19, $"The run stalled at {body.position}; next jump {nextJump}.");
        Assert.AreEqual(0, Time.timeScale);
        CaptureFrame("gameplay-clear");
        yield return null;
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
