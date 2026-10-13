using UnityEngine;
using UnityEngine.SceneManagement;

public class NextScene : MonoBehaviour
{
    public string nextSceneName;
    bool loading;

    private void Awake()
    {
        if (TryGetComponent<SpriteRenderer>(out var visual))
            visual.enabled = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!loading && other.CompareTag("Player"))
        {
            loading = true;
            SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Single);
        }
    }

}
