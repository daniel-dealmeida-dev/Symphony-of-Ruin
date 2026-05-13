using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Painel de créditos com autores (README) e principais pacotes Unity utilizados.
/// </summary>
public static class CreditsOverlay
{
    private static GameObject root;

    public static void Show()
    {
        if (root != null)
        {
            root.SetActive(true);
            return;
        }

        root = new GameObject("CreditsOverlayRoot");

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root.transform, false);
        RectTransform prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = Vector2.zero;
        prt.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.05f, 0.04f, 0.08f, 0.94f);

        GameObject textGo = new GameObject("CreditsText", typeof(RectTransform));
        textGo.transform.SetParent(panel.transform, false);
        RectTransform trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.06f, 0.18f);
        trt.anchorMax = new Vector2(0.94f, 0.88f);
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        TMP_Text body = textGo.AddComponent<TextMeshProUGUI>();
        body.fontSize = 28;
        body.alignment = TextAlignmentOptions.TopLeft;
        body.color = Color.white;
        body.lineSpacing = 6f;
        body.text =
            "<b>Symphony of Ruin</b>\n\n" +
            "<b>Desenvolvedores</b>\n" +
            "• Artur Kremer Theiss\n" +
            "• Daniel de Almeida\n" +
            "• Gabriel Andrade Peixer\n\n" +
            "<b>Instituição</b>\n" +
            "IFSC – Câmpus Gaspar\n\n" +
            "<b>Assets / ferramentas</b>\n" +
            "• Unity Engine\n" +
            "• TextMesh Pro\n" +
            "• Joystick Pack (Unity Asset)\n" +
            "• Pixel Adventure / Forest Tileset (conforme pacotes importados no projeto)\n\n" +
            "Música e SFX procedurais gerados em tempo de execução (sem arquivos de terceiros).";

        GameObject btnGo = new GameObject("Fechar", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(panel.transform, false);
        RectTransform br = btnGo.GetComponent<RectTransform>();
        br.anchorMin = new Vector2(0.5f, 0.06f);
        br.anchorMax = new Vector2(0.5f, 0.06f);
        br.pivot = new Vector2(0.5f, 0.5f);
        br.sizeDelta = new Vector2(420f, 96f);
        btnGo.GetComponent<Image>().color = new Color(0.35f, 0.2f, 0.45f, 1f);

        GameObject btnLabel = new GameObject("Label", typeof(RectTransform));
        btnLabel.transform.SetParent(btnGo.transform, false);
        RectTransform lr = btnLabel.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero;
        lr.offsetMax = Vector2.zero;
        TMP_Text bt = btnLabel.AddComponent<TextMeshProUGUI>();
        bt.text = "Voltar";
        bt.fontSize = 36;
        bt.alignment = TextAlignmentOptions.Center;
        bt.color = Color.white;

        Button b = btnGo.GetComponent<Button>();
        b.onClick.AddListener(() => Hide());
    }

    public static void Hide()
    {
        if (root != null)
        {
            Object.Destroy(root);
            root = null;
        }
    }
}

/// <summary>
/// HUD de fragmentos coletados durante a fase (pontuação visível e legível).
/// </summary>
[DefaultExecutionOrder(50)]
public class SymphonyScoreHud : MonoBehaviour
{
    private TMP_Text label;

    private void Awake()
    {
        BuildHud();
    }

    private void LateUpdate()
    {
        if (label == null || GameManager.gm == null)
        {
            return;
        }

        label.text = "Fragmentos: " + GameManager.gm.moedasColetadas;
    }

    private void BuildHud()
    {
        Canvas canvas = null;
        GameObject named = GameObject.Find("Canvas");
        if (named != null)
        {
            canvas = named.GetComponent<Canvas>();
        }

        if (canvas == null)
        {
            foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (c.gameObject.name != "GameOver" && c.gameObject.name != "GameOverRuntime")
                {
                    canvas = c;
                    break;
                }
            }
        }

        if (canvas == null)
        {
            return;
        }

        GameObject root = new GameObject("SymphonyScoreHud", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);

        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(24f, -24f);
        rt.sizeDelta = new Vector2(520f, 72f);

        label = root.AddComponent<TextMeshProUGUI>();
        label.fontSize = 36;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.color = new Color(1f, 0.95f, 0.85f, 1f);
        label.text = "Fragmentos: 0";

        GameObject bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(root.transform, false);
        bg.transform.SetAsFirstSibling();
        RectTransform brt = bg.GetComponent<RectTransform>();
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = new Vector2(-12f, -8f);
        brt.offsetMax = new Vector2(12f, 8f);
        Image img = bg.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.45f);
        img.raycastTarget = false;
    }
}

/// <summary>
/// Inicialização em tempo de execução: áudio global, GameManager na fase,
/// HUD de pontuação e botão de créditos no menu.
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

        TextMeshProUGUI txt = lbl.AddComponent<TextMeshProUGUI>();
        txt.text = "Créditos";
        txt.fontSize = 34;
        txt.alignment = TextAlignmentOptions.Center;
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
