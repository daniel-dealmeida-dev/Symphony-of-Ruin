using UnityEngine;

public class ConfigScript : MonoBehaviour
{
    [SerializeField] private bool bootstrapOnAwake = true;

    private void Awake()
    {
        if (!bootstrapOnAwake)
        {
            return;
        }

        GameServices.EnsureInstance();
        ResponsiveCanvasUtility.ConfigureAllCanvases();
        DontDestroyOnLoad(gameObject);
    }
}
