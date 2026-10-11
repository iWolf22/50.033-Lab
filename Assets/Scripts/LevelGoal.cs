using UnityEngine;

public class LevelGoal : MonoBehaviour
{
    [SerializeField] GameManager session;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip clearSound;
    bool reached;

    public void GameRestart() { reached = false; audioSource.Stop(); }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (reached || !other.TryGetComponent<PlayerMovement>(out var player) || !player.alive) return;
        reached = true;
        audioSource.PlayOneShot(clearSound);
        session.GameOver();
    }
}
