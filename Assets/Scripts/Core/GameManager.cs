using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public class GameManager : Singleton<GameManager>
{
    [SerializeField] GameScore gameScore;
    public UnityEvent gameStart = new();
    public UnityEvent gameRestart = new();
    public UnityEvent<int> scoreChange = new();
    public UnityEvent gameOver = new();
    public int Score => gameScore.CurrentScore;
    public int HighScore => gameScore.HighScore;
    public bool HasStarted { get; private set; }
    bool finished;

    void Start()
    {
        Time.timeScale = 1;
        SetScore(0);
        gameStart.Invoke();
        SceneManager.activeSceneChanged += SceneSetup;
    }

    protected override void OnDestroy()
    {
        SceneManager.activeSceneChanged -= SceneSetup;
        base.OnDestroy();
    }

    public void SceneSetup(Scene current, Scene next)
    {
        finished = false;
        Time.timeScale = 1;
        gameStart.Invoke();
        SetScore(Score);
    }

    public void IncreaseScore(int increment)
    {
        if (!finished) SetScore(Score + increment);
    }

    public void SetScore(int value)
    {
        gameScore.SetScore(value);
        scoreChange.Invoke(Score);
    }

    public void ResetHighScore()
    {
        gameScore.ResetHighScore();
        scoreChange.Invoke(Score);
    }

    public void GameOver()
    {
        if (finished) return;
        finished = true;
        Time.timeScale = 0;
        gameOver.Invoke();
    }

    public void ReturnToMenu()
    {
        GameRestart();
        HasStarted = false;
        if (SceneManager.GetActiveScene().name != "World-1-1")
            SceneManager.LoadSceneAsync("World-1-1");
        else
            gameStart.Invoke();
    }

    public void GameRestart()
    {
        HasStarted = true;
        finished = false;
        Time.timeScale = 1;
        SetScore(0);
        gameRestart.Invoke();
    }
}
