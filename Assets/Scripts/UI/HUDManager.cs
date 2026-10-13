using System.Collections;
using TMPro;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] TextMeshProUGUI finalScoreText;
    [SerializeField] TextMeshProUGUI highScoreText;
    [SerializeField] GameObject gamePlayUI;
    [SerializeField] GameObject gameOverUI;
    [SerializeField] GameObject mainMenuUI;
    [SerializeField] GameObject loadingUI;
    Coroutine loading;

    void OnEnable()
    {
        GameManager.instance.gameStart.AddListener(GameStart);
        GameManager.instance.gameRestart.AddListener(GameStart);
        GameManager.instance.gameOver.AddListener(GameOver);
        GameManager.instance.scoreChange.AddListener(SetScore);
    }

    void OnDisable()
    {
        if (GameManager.instance == null) return;
        GameManager.instance.gameStart.RemoveListener(GameStart);
        GameManager.instance.gameRestart.RemoveListener(GameStart);
        GameManager.instance.gameOver.RemoveListener(GameOver);
        GameManager.instance.scoreChange.RemoveListener(SetScore);
    }

    public void GameStart()
    {
        SetPanels(GameManager.instance.HasStarted ? gamePlayUI : mainMenuUI);
        Time.timeScale = GameManager.instance.HasStarted ? 1 : 0;
        AudioListener.pause = !GameManager.instance.HasStarted;
    }

    void SetPanels(GameObject visible)
    {
        gamePlayUI.SetActive(visible == gamePlayUI);
        gameOverUI.SetActive(visible == gameOverUI);
        mainMenuUI.SetActive(visible == mainMenuUI);
        loadingUI.SetActive(visible == loadingUI);
    }

    public void ShowLoading()
    {
        if (loading != null) return;
        SetPanels(loadingUI);
        loading = StartCoroutine(BeginGame());
    }

    IEnumerator BeginGame()
    {
        yield return new WaitForSecondsRealtime(1.5f);
        loading = null;
        GameManager.instance.GameRestart();
    }

    public void ReturnToMenu()
    {
        if (loading != null) StopCoroutine(loading);
        loading = null;
        GameManager.instance.ReturnToMenu();
    }

    public void SetScore(int score)
    {
        scoreText.text = finalScoreText.text = $"Score: {score}";
        highScoreText.text = $"High Score: {GameManager.instance.HighScore}";
    }

    public void ResetHighScore()
    {
        GameManager.instance.ResetHighScore();
    }

    public void GameOver()
    {
        SetPanels(gameOverUI);
    }
}
