using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BackgroundMusic : MonoBehaviour
{
    AudioSource music;

    void Awake() => music = GetComponent<AudioSource>();

    public void GameOver()
    {
        music.Play();
        music.Stop();
    }

    public void GameRestart()
    {
        music.Stop();
        music.Play();
    }
}
