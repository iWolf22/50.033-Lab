using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BackgroundMusic : MonoBehaviour
{
    AudioSource music;

    void Awake() => music = GetComponent<AudioSource>();

    void OnEnable()
    {
        GameManager.instance.gameRestart.AddListener(GameRestart);
        GameManager.instance.gameOver.AddListener(GameOver);
    }

    void OnDisable()
    {
        if (GameManager.instance != null)
            GameManager.instance.gameRestart.RemoveListener(GameRestart);
    }

    void GameOver()
    {
        music.Play();
        music.Stop();
    }

    void GameRestart()
    {
        music.Stop();
        music.Play();
    }
}
