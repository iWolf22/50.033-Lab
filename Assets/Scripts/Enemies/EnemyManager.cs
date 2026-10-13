using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] AudioSource stompAudio;
    EnemyMovement[] goombas;

    void Awake()
    {
        stompAudio = SceneAudio.instance.goombaStomp;
        goombas = GetComponentsInChildren<EnemyMovement>(true);
    }

    void OnEnable()
    {
        GameManager.instance.gameRestart.AddListener(GameRestart);
        foreach (var goomba in goombas) goomba.OnGoombaStomped += OnStomp;
    }

    void OnDisable()
    {
        if (GameManager.instance != null)
            GameManager.instance.gameRestart.RemoveListener(GameRestart);
        foreach (var goomba in goombas) goomba.OnGoombaStomped -= OnStomp;
    }

    void OnStomp()
    {
        GameManager.instance.IncreaseScore(1);
        stompAudio.PlayOneShot(stompAudio.clip);
    }

    public void GameRestart()
    {
        stompAudio.Stop();
        foreach (var goomba in goombas) goomba.GameRestart();
    }
}
