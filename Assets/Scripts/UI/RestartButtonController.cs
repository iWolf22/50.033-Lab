using UnityEngine;

public class RestartButtonController : MonoBehaviour, IInteractiveButton
{
    [SerializeField] SimpleGameEvent restartRequested;
    public void ButtonClick() => restartRequested.Raise();
}
