using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BackgroundMusic : MonoBehaviour
{
    AudioSource music;

    void Awake() => music = GetComponent<AudioSource>();

    void OnEnable() => GameManager.instance.gameRestart.AddListener(GameRestart);

    void OnDisable()
    {
        if (GameManager.instance != null)
            GameManager.instance.gameRestart.RemoveListener(GameRestart);
    }

    void GameRestart()
    {
        music.Stop();
        music.Play();
    }
}
