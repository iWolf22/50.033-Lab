using UnityEngine;
using UnityEngine.Events;

public class AnimationEventIntTool : MonoBehaviour
{
    public int parameter = 1;
    public UnityEvent<int> useInt = new();
    public void TriggerIntEvent() => useInt.Invoke(parameter);
}
