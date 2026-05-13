using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
                if (c.gameObject.name != "GameOver")
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
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.text = "Fragmentos: 0";

        // Contorno simples para leitura sobre fundos claros/escuros
        label.outlineWidth = 0.2f;
        label.outlineColor = new Color32(0, 0, 0, 200);

        // Fundo semi-transparente
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
