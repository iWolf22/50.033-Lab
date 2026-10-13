using UnityEngine;

[CreateAssetMenu(fileName = "GameScore", menuName = "ScriptableObjects/GameScore")]
public class GameScore : ScriptableObject
{
    [SerializeField] int highScore;
    [System.NonSerialized] int currentScore;

    public int CurrentScore => currentScore;
    public int HighScore => highScore;

    public void SetScore(int score)
    {
        currentScore = score;
        if (score > highScore) highScore = score;
    }

    public void ResetHighScore()
    {
        highScore = 0;
    }
}
