using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class UiImageMaterialHotfix
{
    private static bool _initialized;
    private static Material _uiMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        SceneManager.sceneLoaded += (_, __) => Apply();
        Apply();
    }

    private static void Apply()
    {
        EnsureMaterial();
        if (_uiMaterial == null) return;

        var images = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var image in images)
        {
            if (image == null) continue;

            // Force a known-good UI shader/material to avoid magenta fallback in builds.
            image.material = _uiMaterial;
            image.SetMaterialDirty();
        }
    }

    private static void EnsureMaterial()
    {
        if (_uiMaterial != null) return;

        var shader = Shader.Find("UI/Default") ?? Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogError("UiImageMaterialHotfix: could not find UI shader.");
            return;
        }

        _uiMaterial = new Material(shader)
        {
            name = "Runtime UI Default Material"
        };
    }
}
