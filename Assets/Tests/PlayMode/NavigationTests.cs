using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class NavigationTests
{
    [UnitySetUp]
    public IEnumerator Setup()
    {
        Time.timeScale = 1;
        AudioListener.pause = false;
        if (Object.FindFirstObjectByType<GameManager>() != null) Object.Destroy(Object.FindFirstObjectByType<GameManager>().gameObject);
        yield return null;
        yield return SceneManager.LoadSceneAsync("World-1-1");
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Time.timeScale = 1;
        AudioListener.pause = false;
        if (Object.FindFirstObjectByType<GameManager>() != null) Object.Destroy(Object.FindFirstObjectByType<GameManager>().gameObject);
        yield return null;
    }

    static GameObject Panel(string name) => Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
        .Single(canvas => canvas.name == "Canvas").transform.Find(name).gameObject;

    static void Click(string panel, string button)
    {
        var control = Panel(panel).GetComponentsInChildren<Button>(true).Single(b => b.name == button);
        Assert.AreEqual(1, control.onClick.GetPersistentEventCount());
        control.onClick.Invoke();
    }

    [UnityTest]
    public IEnumerator MenuLoadingBackPlayAndHomeUseWorldOneBackground()
    {
        Assert.IsTrue(Panel("MainMenu").activeInHierarchy);
        Assert.IsFalse(Panel("GamePlay").activeInHierarchy);
        Assert.AreEqual(0, Time.timeScale);
        Assert.IsTrue(AudioListener.pause);
        Click("MainMenu", "StartButton");
        Assert.IsTrue(Panel("Loading").activeInHierarchy);
        Click("Loading", "BacktoMenuButton");
        Assert.IsTrue(Panel("MainMenu").activeInHierarchy);
        yield return new WaitForSecondsRealtime(1.6f);
        Assert.IsTrue(Panel("MainMenu").activeInHierarchy, "Going back cancels the loading delay.");
        Click("MainMenu", "StartButton");
        yield return new WaitForSecondsRealtime(1.6f);
        Assert.IsTrue(Panel("GamePlay").activeInHierarchy);
        Assert.IsTrue(Object.FindFirstObjectByType<GameManager>().HasStarted);
        Assert.AreEqual(1, Time.timeScale);
        Assert.IsFalse(AudioListener.pause);
        Click("GamePlay", "HomeButton");
        Assert.AreEqual("World-1-1", SceneManager.GetActiveScene().name);
        Assert.IsTrue(Panel("MainMenu").activeInHierarchy);
        Assert.AreEqual(0, Time.timeScale);
    }

    [UnityTest]
    public IEnumerator DeathAndSecondWorldReturnToMainMenu()
    {
        Click("MainMenu", "StartButton");
        yield return new WaitForSecondsRealtime(1.6f);
        Object.FindFirstObjectByType<GameManager>().GameOver();
        Assert.IsTrue(Panel("GameOver").activeInHierarchy);
        Click("GameOver", "MainMenuButton");
        Assert.IsTrue(Panel("MainMenu").activeInHierarchy);
        Click("MainMenu", "StartButton");
        yield return new WaitForSecondsRealtime(1.6f);
        yield return SceneManager.LoadSceneAsync("World-1-2");
        yield return null;
        Assert.IsTrue(Panel("GamePlay").activeInHierarchy);
        Click("GamePlay", "HomeButton");
        float deadline = Time.realtimeSinceStartup + 10;
        while (SceneManager.GetActiveScene().name != "World-1-1" && Time.realtimeSinceStartup < deadline) yield return null;
        yield return null;
        Assert.AreEqual("World-1-1", SceneManager.GetActiveScene().name);
        Assert.IsTrue(Panel("MainMenu").activeInHierarchy);
        Assert.AreEqual(0, Object.FindFirstObjectByType<GameManager>().Score);
    }

    [UnityTest]
    public IEnumerator HighScoreSurvivesManagerRecreationAndResetsFromMenu()
    {
        var manager = Object.FindFirstObjectByType<GameManager>();
        manager.ResetHighScore();
        manager.IncreaseScore(5);
        Assert.AreEqual(5, manager.HighScore);

        yield return SceneManager.LoadSceneAsync("World-1-2");
        yield return null;
        Assert.AreEqual(5, Object.FindFirstObjectByType<GameManager>().Score);
        Assert.AreEqual("Score: 5", Panel("GamePlay").transform.Find("ScoreText").GetComponent<TextMeshProUGUI>().text);

        Object.FindFirstObjectByType<GameManager>().ReturnToMenu();
        while (SceneManager.GetActiveScene().name != "World-1-1") yield return null;
        yield return null;
        Assert.AreEqual(0, Object.FindFirstObjectByType<GameManager>().Score);
        Assert.AreEqual(5, Object.FindFirstObjectByType<GameManager>().HighScore);
        Assert.AreEqual("High Score: 5", Panel("MainMenu").transform.Find("HighScoreText").GetComponent<TextMeshProUGUI>().text);

        Click("MainMenu", "ResetHighScoreButton");
        Assert.AreEqual(0, Object.FindFirstObjectByType<GameManager>().HighScore);
        Assert.AreEqual("High Score: 0", Panel("MainMenu").transform.Find("HighScoreText").GetComponent<TextMeshProUGUI>().text);
    }
}
