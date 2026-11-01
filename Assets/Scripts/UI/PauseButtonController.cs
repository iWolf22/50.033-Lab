using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class PauseButtonController : MonoBehaviour, IInteractiveButton
{
    [SerializeField] Sprite pauseIcon;
    [SerializeField] Sprite playIcon;
    Image image;
    bool isPaused;

    void Awake()
    {
        image = GetComponent<Image>();
        ResetPause();
    }

    public void ButtonClick()
    {
        if (!gameObject.activeInHierarchy) return;
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0 : 1;
        AudioListener.pause = isPaused;
        image.sprite = isPaused ? playIcon : pauseIcon;
    }

    public void ResetPause()
    {
        isPaused = false;
        image.sprite = pauseIcon;
    }
}
