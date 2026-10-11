using TMPro;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] TextMeshProUGUI finalScoreText;
    [SerializeField] GameObject gamePlayUI;
    [SerializeField] GameObject gameOverUI;

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
