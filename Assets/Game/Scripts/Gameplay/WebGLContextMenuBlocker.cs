using System.Runtime.InteropServices;
using UnityEngine;

public static class WebGLContextMenuBlocker
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void DisableContextMenu();
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        DisableContextMenu();
#endif
    }
}
