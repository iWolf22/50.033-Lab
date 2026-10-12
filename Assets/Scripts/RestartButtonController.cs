using UnityEngine;

public class RestartButtonController : MonoBehaviour, IInteractiveButton
{
    public void ButtonClick() => GameManager.instance.GameRestart();
}
