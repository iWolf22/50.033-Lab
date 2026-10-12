using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T instance { get; private set; }

    protected virtual void Awake()
    {
        if (instance != null && instance != this)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        instance = this as T;
        DontDestroyOnLoad(gameObject);
    }

    protected virtual void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
