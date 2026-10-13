using UnityEngine;

[DefaultExecutionOrder(-90)]
public class SceneAudio : MonoBehaviour
{
    public static SceneAudio instance { get; private set; }
    public AudioSource playerJump;
    public AudioSource playerDeath;
    public AudioSource goombaStomp;
    public AudioSource blocks;

    void Awake() => instance = this;

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
