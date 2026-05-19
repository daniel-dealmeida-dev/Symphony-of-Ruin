using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SimpleTmpTextFallback
{
    private const string FallbackObjectNamePrefix = "__SimpleUiTextFallback_";

    private static bool _initialized;
    private static Font _fallbackFont;
    private static Material _fallbackTextMaterial;
    private static string _loggedTextShaderName;
    private static int _lastLoggedTextCount = -1;
    private static int _lastLoggedLegacyTextCount = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        SceneManager.sceneLoaded += (_, __) => Apply();

        var runner = new GameObject("SimpleTmpTextFallbackRunner");
        Object.DontDestroyOnLoad(runner);
        runner.hideFlags = HideFlags.HideAndDontSave;
        runner.AddComponent<SimpleTmpTextFallbackRunner>();

        Apply();
    }

    private static void Apply()
    {
        Font font = GetFallbackFont();
        if (font == null) return;

        var legacyTexts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int legacyTextCount = 0;
        foreach (var legacyText in legacyTexts)
        {
            if (NormalizeLegacyText(legacyText, font))
            {
                legacyTextCount++;
            }
        }

        if (legacyTextCount != _lastLoggedLegacyTextCount)
        {
            _lastLoggedLegacyTextCount = legacyTextCount;
            Debug.Log($"SimpleTmpTextFallback: normalized {legacyTextCount} legacy UI Text components.");
        }

        var tmpTexts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int convertedCount = 0;
        foreach (var tmpText in tmpTexts)
        {
            if (SyncFallbackText(tmpText, font))
            {
                convertedCount++;
            }
        }

        if (convertedCount != _lastLoggedTextCount)
        {
            _lastLoggedTextCount = convertedCount;
            Debug.Log($"SimpleTmpTextFallback: converted {convertedCount} TextMeshProUGUI components to legacy UI Text.");
        }
    }

    private static Font GetFallbackFont()
    {
        if (_fallbackFont != null) return _fallbackFont;

        _fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_fallbackFont == null)
        {
            _fallbackFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        if (_fallbackFont == null)
        {
            _fallbackFont = Font.CreateDynamicFontFromOSFont(
                new[] { "sans-serif", "Roboto", "Droid Sans", "Arial" },
                16);
        }

        if (_fallbackFont == null)
        {
            Debug.LogError("SimpleTmpTextFallback: could not load a built-in font.");
        }

        return _fallbackFont;
    }

    private static bool SyncFallbackText(TextMeshProUGUI tmpText, Font font)
    {
        if (tmpText == null) return false;

        Text fallbackText = GetOrCreateFallback(tmpText);
        if (fallbackText == null) return false;

        fallbackText.font = font;
        fallbackText.text = tmpText.text;
        fallbackText.color = tmpText.color;
        fallbackText.fontSize = Mathf.Clamp(Mathf.RoundToInt(tmpText.fontSize), 1, 300);
        fallbackText.alignment = ToTextAnchor(tmpText.alignment);
        fallbackText.supportRichText = true;
        fallbackText.raycastTarget = false;
        bool shouldWrap = tmpText.textWrappingMode != TextWrappingModes.NoWrap;
        fallbackText.horizontalOverflow = shouldWrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
        fallbackText.verticalOverflow = VerticalWrapMode.Overflow;
        fallbackText.resizeTextForBestFit = tmpText.enableAutoSizing;
        fallbackText.resizeTextMinSize = Mathf.Clamp(Mathf.RoundToInt(tmpText.fontSizeMin), 1, 300);
        fallbackText.resizeTextMaxSize = Mathf.Clamp(Mathf.RoundToInt(tmpText.fontSizeMax), fallbackText.resizeTextMinSize, 300);
        fallbackText.material = GetFallbackTextMaterial(font);
        fallbackText.SetMaterialDirty();
        fallbackText.SetVerticesDirty();

        tmpText.raycastTarget = false;
        tmpText.enabled = false;
        if (tmpText.canvasRenderer != null)
        {
            tmpText.canvasRenderer.SetAlpha(0f);
            tmpText.canvasRenderer.cull = true;
        }

        return true;
    }

    private static Material GetFallbackTextMaterial(Font font)
    {
        if (_fallbackTextMaterial != null) return _fallbackTextMaterial;

        Shader textShader =
            Shader.Find("UI/Default Font") ??
            Shader.Find("GUI/Text Shader");

        if (textShader != null)
        {
            _fallbackTextMaterial = new Material(textShader)
            {
                name = "Runtime Legacy UI Text Material"
            };

            if (font != null && font.material != null && font.material.mainTexture != null)
            {
                _fallbackTextMaterial.mainTexture = font.material.mainTexture;
            }
        }
        else if (font != null && font.material != null)
        {
            _fallbackTextMaterial = font.material;
        }

        string shaderName = _fallbackTextMaterial != null && _fallbackTextMaterial.shader != null
            ? _fallbackTextMaterial.shader.name
            : "missing";

        if (_loggedTextShaderName != shaderName)
        {
            _loggedTextShaderName = shaderName;
            Debug.Log($"SimpleTmpTextFallback: using legacy text material shader '{shaderName}'.");
        }

        return _fallbackTextMaterial;
    }

    private static bool NormalizeLegacyText(Text text, Font font)
    {
        if (text == null) return false;

        text.font = font;
        text.material = GetFallbackTextMaterial(font);
        text.supportRichText = true;
        text.raycastTarget = false;
        text.SetMaterialDirty();
        text.SetVerticesDirty();

        return true;
    }

    private static Text GetOrCreateFallback(TextMeshProUGUI tmpText)
    {
        string fallbackName = FallbackObjectNamePrefix + tmpText.GetInstanceID();
        Transform parent = tmpText.transform.parent;
        if (parent == null) return null;

        Transform existing = parent.Find(fallbackName);
        if (existing != null)
        {
            CopyRectTransform(tmpText.rectTransform, existing as RectTransform);
            existing.SetSiblingIndex(Mathf.Min(tmpText.transform.GetSiblingIndex() + 1, parent.childCount - 1));
            return existing.GetComponent<Text>() ?? existing.gameObject.AddComponent<Text>();
        }

        var fallbackObject = new GameObject(fallbackName);
        fallbackObject.layer = tmpText.gameObject.layer;
        fallbackObject.transform.SetParent(parent, false);
        fallbackObject.transform.SetSiblingIndex(Mathf.Min(tmpText.transform.GetSiblingIndex() + 1, parent.childCount - 1));

        var fallbackRect = fallbackObject.AddComponent<RectTransform>();
        CopyRectTransform(tmpText.rectTransform, fallbackRect);

        return fallbackObject.AddComponent<Text>();
    }

    private static void CopyRectTransform(RectTransform source, RectTransform target)
    {
        if (source == null || target == null) return;

        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.pivot = source.pivot;
        target.localScale = source.localScale;
        target.localRotation = source.localRotation;
    }

    private static TextAnchor ToTextAnchor(TextAlignmentOptions alignment)
    {
        string alignmentName = alignment.ToString();
        bool top = alignmentName.Contains("Top");
        bool bottom = alignmentName.Contains("Bottom");
        bool right = alignmentName.Contains("Right");
        bool left = alignmentName.Contains("Left");

        if (top)
        {
            if (right) return TextAnchor.UpperRight;
            if (left) return TextAnchor.UpperLeft;
            return TextAnchor.UpperCenter;
        }

        if (bottom)
        {
            if (right) return TextAnchor.LowerRight;
            if (left) return TextAnchor.LowerLeft;
            return TextAnchor.LowerCenter;
        }

        if (right) return TextAnchor.MiddleRight;
        if (left) return TextAnchor.MiddleLeft;
        return TextAnchor.MiddleCenter;
    }

    private sealed class SimpleTmpTextFallbackRunner : MonoBehaviour
    {
        private float _nextApplyTime;

        private void Update()
        {
            if (Time.unscaledTime < _nextApplyTime) return;

            _nextApplyTime = Time.unscaledTime + 0.25f;
            Apply();
        }
    }
}
