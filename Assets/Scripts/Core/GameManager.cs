using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    [SerializeField] GameScore gameScore;
    public SimpleGameEvent gameStart;
    public SimpleGameEvent gameRestart;
    public IntGameEvent scoreChange;
    public SimpleGameEvent gameOver;
    public int Score => gameScore.CurrentScore;
    public int HighScore => gameScore.HighScore;
    public bool HasStarted => gameScore.HasStarted;
    bool finished;

    void Start()
    {
        finished = false;
        Time.timeScale = 1;
        if (SceneManager.GetActiveScene().name == "World-1-2") gameScore.HasStarted = true;
        gameStart.Raise();
        scoreChange.Raise(Score);
    }

    public void IncreaseScore(int increment)
    {
        if (!finished) SetScore(Score + increment);
    }

    public void SetScore(int value)
    {
        gameScore.SetScore(value);
        scoreChange.Raise(Score);
    }

    public void ResetHighScore()
    {
        gameScore.ResetHighScore();
        scoreChange.Raise(Score);
    }

    public void GameOver()
    {
        if (finished) return;
        finished = true;
        Time.timeScale = 0;
        gameOver.Raise();
    }

    public void ReturnToMenu()
    {
        GameRestart();
        gameScore.HasStarted = false;
        if (SceneManager.GetActiveScene().name != "World-1-1")
            SceneManager.LoadSceneAsync("World-1-1");
        else
            gameStart.Raise();
    }

    public void GameRestart()
    {
        gameScore.HasStarted = true;
        finished = false;
        Time.timeScale = 1;
        SetScore(0);
        gameRestart.Raise();
    }
}
