using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] AudioSource stompAudio;
    [SerializeField] IntGameEvent scoreRequested;
    EnemyMovement[] goombas;

    void Awake()
    {
        stompAudio = FindFirstObjectByType<SceneAudio>().goombaStomp;
        goombas = GetComponentsInChildren<EnemyMovement>(true);
    }

    void OnEnable()
    {
        foreach (var goomba in goombas) goomba.OnGoombaStomped += OnStomp;
    }

    void OnDisable()
    {
        foreach (var goomba in goombas) goomba.OnGoombaStomped -= OnStomp;
    }

    void OnStomp()
    {
        scoreRequested.Raise(1);
        stompAudio.PlayOneShot(stompAudio.clip);
    }

    public void GameRestart()
    {
        stompAudio.Stop();
        foreach (var goomba in goombas) goomba.GameRestart();
    }
}
