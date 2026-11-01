using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class ScenePersistenceTests
{
    [UnitySetUp]
    public IEnumerator LoadFirstWorld()
    {
        Time.timeScale = 1;
        if (Object.FindFirstObjectByType<GameManager>() != null) Object.Destroy(Object.FindFirstObjectByType<GameManager>().gameObject);
        yield return null;
        yield return SceneManager.LoadSceneAsync("World-1-1");
        Object.FindFirstObjectByType<GameManager>().GameRestart();
        yield return new WaitForSeconds(0.1f);
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Time.timeScale = 1;
        if (Object.FindFirstObjectByType<GameManager>() != null) Object.Destroy(Object.FindFirstObjectByType<GameManager>().gameObject);
        yield return null;
    }

    static TextMeshProUGUI ScoreText() => Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None)
        .Single(text => text.name == "ScoreText");

    [UnityTest]
    public IEnumerator SharedAudioAndFlatBlocksKeepBindingsInBothWorlds()
    {
        foreach (string world in new[] { "World-1-1", "World-1-2" })
        {
            yield return SceneManager.LoadSceneAsync(world);
            yield return new WaitForSeconds(0.1f);
            var audio = Object.FindFirstObjectByType<SceneAudio>();
            Assert.IsNotNull(audio);
            Assert.AreEqual(SceneManager.GetActiveScene(), audio.gameObject.scene);
            var sources = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(8, sources.Length);
            Assert.IsTrue(sources.All(source => source.transform.IsChildOf(audio.transform)));
            Assert.AreEqual(8, sources.Select(source => (source.clip, source.outputAudioMixerGroup)).Distinct().Count());
            var player = Object.FindFirstObjectByType<PlayerMovement>();
            Assert.IsNull(player.GetComponent<AudioSource>());
            Assert.AreSame(audio.playerJump, player.marioAudio);
            Assert.AreSame(audio.playerDeath, player.deathAudio);
            foreach (var block in Object.FindObjectsByType<BlockBounce>(FindObjectsSortMode.None))
            {
                var root = block.GetComponent<SpringJoint2D>() != null ? block.transform.parent : block.transform;
                Assert.AreEqual(block.Powerup == null ? 1 : 2, root.childCount);
                Assert.IsNull(root.Find("question-mark-wrapper"));
                Assert.IsNull(root.GetComponentInChildren<AudioSource>(true));
                Assert.AreEqual("Blocks and Pipes", root.parent.name);
                if (block.Powerup is BasePowerup powerup) Assert.AreSame(root, powerup.transform.parent);
            }
            Object.FindFirstObjectByType<GameManager>().GameOver();
            Object.FindFirstObjectByType<GameManager>().GameRestart();
            yield return null;
            Assert.AreEqual(1, Time.timeScale);
            Assert.AreEqual(0, Object.FindFirstObjectByType<GameManager>().Score);
        }
    }

    [UnityTest]
    public IEnumerator PauseResumeAndRestartWorkInBothWorlds()
    {
        foreach (string world in new[] { "World-1-1", "World-1-2" })
        {
            yield return SceneManager.LoadSceneAsync(world);
            yield return new WaitForSeconds(0.1f);
            var pause = Object.FindFirstObjectByType<PauseButtonController>();
            var button = pause.GetComponent<Button>();
            var image = pause.GetComponent<Image>();
            var pauseIcon = image.sprite;
            Assert.IsInstanceOf<IInteractiveButton>(pause);
            var restart = Object.FindObjectsByType<RestartButtonController>(FindObjectsSortMode.None)
                .Single(controller => controller.name == "RestartButton");
            Assert.IsInstanceOf<IInteractiveButton>(restart);
            button.onClick.Invoke();
            Assert.AreEqual(0, Time.timeScale);
            Assert.IsTrue(AudioListener.pause);
            Assert.AreNotSame(pauseIcon, image.sprite);
            var enemy = Object.FindFirstObjectByType<EnemyMovement>();
            Vector3 position = enemy.transform.position;
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.AreEqual(position, enemy.transform.position);
            button.onClick.Invoke();
            Assert.AreEqual(1, Time.timeScale);
            Assert.IsFalse(AudioListener.pause);
            Assert.AreSame(pauseIcon, image.sprite);
            yield return new WaitForSeconds(0.1f);
            Assert.AreNotEqual(position, enemy.transform.position);
            Object.FindFirstObjectByType<GameManager>().IncreaseScore(2);
            button.onClick.Invoke();
            restart.GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(1, Time.timeScale);
            Assert.IsFalse(AudioListener.pause);
            Assert.AreEqual(0, Object.FindFirstObjectByType<GameManager>().Score);
            Assert.AreSame(pauseIcon, image.sprite);
            button.onClick.Invoke();
            Assert.AreEqual(0, Time.timeScale);
            button.onClick.Invoke();
            Object.FindFirstObjectByType<GameManager>().GameOver();
            button.onClick.Invoke();
            Assert.AreEqual(0, Time.timeScale, "Hidden pause button must not resume a finished game.");
        }
    }

    [UnityTest]
    public IEnumerator CastleTransitionRecreatesManagerAndPreservesScore()
    {
        var manager = Object.FindFirstObjectByType<GameManager>();
        var oldPlayer = Object.FindFirstObjectByType<PlayerMovement>();
        var oldHUD = Object.FindFirstObjectByType<HUDManager>();
        manager.IncreaseScore(3);
        Assert.AreEqual("Score: 3", ScoreText().text);
        var trigger = Object.FindFirstObjectByType<NextScene>().GetComponent<Collider2D>();
        var body = oldPlayer.GetComponent<Rigidbody2D>();
        body.position = trigger.bounds.center;
        body.linearVelocity = Vector2.zero;
        oldPlayer.transform.position = body.position;
        Physics2D.SyncTransforms();
        float deadline = Time.realtimeSinceStartup + 10;
        while (SceneManager.GetActiveScene().name != "World-1-2" && Time.realtimeSinceStartup < deadline)
            yield return null;
        yield return null;
        Assert.AreEqual("World-1-2", SceneManager.GetActiveScene().name);
        Assert.IsTrue(manager == null);
        manager = Object.FindFirstObjectByType<GameManager>();
        Assert.IsTrue(oldPlayer == null);
        Assert.IsTrue(oldHUD == null);
        Assert.AreEqual(3, manager.Score);
        Assert.AreEqual("Score: 3", ScoreText().text);
        Assert.AreEqual(1, Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length);
        Assert.AreEqual(0, manager.transform.childCount);
        Assert.AreEqual(SceneManager.GetActiveScene(), manager.gameObject.scene);
        Assert.IsNull(Object.FindFirstObjectByType<NextScene>());
        var player = Object.FindFirstObjectByType<PlayerMovement>();
        Assert.AreEqual(SceneManager.GetActiveScene(), player.gameObject.scene);
        Assert.That(player.transform.position.x, Is.EqualTo(-5).Within(0.05f));
        Assert.IsTrue(Object.FindFirstObjectByType<EventSystem>().isActiveAndEnabled);
    }

    [UnityTest]
    public IEnumerator SecondWorldStompAndRestartUseSceneManager()
    {
        var manager = Object.FindFirstObjectByType<GameManager>();
        manager.IncreaseScore(4);
        yield return SceneManager.LoadSceneAsync("World-1-2");
        yield return new WaitForSeconds(0.1f);
        manager = Object.FindFirstObjectByType<GameManager>();
        var enemyManager = Object.FindFirstObjectByType<EnemyManager>();
        for (int i = 0; i < 2; i++) { enemyManager.enabled = false; enemyManager.enabled = true; }
        var enemies = Object.FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None);
        Assert.AreEqual(1, enemies.Length);
        enemies[0].Stomp();
        enemies[0].Stomp();
        Assert.AreEqual(5, manager.Score);
        Assert.AreEqual("Score: 5", ScoreText().text);
        yield return new WaitForSeconds(0.6f);
        Assert.IsFalse(enemies[0].gameObject.activeSelf);
        manager.GameOver();
        var button = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(b => b.name == "RestartButton-GameOver");
        Assert.IsTrue(button.gameObject.activeInHierarchy);
        button.onClick.Invoke();
        yield return null;
        Assert.AreEqual(0, manager.Score);
        Assert.AreEqual("Score: 0", ScoreText().text);
        Assert.AreEqual(1, Time.timeScale);
        Assert.IsTrue(enemies[0].gameObject.activeSelf);
        Assert.IsTrue(enemies[0].GetComponent<Collider2D>().enabled);
        Assert.IsTrue(Object.FindFirstObjectByType<PlayerMovement>().alive);
        Assert.IsFalse(button.gameObject.activeInHierarchy);
        Assert.IsTrue(Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None)
            .Single(source => source.name == "Background Music").isPlaying);
        enemies[0].Stomp();
        Assert.AreEqual(1, manager.Score);
    }

    [UnityTest]
    public IEnumerator SecondWorldWorksStandalone()
    {
        Object.Destroy(Object.FindFirstObjectByType<GameManager>().gameObject);
        yield return null;
        yield return SceneManager.LoadSceneAsync("World-1-2");
        if (!Object.FindFirstObjectByType<GameManager>().HasStarted) Object.FindFirstObjectByType<GameManager>().GameRestart();
        yield return new WaitForSeconds(0.1f);
        Assert.IsNotNull(Object.FindFirstObjectByType<GameManager>());
        Assert.AreEqual(0, Object.FindFirstObjectByType<GameManager>().Score);
        Assert.AreEqual("Score: 0", ScoreText().text);
        Assert.AreEqual(1, Object.FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None).Length);
        var enemy = Object.FindFirstObjectByType<EnemyMovement>();
        enemy.Stomp();
        Assert.AreEqual(1, Object.FindFirstObjectByType<GameManager>().Score);
        Object.FindFirstObjectByType<GameManager>().GameRestart();
        Assert.AreEqual(0, Object.FindFirstObjectByType<GameManager>().Score);
        Assert.IsTrue(enemy.gameObject.activeSelf);
    }
}
