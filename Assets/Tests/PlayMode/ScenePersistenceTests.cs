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
        if (GameManager.instance != null) Object.Destroy(GameManager.instance.gameObject);
        yield return null;
        yield return SceneManager.LoadSceneAsync("World-1-1");
        yield return new WaitForSeconds(0.1f);
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Time.timeScale = 1;
        if (GameManager.instance != null) Object.Destroy(GameManager.instance.gameObject);
        yield return null;
    }

    static TextMeshProUGUI ScoreText() => Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None)
        .Single(text => text.name == "ScoreText");

    [UnityTest]
    public IEnumerator CastleTransitionPreservesOnlyManagerAndScore()
    {
        var manager = GameManager.instance;
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
        Assert.AreSame(manager, GameManager.instance);
        Assert.IsTrue(oldPlayer == null);
        Assert.IsTrue(oldHUD == null);
        Assert.AreEqual(3, manager.Score);
        Assert.AreEqual("Score: 3", ScoreText().text);
        Assert.AreEqual(1, Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length);
        Assert.AreEqual(0, manager.transform.childCount);
        Assert.AreEqual(1, manager.gameObject.scene.GetRootGameObjects().Count(root =>
            root.GetComponentsInChildren<MonoBehaviour>(true).Any(component =>
                component != null && component.GetType().Assembly == typeof(GameManager).Assembly)));
        Assert.IsNull(Object.FindFirstObjectByType<NextScene>());
        Assert.AreEqual(0, manager.gameRestart.GetPersistentEventCount());
        var player = Object.FindFirstObjectByType<PlayerMovement>();
        Assert.AreEqual(SceneManager.GetActiveScene(), player.gameObject.scene);
        Assert.That(player.transform.position.x, Is.EqualTo(-5).Within(0.05f));
        Assert.IsTrue(Object.FindFirstObjectByType<EventSystem>().isActiveAndEnabled);
    }

    [UnityTest]
    public IEnumerator SecondWorldStompAndRestartUsePersistentManager()
    {
        var manager = GameManager.instance;
        manager.IncreaseScore(4);
        yield return SceneManager.LoadSceneAsync("World-1-2");
        yield return new WaitForSeconds(0.1f);
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
        Object.Destroy(GameManager.instance.gameObject);
        yield return null;
        yield return SceneManager.LoadSceneAsync("World-1-2");
        yield return new WaitForSeconds(0.1f);
        Assert.IsNotNull(GameManager.instance);
        Assert.AreEqual(0, GameManager.instance.Score);
        Assert.AreEqual("Score: 0", ScoreText().text);
        Assert.AreEqual(1, Object.FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None).Length);
        var enemy = Object.FindFirstObjectByType<EnemyMovement>();
        enemy.Stomp();
        Assert.AreEqual(1, GameManager.instance.Score);
        GameManager.instance.GameRestart();
        Assert.AreEqual(0, GameManager.instance.Score);
        Assert.IsTrue(enemy.gameObject.activeSelf);
    }
}
