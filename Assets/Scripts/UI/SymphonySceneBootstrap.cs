using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Inicialização em tempo de execução: áudio global, GameManager na fase,
/// HUD de pontuação, botão de créditos no menu e botão de reinício no game over.
/// </summary>
public class SymphonySceneBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnAfterSceneLoad()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        EnsureAudioManager();
        RouteMusicForScene(SceneManager.GetActiveScene().name);

        if (Object.FindFirstObjectByType<Controle>(FindObjectsInactive.Exclude) != null)
        {
            EnsureGameManager();
            EnsureScoreHud();
            EnsureGameOverRestartButton();
            TryAttachParallax();
        }

        if (SceneManager.GetActiveScene().name == "TelaInicial")
        {
            EnsureCreditsEntryButton();
        }
    }

    private static void EnsureAudioManager()
    {
        if (AudioManager.Instance != null)
        {
            return;
        }

        GameObject audioGo = new GameObject("AudioManager");
        audioGo.AddComponent<AudioManager>();
    }

    private static void RouteMusicForScene(string sceneName)
    {
        if (AudioManager.Instance == null)
        {
            return;
        }

        if (sceneName == "TelaInicial")
        {
            AudioManager.Instance.PlayTitleMusic();
        }
        else if (sceneName == "PrimeiraFase" || sceneName.Contains("Fase"))
        {
            AudioManager.Instance.PlayGameplayMusic();
        }
    }

    private static void EnsureGameManager()
    {
        if (GameManager.instance != null)
        {
            return;
        }

        GameObject go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
    }

    private static void EnsureScoreHud()
    {
        if (Object.FindFirstObjectByType<SymphonyScoreHud>(FindObjectsInactive.Include) != null)
        {
            return;
        }

        GameObject hud = new GameObject("SymphonyScoreHudHost");
        hud.AddComponent<SymphonyScoreHud>();
    }

    private static void EnsureGameOverRestartButton()
    {
        GameObject canvasGo = GameObject.Find("GameOver");
        if (canvasGo == null)
        {
            return;
        }

        if (canvasGo.transform.Find("BotaoReiniciar") != null)
        {
            return;
        }

        GameObject btnGo = new GameObject("BotaoReiniciar", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(canvasGo.transform, false);

        RectTransform rt = btnGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.35f);
        rt.anchorMax = new Vector2(0.5f, 0.35f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(440f, 100f);

        Image img = btnGo.GetComponent<Image>();
        img.color = new Color(0.4f, 0.15f, 0.2f, 0.95f);

        GameObject lbl = new GameObject("Label", typeof(RectTransform));
        lbl.transform.SetParent(btnGo.transform, false);
        RectTransform lr = lbl.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero;
        lr.offsetMax = Vector2.zero;

        TMPro.TextMeshProUGUI txt = lbl.AddComponent<TMPro.TextMeshProUGUI>();
        txt.text = "Reiniciar";
        txt.fontSize = 40;
        txt.alignment = TMPro.TextAlignmentOptions.Center;
        txt.color = Color.white;

        Button b = btnGo.GetComponent<Button>();
        b.onClick.AddListener(() =>
        {
            if (GameManager.instance != null)
            {
                GameManager.instance.ReiniciarFase();
            }
        });
    }

    private static void EnsureCreditsEntryButton()
    {
        CanvasMenuScript menu = Object.FindFirstObjectByType<CanvasMenuScript>(FindObjectsInactive.Include);
        if (menu == null)
        {
            return;
        }

        Transform host = menu.transform;
        if (host.Find("BotaoCreditosRuntime") != null)
        {
            return;
        }

        GameObject btnGo = new GameObject("BotaoCreditosRuntime", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(host, false);

        RectTransform rt = btnGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.12f);
        rt.anchorMax = new Vector2(0.5f, 0.12f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(360f, 88f);

        btnGo.GetComponent<Image>().color = new Color(0.2f, 0.25f, 0.45f, 0.92f);

        GameObject lbl = new GameObject("Label", typeof(RectTransform));
        lbl.transform.SetParent(btnGo.transform, false);
        RectTransform lr = lbl.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero;
        lr.offsetMax = Vector2.zero;

        TMPro.TextMeshProUGUI txt = lbl.AddComponent<TMPro.TextMeshProUGUI>();
        txt.text = "Créditos";
        txt.fontSize = 34;
        txt.alignment = TMPro.TextAlignmentOptions.Center;
        txt.color = Color.white;

        Button b = btnGo.GetComponent<Button>();
        b.onClick.AddListener(() => CreditsOverlay.Show());
    }

    private static void TryAttachParallax()
    {
        GameObject montanhas = GameObject.Find("Montanhas");
        if (montanhas == null || montanhas.GetComponent<ParallaxLayer>() != null)
        {
            return;
        }

        ParallaxLayer layer = montanhas.AddComponent<ParallaxLayer>();
        if (Camera.main != null)
        {
            layer.cameraTransform = Camera.main.transform;
        }

        layer.parallaxFactor = 0.32f;
    }
}
