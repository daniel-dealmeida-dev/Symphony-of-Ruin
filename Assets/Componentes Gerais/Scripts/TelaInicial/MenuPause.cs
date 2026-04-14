using UnityEngine;

public class MenuPause : MonoBehaviour
{
    private void Awake()
    {
        GameServices.EnsureInstance();
    }

    public void TogglePause()
    {
        if (GameManager.gm == null)
        {
            return;
        }

        if (GameManager.gm.jogoPausado)
        {
            GameManager.gm.Retomar();
        }
        else
        {
            GameManager.gm.Pausar();
        }
    }
}
