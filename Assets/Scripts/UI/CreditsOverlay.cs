using TMPro;
using UnityEngine;
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
        b.onClick.AddListener(Hide);
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
