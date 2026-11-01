using UnityEngine;
using UnityEngine.Rendering;

static class RenderingDebugSetup
{
    // URP's runtime debugger otherwise creates a persistent [Debug Updater] object.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize() => DebugManager.instance.enableRuntimeUI = false;
}
