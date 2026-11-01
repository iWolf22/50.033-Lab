using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class TeenTests
{
    PlayerMovement player;
    MarioStateController mario;
    BuffStateController buff;
    GameManager manager;
    Keyboard keyboard;
    InputSettings.EditorInputBehaviorInPlayMode previousInput;
    InputSettings.BackgroundBehavior previousBackground;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        Time.timeScale = 1;
        previousInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        previousBackground = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        yield return SceneManager.LoadSceneAsync("World-1-1");
        manager = Object.FindFirstObjectByType<GameManager>(); manager.GameRestart();
        player = Object.FindFirstObjectByType<PlayerMovement>();
        mario = player.GetComponent<MarioStateController>(); buff = player.GetComponent<BuffStateController>();
        keyboard = InputSystem.AddDevice<Keyboard>();
        InputUser.PerformPairingWithDevice(keyboard, player.GetComponent<PlayerInput>().user);
        foreach (var enemy in Object.FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None)) enemy.enabled = false;
        yield return new WaitForSeconds(0.15f);
    }
    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Time.timeScale = 1; AudioListener.pause = false;
        InputSystem.RemoveDevice(keyboard);
        InputSystem.settings.editorInputBehaviorInPlayMode = previousInput;
        InputSystem.settings.backgroundBehavior = previousBackground;
        yield return null;
    }
    static T Powerup<T>() where T : BasePowerup => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
    void Move(Vector2 position, Vector2 velocity)
    {
        player.transform.position = position;
        var body = player.GetComponent<Rigidbody2D>(); body.position = position; body.linearVelocity = velocity;
        Physics2D.SyncTransforms();
    }
    IEnumerator Collect<T>(string message) where T : MovingPowerup
    {
        var powerup = Powerup<T>();
        var block = powerup.transform.parent.GetComponentInChildren<BlockBounce>();
        float below = player.IsSuperMario ? 1.7f : 1.1f;
        Move(block.transform.position + Vector3.down * below, Vector2.up * 10);
        yield return new WaitForSeconds(0.65f);
        Assert.IsTrue(powerup.hasSpawned);
        LogAssert.Expect(LogType.Log, message);
        Move(powerup.transform.position, Vector2.zero);
        yield return new WaitForSeconds(0.1f);
        Assert.IsFalse(powerup.gameObject.activeSelf);
    }
    [UnityTest]
    public IEnumerator ThreeBoxesProduceBigFireAndIndependentTimedStar()
    {
        Assert.AreEqual(1, Object.FindObjectsByType<FireFlowerPowerup>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length);
        yield return Collect<MagicMushroomPowerup>("mushroom collected");
        Assert.AreEqual(MarioForm.Super, mario.Form); Assert.IsTrue(player.IsSuperMario);
        Assert.That(player.GetComponent<BoxCollider2D>().size.y, Is.EqualTo(2));
        yield return Collect<FireFlowerPowerup>("fire flower collected");
        Assert.AreEqual(MarioForm.Fire, mario.Form);
        yield return Collect<StarPowerup>("star collected");
        Assert.IsTrue(buff.IsStarActive); Assert.AreEqual(MarioForm.Fire, mario.Form);
        Assert.IsTrue(Object.FindFirstObjectByType<SceneAudio>().starMusic.isPlaying);
        Move(new Vector2(-6, -2.45f), Vector2.zero);
        player.GetComponent<Rigidbody2D>().simulated = false;
        yield return new WaitForSeconds(8.1f);
        player.GetComponent<Rigidbody2D>().simulated = true;
        Assert.IsFalse(buff.IsStarActive); Assert.AreEqual(MarioForm.Fire, mario.Form);
        Assert.IsFalse(Object.FindFirstObjectByType<SceneAudio>().starMusic.isPlaying);
    }
    [UnityTest]
    public IEnumerator FireUsesKeyboardFacingCapLifetimeAndEnemyHit()
    {
        mario.Fire(); Assert.AreEqual(0, Object.FindObjectsByType<FireballController>(FindObjectsSortMode.None).Length);
        yield return Collect<FireFlowerPowerup>("fire flower collected");
        Move(new Vector2(-6, -2.45f), Vector2.zero);
        player.MoveCheck(-1);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Z));
        yield return null; yield return null;
        var shot = Object.FindFirstObjectByType<FireballController>(); Assert.IsNotNull(shot);
        Assert.Less(shot.GetComponent<Rigidbody2D>().linearVelocity.x, 0);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        mario.Fire(); mario.Fire(); mario.Fire();
        Assert.AreEqual(3, Object.FindObjectsByType<FireballController>(FindObjectsSortMode.None).Length);
        yield return new WaitForSeconds(2.6f);
        Assert.AreEqual(0, Object.FindObjectsByType<FireballController>(FindObjectsSortMode.None).Length);
        player.MoveCheck(1); mario.Fire();
        shot = Object.FindFirstObjectByType<FireballController>();
        var enemy = Object.FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None).First();
        var body = shot.GetComponent<Rigidbody2D>(); body.position = enemy.transform.position; shot.transform.position = body.position; body.linearVelocity = Vector2.zero; Physics2D.SyncTransforms();
        yield return new WaitForSeconds(0.1f);
        Assert.IsFalse(enemy.GetComponent<Collider2D>().enabled); Assert.AreEqual(1, manager.Score);
    }
    [UnityTest]
    public IEnumerator DamageShrinksProtectsExpiresAndThenKills()
    {
        yield return Collect<MagicMushroomPowerup>("mushroom collected");
        mario.TakeDamage(); Assert.IsTrue(player.alive); Assert.AreEqual(MarioForm.Hurt, mario.Form); Assert.IsFalse(player.IsSuperMario);
        mario.TakeDamage(); Assert.IsTrue(player.alive);
        Move(new Vector2(-6, -2.45f), Vector2.zero);
        yield return new WaitForSeconds(2.1f);
        Assert.AreEqual(MarioForm.Small, mario.Form); Assert.IsTrue(player.GetComponent<SpriteRenderer>().enabled);
        mario.TakeDamage(); Assert.IsFalse(player.alive); Assert.AreEqual(MarioForm.Dead, mario.Form);
        yield return new WaitForSecondsRealtime(1.5f);
        Assert.AreEqual(0, Time.timeScale);
        manager.GameRestart(); Assert.IsTrue(player.alive); Assert.AreEqual(MarioForm.Small, mario.Form);
    }
    [UnityTest]
    public IEnumerator StarContactDefeatsEnemyWithoutDamageAndRestartRestoresEverything()
    {
        yield return Collect<FireFlowerPowerup>("fire flower collected");
        yield return Collect<StarPowerup>("star collected");
        mario.TakeDamage(); Assert.AreEqual(MarioForm.Fire, mario.Form);
        var enemy = Object.FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None).First();
        Move(enemy.transform.position, Vector2.zero);
        yield return new WaitForSeconds(0.1f);
        Assert.IsTrue(player.alive); Assert.IsFalse(enemy.GetComponent<Collider2D>().enabled);
        mario.Fire(); manager.GameRestart(); yield return null;
        Assert.AreEqual(MarioForm.Small, mario.Form); Assert.IsFalse(buff.IsStarActive);
        Assert.AreEqual(0, manager.Score); Assert.AreEqual(0, Object.FindObjectsByType<FireballController>(FindObjectsSortMode.None).Length);
        Assert.IsTrue(enemy.GetComponent<Collider2D>().enabled); Assert.AreEqual(Color.white, player.GetComponent<SpriteRenderer>().color);
        foreach (var powerup in Object.FindObjectsByType<BasePowerup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        { Assert.IsFalse(powerup.hasSpawned); Assert.IsFalse(powerup.gameObject.activeSelf); }
        Assert.IsFalse(Object.FindFirstObjectByType<SceneAudio>().starMusic.isPlaying);
    }
    [UnityTest]
    public IEnumerator SceneChangeCarriesScoreWithNoPersistentGameplayObjectsOrStaleListeners()
    {
        var oldPlayer = player;
        manager.IncreaseScore(4);
        yield return SceneManager.LoadSceneAsync("World-1-2"); yield return null;
        Assert.IsTrue(manager == null); Assert.IsTrue(oldPlayer == null);
        manager = Object.FindFirstObjectByType<GameManager>(); Assert.AreEqual(4, manager.Score);
        Assert.AreEqual(SceneManager.GetActiveScene(), manager.gameObject.scene);
        Assert.AreEqual(0, Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).Count(c => c != null && c.GetType().Assembly == typeof(GameManager).Assembly && c.gameObject.scene.name == "DontDestroyOnLoad"));
        Object.FindFirstObjectByType<EnemyMovement>().Stomp(); Assert.AreEqual(5, manager.Score);
        manager.GameRestart(); Assert.AreEqual(0, manager.Score);
        yield return SceneManager.LoadSceneAsync("World-1-1"); yield return null;
        Object.FindFirstObjectByType<GameManager>().GameRestart();
        Object.FindFirstObjectByType<EnemyMovement>().Stomp();
        Assert.AreEqual(1, Object.FindFirstObjectByType<GameManager>().Score);
    }
}
