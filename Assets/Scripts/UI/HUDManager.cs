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
    [SerializeField] GameScore gameScore;
    [SerializeField] SimpleGameEvent restartRequested;
    [SerializeField] SimpleGameEvent menuRequested;
    Coroutine loading;

    public void GameStart()
    {
        SetPanels(gameScore.HasStarted ? gamePlayUI : mainMenuUI);
        Time.timeScale = gameScore.HasStarted ? 1 : 0;
        AudioListener.pause = !gameScore.HasStarted;
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
        restartRequested.Raise();
    }

    public void ReturnToMenu()
    {
        if (loading != null) StopCoroutine(loading);
        loading = null;
        menuRequested.Raise();
    }

    public void SetScore(int score)
    {
        scoreText.text = finalScoreText.text = $"Score: {score}";
        highScoreText.text = $"High Score: {gameScore.HighScore}";
    }

    public void ResetHighScore()
    {
        gameScore.ResetHighScore();
        SetScore(gameScore.CurrentScore);
    }

    public void GameOver()
    {
        SetPanels(gameOverUI);
    }
}
