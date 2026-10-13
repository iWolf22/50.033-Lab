using TMPro;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] TextMeshProUGUI finalScoreText;
    [SerializeField] GameObject gamePlayUI;
    [SerializeField] GameObject gameOverUI;

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
        gameOverUI.SetActive(false);
        gamePlayUI.SetActive(true);
    }

    public void SetScore(int score)
    {
        scoreText.text = finalScoreText.text = $"Score: {score}";
    }

    public void GameOver()
    {
        gamePlayUI.SetActive(false);
        gameOverUI.SetActive(true);
    }
}
