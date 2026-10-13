using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class PowerupTests
{
    [UnitySetUp]
    public IEnumerator LoadWorld()
    {
        Time.timeScale = 1;
        if (GameManager.instance != null) Object.Destroy(GameManager.instance.gameObject);
        yield return null;
        yield return SceneManager.LoadSceneAsync("World-1-1");
        GameManager.instance.GameRestart();
        yield return new WaitForSeconds(0.1f);
        foreach (var enemy in Object.FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None))
            enemy.GetComponent<Collider2D>().enabled = false;
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Time.timeScale = 1;
        if (GameManager.instance != null) Object.Destroy(GameManager.instance.gameObject);
        yield return null;
    }

    static T Find<T>() where T : Object => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).First();

    static void MovePlayer(Vector2 position, Vector2 velocity)
    {
        var player = Find<PlayerMovement>();
        player.GetComponent<Rigidbody2D>().position = position;
        player.GetComponent<Rigidbody2D>().linearVelocity = velocity;
        player.transform.position = position;
        Physics2D.SyncTransforms();
    }

    [UnityTest]
    public IEnumerator MushroomBoxSpawnsOnceCollectsAndRestarts()
    {
        var mushroom = Find<MagicMushroomPowerup>();
        var block = mushroom.transform.parent.GetComponentInChildren<QuestionBoxPowerupController>();
        Assert.IsInstanceOf<IPowerupController>(block);
        Assert.AreEqual(PowerupType.MagicMushroom, mushroom.powerupType);
        Assert.IsFalse(mushroom.hasSpawned);
        MovePlayer(block.transform.position + Vector3.up * 1.1f, Vector2.down * 4);
        yield return new WaitForSeconds(0.2f);
        Assert.IsFalse(mushroom.hasSpawned, "Landing on a box must not spawn its powerup.");
        MovePlayer(block.transform.position + Vector3.down * 1.1f, Vector2.up * 10);
        yield return new WaitForSeconds(0.7f);
        Assert.IsTrue(mushroom.hasSpawned);
        Assert.IsTrue(mushroom.GetComponent<Rigidbody2D>().simulated);
        Assert.AreEqual(RigidbodyType2D.Static, block.GetComponent<Rigidbody2D>().bodyType);
        Vector3 playerScale = Find<PlayerMovement>().transform.localScale;
        LogAssert.Expect(LogType.Log, "mushroom collected");
        MovePlayer(mushroom.transform.position, Vector2.zero);
        yield return new WaitForSeconds(0.1f);
        Assert.IsFalse(Find<PlayerMovement>().IsSuperMario);
        Assert.AreEqual(playerScale, Find<PlayerMovement>().transform.localScale);
        Find<PlayerMovement>().RequestPowerupEffect(mushroom);
        LogAssert.NoUnexpectedReceived();
        Assert.IsFalse(mushroom.gameObject.activeSelf);
        Assert.AreEqual(0, GameManager.instance.Score);
        GameManager.instance.GameRestart();
        yield return null;
        Assert.IsFalse(Find<PlayerMovement>().IsSuperMario);
        Assert.IsFalse(mushroom.hasSpawned);
        Assert.IsFalse(mushroom.gameObject.activeSelf);
        Assert.AreEqual(Vector3.zero, mushroom.transform.localPosition);
        Assert.AreEqual(RigidbodyType2D.Dynamic, block.GetComponent<Rigidbody2D>().bodyType);
        MovePlayer(block.transform.position + Vector3.down * 1.1f, Vector2.up * 10);
        yield return new WaitForSeconds(0.15f);
        Assert.IsTrue(mushroom.hasSpawned);
        GameManager.instance.GameRestart();
        yield return new WaitForSeconds(0.7f);
        Assert.IsFalse(mushroom.hasSpawned, "Restart during emergence cancels the spawn.");
        Assert.IsFalse(mushroom.GetComponent<Rigidbody2D>().simulated);
    }

    [UnityTest]
    public IEnumerator CoinRefactorScoresOnceAndSpentBrickBreaksThenResets()
    {
        var brick = Object.FindObjectsByType<BrickPowerupController>(FindObjectsSortMode.None).First(b => b.spawnCoin);
        var coin = brick.GetComponentInChildren<CoinPowerup>(true);
        Assert.AreEqual(PowerupType.Coin, coin.powerupType);
        MovePlayer(brick.transform.position + Vector3.down * 1.1f, Vector2.up * 10);
        yield return new WaitForSeconds(0.7f);
        Assert.AreEqual(1, GameManager.instance.Score);
        Assert.IsTrue(coin.hasSpawned);
        MovePlayer(brick.transform.position + Vector3.down * 1.1f, Vector2.up * 10);
        yield return new WaitForSeconds(0.7f);
        Assert.AreEqual(1, GameManager.instance.Score);
        Find<PlayerMovement>().BecomeSuperMario();
        MovePlayer(brick.transform.position + Vector3.down * 1.7f, Vector2.up * 10);
        yield return new WaitForSeconds(0.2f);
        Assert.IsFalse(brick.GetComponent<BoxCollider2D>().enabled);
        GameManager.instance.GameRestart();
        yield return null;
        Assert.IsTrue(brick.GetComponent<BoxCollider2D>().enabled);
        Assert.IsFalse(coin.hasSpawned);
        MovePlayer(brick.transform.position + Vector3.down * 1.1f, Vector2.up * 10);
        yield return new WaitForSeconds(0.7f);
        Assert.AreEqual(1, GameManager.instance.Score);
    }

    [UnityTest]
    public IEnumerator StarSpawnsMovesLogsOnceAndRestartsInBothWorlds()
    {
        foreach (string world in new[] { "World-1-1", "World-1-2" })
        {
            yield return SceneManager.LoadSceneAsync(world);
            yield return new WaitForSeconds(0.1f);
            var star = Find<StarPowerup>();
            var block = star.transform.parent.GetComponentInChildren<QuestionBoxPowerupController>();
            var player = Find<PlayerMovement>();
            Vector3 scale = player.transform.localScale;
            int score = GameManager.instance.Score;
            Assert.AreEqual(PowerupType.StarMan, star.powerupType);
            Assert.AreEqual("star", star.GetComponent<SpriteRenderer>().sprite.name);
            Assert.IsFalse(star.hasSpawned);
            MovePlayer(block.transform.position + Vector3.down * 1.1f, Vector2.up * 10);
            yield return new WaitForSeconds(0.7f);
            Assert.IsTrue(star.hasSpawned);
            Assert.IsTrue(star.GetComponent<Rigidbody2D>().simulated);
            Assert.Greater(star.GetComponent<Rigidbody2D>().linearVelocity.x, 0);
            LogAssert.Expect(LogType.Log, "star collected");
            MovePlayer(star.transform.position, Vector2.zero);
            yield return new WaitForSeconds(0.1f);
            Assert.IsFalse(star.gameObject.activeSelf);
            Assert.AreEqual(scale, player.transform.localScale);
            Assert.IsFalse(player.IsSuperMario);
            Assert.AreEqual(score, GameManager.instance.Score);
            player.RequestPowerupEffect(star);
            LogAssert.NoUnexpectedReceived();
            GameManager.instance.GameRestart();
            Assert.IsFalse(star.hasSpawned);
            Assert.AreEqual(Vector3.zero, star.transform.localPosition);
            Assert.IsFalse(star.GetComponent<Rigidbody2D>().simulated);
            star.SpawnPowerup();
            yield return new WaitForSeconds(0.15f);
            GameManager.instance.GameRestart();
            yield return new WaitForSeconds(0.6f);
            Assert.IsFalse(star.hasSpawned);
            Assert.IsFalse(star.gameObject.activeSelf);
        }
    }

    [UnityTest]
    public IEnumerator MushroomReversesAtWallAndSecondWorldHasIndependentPowerup()
    {
        yield return SceneManager.LoadSceneAsync("World-1-2");
        yield return new WaitForSeconds(0.1f);
        var mushroom = Find<MagicMushroomPowerup>();
        mushroom.SpawnPowerup();
        yield return new WaitForSeconds(0.55f);
        var wall = new GameObject("Test wall");
        wall.transform.position = mushroom.transform.position + Vector3.right * 0.8f;
        wall.AddComponent<BoxCollider2D>().size = new Vector2(0.2f, 5);
        Physics2D.SyncTransforms();
        yield return new WaitForSeconds(0.25f);
        Assert.Less(mushroom.GetComponent<Rigidbody2D>().linearVelocity.x, 0);
        Object.Destroy(wall);
        GameManager.instance.GameRestart();
        Assert.IsFalse(mushroom.hasSpawned);
        Assert.IsFalse(mushroom.gameObject.activeSelf);
    }
}
