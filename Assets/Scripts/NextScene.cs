using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NextScene : MonoBehaviour
{
    public string nextSceneName;

    private void Awake()
    {
        if (TryGetComponent<SpriteRenderer>(out var visual))
            visual.enabled = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Single);
        }
    }

}
