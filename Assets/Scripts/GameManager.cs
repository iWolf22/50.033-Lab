using UnityEngine;
using UnityEngine.Events;

[DefaultExecutionOrder(-100)]
public class GameManager : Singleton<GameManager>
{
    public UnityEvent gameStart = new();
    public UnityEvent gameRestart = new();
    public UnityEvent<int> scoreChange = new();
    public UnityEvent gameOver = new();
    public int Score { get; private set; }
    bool finished;

    void Start()
    {
        Time.timeScale = 1;
        SetScore(0);
        gameStart.Invoke();
    }

    public void IncreaseScore(int increment)
    {
        if (!finished) SetScore(Score + increment);
    }

    public void SetScore(int value)
    {
        Score = value;
        scoreChange.Invoke(Score);
    }

    public void GameOver()
    {
        if (finished) return;
        finished = true;
        Time.timeScale = 0;
        gameOver.Invoke();
    }

    public void GameRestart()
    {
        finished = false;
        Time.timeScale = 1;
        SetScore(0);
        gameRestart.Invoke();
    }
}
