using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] GameManager gameManager;
    [SerializeField] AudioSource stompAudio;
    EnemyMovement[] goombas;

    void Awake() => goombas = GetComponentsInChildren<EnemyMovement>(true);

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
        gameManager.IncreaseScore(1);
        stompAudio.PlayOneShot(stompAudio.clip);
    }

    public void GameRestart()
    {
        stompAudio.Stop();
        foreach (var goomba in goombas) goomba.GameRestart();
    }
}
