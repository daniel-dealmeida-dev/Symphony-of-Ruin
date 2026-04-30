using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SpriteScaleTuningSceneController : MonoBehaviour
{
    private const string ReferenceResourcePath = "SpritsProtagoniista/PlayerBodyConsistent_v1/sheets/player_walk_sheet_416x288";
    private const string DefaultReferenceSpriteName = "player_walk_01";
    private const string ExportFolderAssetPath = "Resources/SpritsProtagoniista/ScaleCalibrationExports";
    private const int FrameWidthPixels = 416;
    private const int FrameHeightPixels = 288;
    private const int DefaultBaselinePixelY = 260;

    private readonly List<PlayerAttackSpriteVersion> sourceOptions = new List<PlayerAttackSpriteVersion>();
    private readonly int[] attackRowCounts = { 11, 9, 11, 13 };

    private Camera previewCamera;
    private SpriteRenderer referenceRenderer;
    private SpriteRenderer targetRenderer;
    private LineRenderer baselineRenderer;
    private Dropdown sourceDropdown;
    private Dropdown frameDropdown;
    private Dropdown referenceDropdown;
    private Slider scaleSlider;
    private Slider offsetXSlider;
    private Slider offsetYSlider;
    private Slider zoomSlider;
    private Toggle showReferenceToggle;
    private Toggle approvedToggle;
    private Text scaleValueLabel;
    private Text offsetXValueLabel;
    private Text offsetYValueLabel;
    private Text statusLabel;
    private Text frameTitleLabel;

    private Sprite[] sourceSprites = new Sprite[0];
    private Sprite[] referenceSprites = new Sprite[0];
    private SpriteScaleTuningFrameValue[] frameValues = new SpriteScaleTuningFrameValue[0];
    private int currentSourceIndex;
    private int currentFrameIndex;
    private int currentReferenceIndex;
    private bool updatingUi;
    private string lastExportPath = string.Empty;

    private void Awake()
    {
        EnsureEventSystem();
        BuildPreviewWorld();
        BuildUi();
        LoadSources();
        LoadReferenceSprites();
        SelectSource(PlayerAttackSpriteVersions.DefaultVersionId);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            ChangeFrame(1);
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            ChangeFrame(-1);
        }

        if (Input.GetKey(KeyCode.Q))
        {
            NudgeScale(-Time.deltaTime * 0.25f);
        }

        if (Input.GetKey(KeyCode.E))
        {
            NudgeScale(Time.deltaTime * 0.25f);
        }

        float offsetSpeed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 80f : 30f;
        if (Input.GetKey(KeyCode.A))
        {
            NudgeOffset(-offsetSpeed * Time.deltaTime, 0f);
        }

        if (Input.GetKey(KeyCode.D))
        {
            NudgeOffset(offsetSpeed * Time.deltaTime, 0f);
        }

        if (Input.GetKey(KeyCode.W))
        {
            NudgeOffset(0f, offsetSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.S))
        {
            NudgeOffset(0f, -offsetSpeed * Time.deltaTime);
        }
    }

    private void LoadSources()
    {
        sourceOptions.Clear();
        foreach (PlayerAttackSpriteVersion version in PlayerAttackSpriteVersions.All)
        {
            sourceOptions.Add(version);
        }

        sourceDropdown.options.Clear();
        foreach (PlayerAttackSpriteVersion version in sourceOptions)
        {
            sourceDropdown.options.Add(new Dropdown.OptionData(version.DisplayName));
        }
    }

    private void LoadReferenceSprites()
    {
        referenceSprites = Resources.LoadAll<Sprite>(ReferenceResourcePath);
        Array.Sort(referenceSprites, CompareSpritesByName);

        referenceDropdown.options.Clear();
        for (int index = 0; index < referenceSprites.Length; index++)
        {
            referenceDropdown.options.Add(new Dropdown.OptionData(referenceSprites[index].name));
        }

        currentReferenceIndex = FindDefaultReferenceSpriteIndex();

        if (referenceSprites.Length == 0)
        {
            SetStatus("Referencia de caminhada nao encontrada em Resources/" + ReferenceResourcePath);
        }
    }

    private void SelectSource(string versionId)
    {
        int selectedIndex = 0;
        for (int index = 0; index < sourceOptions.Count; index++)
        {
            if (sourceOptions[index].Id == versionId)
            {
                selectedIndex = index;
                break;
            }
        }

        updatingUi = true;
        sourceDropdown.value = selectedIndex;
        sourceDropdown.RefreshShownValue();
        updatingUi = false;
        LoadSelectedSource(selectedIndex);
    }

    private void LoadSelectedSource(int selectedIndex)
    {
        if (selectedIndex < 0 || selectedIndex >= sourceOptions.Count)
        {
            return;
        }

        currentSourceIndex = selectedIndex;
        PlayerAttackSpriteVersion version = sourceOptions[currentSourceIndex];
        sourceSprites = Resources.LoadAll<Sprite>(version.ResourcePath);
        Array.Sort(sourceSprites, CompareSpritesByName);

        frameValues = new SpriteScaleTuningFrameValue[sourceSprites.Length];
        for (int index = 0; index < sourceSprites.Length; index++)
        {
            int row;
            int column;
            GetAttackRowAndColumn(index, out row, out column);
            frameValues[index] = new SpriteScaleTuningFrameValue
            {
                frameName = sourceSprites[index].name,
                frameIndex = index,
                row = row,
                column = column,
                scale = 1f,
                offsetXPixels = 0f,
                offsetYPixels = 0f,
                approved = false
            };
        }

        PopulateFrameDropdown();
        currentFrameIndex = 0;
        RefreshAllUiFromState();
        SetStatus("Editando " + version.DisplayName + ". Ajuste escala/posicao e exporte o JSON.");
    }

    private void PopulateFrameDropdown()
    {
        frameDropdown.options.Clear();
        for (int index = 0; index < sourceSprites.Length; index++)
        {
            frameDropdown.options.Add(new Dropdown.OptionData(GetFrameLabel(index)));
        }

        frameDropdown.RefreshShownValue();
    }

    private void RefreshAllUiFromState()
    {
        updatingUi = true;
        if (sourceSprites.Length > 0)
        {
            currentFrameIndex = Mathf.Clamp(currentFrameIndex, 0, sourceSprites.Length - 1);
            frameDropdown.value = currentFrameIndex;
            frameDropdown.RefreshShownValue();
        }

        if (referenceSprites.Length > 0)
        {
            currentReferenceIndex = Mathf.Clamp(currentReferenceIndex, 0, referenceSprites.Length - 1);
            referenceDropdown.value = currentReferenceIndex;
            referenceDropdown.RefreshShownValue();
        }

        SpriteScaleTuningFrameValue value = GetCurrentValue();
        if (value != null)
        {
            scaleSlider.value = value.scale;
            offsetXSlider.value = value.offsetXPixels;
            offsetYSlider.value = value.offsetYPixels;
            approvedToggle.isOn = value.approved;
        }

        updatingUi = false;
        UpdateLabels();
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (targetRenderer == null || referenceRenderer == null)
        {
            return;
        }

        Sprite targetSprite = sourceSprites.Length > 0 ? sourceSprites[currentFrameIndex] : null;
        Sprite referenceSprite = referenceSprites.Length > 0 ? referenceSprites[currentReferenceIndex] : null;
        targetRenderer.sprite = targetSprite;
        referenceRenderer.sprite = referenceSprite;

        bool showReference = showReferenceToggle == null || showReferenceToggle.isOn;
        referenceRenderer.enabled = showReference && referenceSprite != null;
        targetRenderer.enabled = targetSprite != null;

        SpriteScaleTuningFrameValue value = GetCurrentValue();
        float pixelsPerUnit = targetSprite != null ? targetSprite.pixelsPerUnit : 55f;
        if (value != null)
        {
            targetRenderer.transform.localScale = Vector3.one * Mathf.Max(0.01f, value.scale);
            targetRenderer.transform.position = new Vector3(value.offsetXPixels / pixelsPerUnit, value.offsetYPixels / pixelsPerUnit, -0.1f);
        }
        else
        {
            targetRenderer.transform.localScale = Vector3.one;
            targetRenderer.transform.position = new Vector3(0f, 0f, -0.1f);
        }

        referenceRenderer.transform.localScale = Vector3.one;
        referenceRenderer.transform.position = Vector3.zero;
        UpdateBaseline(targetSprite != null ? targetSprite : referenceSprite);
    }

    private void UpdateBaseline(Sprite sprite)
    {
        if (baselineRenderer == null)
        {
            return;
        }

        float pixelsPerUnit = sprite != null ? sprite.pixelsPerUnit : 55f;
        float halfWidth = FrameWidthPixels * 0.5f / pixelsPerUnit;
        float baselineY = (FrameHeightPixels * 0.5f - DefaultBaselinePixelY) / pixelsPerUnit;
        baselineRenderer.SetPosition(0, new Vector3(-halfWidth, baselineY, -0.3f));
        baselineRenderer.SetPosition(1, new Vector3(halfWidth, baselineY, -0.3f));
    }

    private void UpdateLabels()
    {
        SpriteScaleTuningFrameValue value = GetCurrentValue();
        if (value == null)
        {
            return;
        }

        frameTitleLabel.text = GetFrameLabel(currentFrameIndex) + " | " + value.frameName;
        scaleValueLabel.text = "Escala: " + value.scale.ToString("0.000", CultureInfo.InvariantCulture);
        offsetXValueLabel.text = "X: " + value.offsetXPixels.ToString("0.0", CultureInfo.InvariantCulture) + " px";
        offsetYValueLabel.text = "Y: " + value.offsetYPixels.ToString("0.0", CultureInfo.InvariantCulture) + " px";
    }

    private void HandleSourceChanged(int selectedIndex)
    {
        if (updatingUi)
        {
            return;
        }

        LoadSelectedSource(selectedIndex);
    }

    private void HandleFrameChanged(int selectedIndex)
    {
        if (updatingUi || selectedIndex < 0 || selectedIndex >= sourceSprites.Length)
        {
            return;
        }

        currentFrameIndex = selectedIndex;
        RefreshAllUiFromState();
    }

    private void HandleReferenceChanged(int selectedIndex)
    {
        if (updatingUi || selectedIndex < 0 || selectedIndex >= referenceSprites.Length)
        {
            return;
        }

        currentReferenceIndex = selectedIndex;
        UpdatePreview();
    }

    private void HandleScaleChanged(float value)
    {
        if (updatingUi)
        {
            return;
        }

        SpriteScaleTuningFrameValue frameValue = GetCurrentValue();
        if (frameValue == null)
        {
            return;
        }

        frameValue.scale = value;
        UpdateLabels();
        UpdatePreview();
    }

    private void HandleOffsetXChanged(float value)
    {
        if (updatingUi)
        {
            return;
        }

        SpriteScaleTuningFrameValue frameValue = GetCurrentValue();
        if (frameValue == null)
        {
            return;
        }

        frameValue.offsetXPixels = value;
        UpdateLabels();
        UpdatePreview();
    }

    private void HandleOffsetYChanged(float value)
    {
        if (updatingUi)
        {
            return;
        }

        SpriteScaleTuningFrameValue frameValue = GetCurrentValue();
        if (frameValue == null)
        {
            return;
        }

        frameValue.offsetYPixels = value;
        UpdateLabels();
        UpdatePreview();
    }

    private void HandleApprovedChanged(bool approved)
    {
        if (updatingUi)
        {
            return;
        }

        SpriteScaleTuningFrameValue frameValue = GetCurrentValue();
        if (frameValue != null)
        {
            frameValue.approved = approved;
        }
    }

    private void ChangeFrame(int direction)
    {
        if (sourceSprites.Length == 0)
        {
            return;
        }

        currentFrameIndex = (currentFrameIndex + direction + sourceSprites.Length) % sourceSprites.Length;
        RefreshAllUiFromState();
    }

    private void NudgeScale(float delta)
    {
        SpriteScaleTuningFrameValue value = GetCurrentValue();
        if (value == null)
        {
            return;
        }

        value.scale = Mathf.Clamp(value.scale + delta, scaleSlider.minValue, scaleSlider.maxValue);
        RefreshAllUiFromState();
    }

    private void NudgeOffset(float deltaX, float deltaY)
    {
        SpriteScaleTuningFrameValue value = GetCurrentValue();
        if (value == null)
        {
            return;
        }

        value.offsetXPixels = Mathf.Clamp(value.offsetXPixels + deltaX, offsetXSlider.minValue, offsetXSlider.maxValue);
        value.offsetYPixels = Mathf.Clamp(value.offsetYPixels + deltaY, offsetYSlider.minValue, offsetYSlider.maxValue);
        RefreshAllUiFromState();
    }

    private void ResetCurrentFrame()
    {
        SpriteScaleTuningFrameValue value = GetCurrentValue();
        if (value == null)
        {
            return;
        }

        value.scale = 1f;
        value.offsetXPixels = 0f;
        value.offsetYPixels = 0f;
        value.approved = false;
        RefreshAllUiFromState();
    }

    private void ApplyCurrentScaleToRow()
    {
        SpriteScaleTuningFrameValue current = GetCurrentValue();
        if (current == null)
        {
            return;
        }

        foreach (SpriteScaleTuningFrameValue value in frameValues)
        {
            if (value.row == current.row)
            {
                value.scale = current.scale;
            }
        }

        SetStatus("Escala " + current.scale.ToString("0.000", CultureInfo.InvariantCulture) + " aplicada na linha " + current.row + ".");
        RefreshAllUiFromState();
    }

    private void ApplyCurrentTransformToRow()
    {
        SpriteScaleTuningFrameValue current = GetCurrentValue();
        if (current == null)
        {
            return;
        }

        foreach (SpriteScaleTuningFrameValue value in frameValues)
        {
            if (value.row == current.row)
            {
                value.scale = current.scale;
                value.offsetXPixels = current.offsetXPixels;
                value.offsetYPixels = current.offsetYPixels;
            }
        }

        SetStatus("Escala e deslocamento aplicados na linha " + current.row + ".");
        RefreshAllUiFromState();
    }

    private void ExportCalibration()
    {
        if (sourceOptions.Count == 0 || frameValues.Length == 0)
        {
            SetStatus("Nada para exportar.");
            return;
        }

        PlayerAttackSpriteVersion version = sourceOptions[currentSourceIndex];
        var export = new SpriteScaleTuningExport
        {
            exportedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            sourceVersionId = version.Id,
            sourceDisplayName = version.DisplayName,
            sourceResourcePath = version.ResourcePath,
            referenceResourcePath = ReferenceResourcePath,
            frameWidthPixels = FrameWidthPixels,
            frameHeightPixels = FrameHeightPixels,
            baselinePixelY = DefaultBaselinePixelY,
            pivotMode = "sprite import pivot",
            offsetConvention = "offsetXPixels positive right; offsetYPixels positive up",
            frames = new List<SpriteScaleTuningFrameValue>(frameValues)
        };

        string fileName = "player_attack_scale_calibration_" + version.Id + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".json";
        string exportDirectory;
        if (Application.isEditor)
        {
            exportDirectory = Path.Combine(Application.dataPath, ExportFolderAssetPath);
        }
        else
        {
            exportDirectory = Path.Combine(Application.persistentDataPath, "ScaleCalibrationExports");
        }

        Directory.CreateDirectory(exportDirectory);
        lastExportPath = Path.Combine(exportDirectory, fileName);
        File.WriteAllText(lastExportPath, JsonUtility.ToJson(export, true));
        SetStatus("Exportado: " + lastExportPath);
    }

    private void SetStatus(string message)
    {
        if (statusLabel != null)
        {
            statusLabel.text = message;
        }
    }

    private SpriteScaleTuningFrameValue GetCurrentValue()
    {
        if (frameValues == null || currentFrameIndex < 0 || currentFrameIndex >= frameValues.Length)
        {
            return null;
        }

        return frameValues[currentFrameIndex];
    }

    private string GetFrameLabel(int index)
    {
        int row;
        int column;
        GetAttackRowAndColumn(index, out row, out column);
        return "Linha " + row + " / Frame " + column;
    }

    private void GetAttackRowAndColumn(int flatIndex, out int row, out int column)
    {
        int cursor = 0;
        for (int rowIndex = 0; rowIndex < attackRowCounts.Length; rowIndex++)
        {
            int count = attackRowCounts[rowIndex];
            if (flatIndex < cursor + count)
            {
                row = rowIndex + 1;
                column = flatIndex - cursor + 1;
                return;
            }

            cursor += count;
        }

        row = 1;
        column = flatIndex + 1;
    }

    private static int CompareSpritesByName(Sprite a, Sprite b)
    {
        string nameA = a != null ? a.name : string.Empty;
        string nameB = b != null ? b.name : string.Empty;
        return string.CompareOrdinal(nameA, nameB);
    }

    private int FindDefaultReferenceSpriteIndex()
    {
        for (int index = 0; index < referenceSprites.Length; index++)
        {
            string spriteName = referenceSprites[index] != null ? referenceSprites[index].name : string.Empty;
            if (string.Equals(spriteName, DefaultReferenceSpriteName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(spriteName, "player_walk_1", StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return 0;
    }

    private void BuildPreviewWorld()
    {
        previewCamera = Camera.main;
        if (previewCamera == null)
        {
            var cameraObject = new GameObject("SpriteTuningCamera", typeof(Camera));
            previewCamera = cameraObject.GetComponent<Camera>();
        }

        previewCamera.orthographic = true;
        previewCamera.orthographicSize = 3.8f;
        previewCamera.transform.position = new Vector3(0f, 0f, -10f);
        previewCamera.backgroundColor = new Color(0.04f, 0.045f, 0.055f, 1f);
        previewCamera.clearFlags = CameraClearFlags.SolidColor;

        referenceRenderer = new GameObject("ReferenceWalkSprite").AddComponent<SpriteRenderer>();
        referenceRenderer.color = new Color(0.55f, 0.85f, 1f, 0.35f);
        referenceRenderer.sortingOrder = 0;

        targetRenderer = new GameObject("EditableAttackSprite").AddComponent<SpriteRenderer>();
        targetRenderer.color = Color.white;
        targetRenderer.sortingOrder = 1;

        baselineRenderer = new GameObject("FootBaseline").AddComponent<LineRenderer>();
        baselineRenderer.positionCount = 2;
        baselineRenderer.startWidth = 0.025f;
        baselineRenderer.endWidth = 0.025f;
        baselineRenderer.useWorldSpace = true;
        baselineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        baselineRenderer.startColor = new Color(0f, 1f, 0.18f, 0.85f);
        baselineRenderer.endColor = new Color(0f, 1f, 0.18f, 0.85f);
        baselineRenderer.sortingOrder = 5;
    }

    private void BuildUi()
    {
        Canvas canvas = CreateCanvas();
        CreateTopPanel(canvas.transform);
        CreateRightPanel(canvas.transform);
        CreateStatusLabel(canvas.transform);
    }

    private Canvas CreateCanvas()
    {
        var canvasObject = new GameObject("SpriteScaleTuningCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private void CreateTopPanel(Transform parent)
    {
        GameObject panel = CreatePanel("TopPanel", parent, new Color(0.07f, 0.08f, 0.11f, 0.94f));
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, 118f);
        rect.anchoredPosition = Vector2.zero;

        Text title = CreateText("Title", panel.transform, "Teste de escala dos sprites", 30, TextAnchor.MiddleLeft);
        SetRect(title.GetComponent<RectTransform>(), new Vector2(20f, -16f), new Vector2(520f, 42f), new Vector2(0f, 1f), new Vector2(0f, 1f));

        sourceDropdown = CreateDropdown(panel.transform, "SourceDropdown", new Vector2(540f, -22f), new Vector2(310f, 46f));
        sourceDropdown.onValueChanged.AddListener(HandleSourceChanged);

        frameDropdown = CreateDropdown(panel.transform, "FrameDropdown", new Vector2(870f, -22f), new Vector2(310f, 46f));
        frameDropdown.onValueChanged.AddListener(HandleFrameChanged);

        referenceDropdown = CreateDropdown(panel.transform, "ReferenceDropdown", new Vector2(1200f, -22f), new Vector2(300f, 46f));
        referenceDropdown.onValueChanged.AddListener(HandleReferenceChanged);

        CreateButton(panel.transform, "PrevButton", "< Frame", new Vector2(540f, -76f), new Vector2(150f, 38f), delegate { ChangeFrame(-1); });
        CreateButton(panel.transform, "NextButton", "Frame >", new Vector2(702f, -76f), new Vector2(150f, 38f), delegate { ChangeFrame(1); });
        CreateButton(panel.transform, "ExportButton", "Exportar JSON", new Vector2(870f, -76f), new Vector2(190f, 38f), ExportCalibration);
        CreateButton(panel.transform, "MenuButton", "Voltar ao menu", new Vector2(1080f, -76f), new Vector2(190f, 38f), delegate { SceneManager.LoadScene("TelaInicial"); });

        frameTitleLabel = CreateText("FrameTitle", panel.transform, string.Empty, 22, TextAnchor.MiddleLeft);
        SetRect(frameTitleLabel.GetComponent<RectTransform>(), new Vector2(20f, -68f), new Vector2(500f, 38f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        frameTitleLabel.color = new Color(0.84f, 0.9f, 1f, 1f);
    }

    private void CreateRightPanel(Transform parent)
    {
        GameObject panel = CreatePanel("ControlPanel", parent, new Color(0.07f, 0.08f, 0.11f, 0.9f));
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.sizeDelta = new Vector2(380f, -180f);
        rect.anchoredPosition = new Vector2(-20f, -30f);

        Text hint = CreateText("Hint", panel.transform, "Ajuste visual por frame", 24, TextAnchor.MiddleCenter);
        SetRect(hint.GetComponent<RectTransform>(), new Vector2(0f, -24f), new Vector2(340f, 40f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));

        scaleSlider = CreateSlider(panel.transform, "ScaleSlider", 0.5f, 1.8f, 1f, new Vector2(0f, -100f));
        scaleSlider.onValueChanged.AddListener(HandleScaleChanged);
        scaleValueLabel = CreateText("ScaleValue", panel.transform, string.Empty, 20, TextAnchor.MiddleLeft);
        SetRect(scaleValueLabel.GetComponent<RectTransform>(), new Vector2(-160f, -68f), new Vector2(320f, 30f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));

        offsetXSlider = CreateSlider(panel.transform, "OffsetXSlider", -140f, 140f, 0f, new Vector2(0f, -185f));
        offsetXSlider.onValueChanged.AddListener(HandleOffsetXChanged);
        offsetXValueLabel = CreateText("OffsetXValue", panel.transform, string.Empty, 20, TextAnchor.MiddleLeft);
        SetRect(offsetXValueLabel.GetComponent<RectTransform>(), new Vector2(-160f, -153f), new Vector2(320f, 30f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));

        offsetYSlider = CreateSlider(panel.transform, "OffsetYSlider", -140f, 140f, 0f, new Vector2(0f, -270f));
        offsetYSlider.onValueChanged.AddListener(HandleOffsetYChanged);
        offsetYValueLabel = CreateText("OffsetYValue", panel.transform, string.Empty, 20, TextAnchor.MiddleLeft);
        SetRect(offsetYValueLabel.GetComponent<RectTransform>(), new Vector2(-160f, -238f), new Vector2(320f, 30f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));

        zoomSlider = CreateSlider(panel.transform, "ZoomSlider", 2.6f, 6.5f, 3.8f, new Vector2(0f, -355f));
        zoomSlider.onValueChanged.AddListener(delegate(float value)
        {
            if (previewCamera != null)
            {
                previewCamera.orthographicSize = value;
            }
        });
        Text zoomLabel = CreateText("ZoomLabel", panel.transform, "Zoom", 20, TextAnchor.MiddleLeft);
        SetRect(zoomLabel.GetComponent<RectTransform>(), new Vector2(-160f, -323f), new Vector2(320f, 30f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));

        showReferenceToggle = CreateToggle(panel.transform, "ShowReferenceToggle", "Mostrar referencia", new Vector2(-120f, -410f));
        showReferenceToggle.isOn = true;
        showReferenceToggle.onValueChanged.AddListener(delegate { UpdatePreview(); });

        approvedToggle = CreateToggle(panel.transform, "ApprovedToggle", "Frame aprovado", new Vector2(-120f, -462f));
        approvedToggle.onValueChanged.AddListener(HandleApprovedChanged);

        CreateButton(panel.transform, "ApplyScaleRowButton", "Aplicar escala na linha", new Vector2(0f, -530f), new Vector2(320f, 44f), ApplyCurrentScaleToRow);
        CreateButton(panel.transform, "ApplyTransformRowButton", "Aplicar tudo na linha", new Vector2(0f, -590f), new Vector2(320f, 44f), ApplyCurrentTransformToRow);
        CreateButton(panel.transform, "ResetFrameButton", "Resetar frame", new Vector2(0f, -650f), new Vector2(320f, 44f), ResetCurrentFrame);

        Text keys = CreateText("KeyboardHint", panel.transform, "Setas trocam frame | Q/E escala | WASD move", 17, TextAnchor.MiddleCenter);
        SetRect(keys.GetComponent<RectTransform>(), new Vector2(0f, 32f), new Vector2(340f, 46f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        keys.color = new Color(0.78f, 0.84f, 0.95f, 1f);
    }

    private void CreateStatusLabel(Transform parent)
    {
        GameObject panel = CreatePanel("StatusPanel", parent, new Color(0.04f, 0.05f, 0.075f, 0.9f));
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(0f, 58f);
        rect.anchoredPosition = Vector2.zero;

        statusLabel = CreateText("StatusLabel", panel.transform, "Pronto.", 20, TextAnchor.MiddleLeft);
        RectTransform statusRect = statusLabel.GetComponent<RectTransform>();
        statusRect.anchorMin = Vector2.zero;
        statusRect.anchorMax = Vector2.one;
        statusRect.offsetMin = new Vector2(20f, 0f);
        statusRect.offsetMax = new Vector2(-20f, 0f);
        statusLabel.color = new Color(0.86f, 0.91f, 1f, 1f);
    }

    private Slider CreateSlider(Transform parent, string name, float min, float max, float value, Vector2 anchoredPosition)
    {
        GameObject sliderObject = new GameObject(name, typeof(RectTransform));
        sliderObject.transform.SetParent(parent, false);
        RectTransform rect = sliderObject.GetComponent<RectTransform>();
        SetRect(rect, anchoredPosition, new Vector2(320f, 34f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = value;

        GameObject background = CreatePanel("Background", sliderObject.transform, new Color(0.12f, 0.16f, 0.22f, 1f));
        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0.25f);
        bgRect.anchorMax = new Vector2(1f, 0.75f);
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.offsetMin = new Vector2(8f, 0f);
        fillAreaRect.offsetMax = new Vector2(-8f, 0f);

        GameObject fill = CreatePanel("Fill", fillArea.transform, new Color(0.34f, 0.62f, 1f, 1f));
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        slider.fillRect = fillRect;

        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderObject.transform, false);
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10f, 0f);
        handleAreaRect.offsetMax = new Vector2(-10f, 0f);

        GameObject handle = CreatePanel("Handle", handleArea.transform, new Color(0.92f, 0.96f, 1f, 1f));
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(22f, 28f);
        slider.handleRect = handleRect;
        slider.targetGraphic = handle.GetComponent<Image>();
        return slider;
    }

    private Toggle CreateToggle(Transform parent, string name, string label, Vector2 anchoredPosition)
    {
        GameObject toggleObject = new GameObject(name, typeof(RectTransform));
        toggleObject.transform.SetParent(parent, false);
        SetRect(toggleObject.GetComponent<RectTransform>(), anchoredPosition, new Vector2(300f, 34f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));

        Toggle toggle = toggleObject.AddComponent<Toggle>();
        GameObject box = CreatePanel("Box", toggleObject.transform, new Color(0.13f, 0.17f, 0.24f, 1f));
        SetRect(box.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(28f, 28f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        GameObject check = CreatePanel("Checkmark", box.transform, new Color(0.34f, 0.84f, 0.48f, 1f));
        RectTransform checkRect = check.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.22f, 0.22f);
        checkRect.anchorMax = new Vector2(0.78f, 0.78f);
        checkRect.offsetMin = Vector2.zero;
        checkRect.offsetMax = Vector2.zero;

        Text labelText = CreateText("Label", toggleObject.transform, label, 20, TextAnchor.MiddleLeft);
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = new Vector2(42f, 0f);
        labelRect.offsetMax = Vector2.zero;

        toggle.targetGraphic = box.GetComponent<Image>();
        toggle.graphic = check.GetComponent<Image>();
        return toggle;
    }

    private Dropdown CreateDropdown(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject dropdownObject = CreatePanel(name, parent, new Color(0.12f, 0.16f, 0.22f, 0.98f));
        SetRect(dropdownObject.GetComponent<RectTransform>(), anchoredPosition, size, new Vector2(0f, 1f), new Vector2(0f, 1f));

        Dropdown dropdown = dropdownObject.AddComponent<Dropdown>();
        dropdown.targetGraphic = dropdownObject.GetComponent<Image>();

        Text caption = CreateText("Label", dropdownObject.transform, string.Empty, 18, TextAnchor.MiddleLeft);
        RectTransform captionRect = caption.GetComponent<RectTransform>();
        captionRect.anchorMin = Vector2.zero;
        captionRect.anchorMax = Vector2.one;
        captionRect.offsetMin = new Vector2(14f, 0f);
        captionRect.offsetMax = new Vector2(-40f, 0f);
        dropdown.captionText = caption;

        Text arrow = CreateText("Arrow", dropdownObject.transform, "v", 18, TextAnchor.MiddleCenter);
        RectTransform arrowRect = arrow.GetComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1f, 0f);
        arrowRect.anchorMax = new Vector2(1f, 1f);
        arrowRect.sizeDelta = new Vector2(36f, 0f);
        arrowRect.anchoredPosition = Vector2.zero;

        GameObject template = CreateDropdownTemplate(dropdownObject.transform, size.x);
        dropdown.template = template.GetComponent<RectTransform>();
        Transform itemLabel = FindChildRecursive(template.transform, "Item Label");
        dropdown.itemText = itemLabel != null ? itemLabel.GetComponent<Text>() : null;
        template.SetActive(false);
        return dropdown;
    }

    private GameObject CreateDropdownTemplate(Transform parent, float width)
    {
        GameObject template = CreatePanel("Template", parent, new Color(0.08f, 0.1f, 0.14f, 0.98f));
        RectTransform templateRect = template.GetComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0f, 0f);
        templateRect.anchorMax = new Vector2(1f, 0f);
        templateRect.pivot = new Vector2(0.5f, 1f);
        templateRect.sizeDelta = new Vector2(0f, 260f);
        templateRect.anchoredPosition = new Vector2(0f, -48f);

        ScrollRect scrollRect = template.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;

        GameObject viewport = CreatePanel("Viewport", template.transform, new Color(0.08f, 0.1f, 0.14f, 1f));
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(4f, 4f);
        viewportRect.offsetMax = new Vector2(-4f, -4f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject item = CreatePanel("Item", content.transform, new Color(0.13f, 0.18f, 0.25f, 1f));
        item.AddComponent<LayoutElement>().preferredHeight = 38f;
        Toggle toggle = item.AddComponent<Toggle>();
        toggle.targetGraphic = item.GetComponent<Image>();
        Text itemLabel = CreateText("Item Label", item.transform, string.Empty, 18, TextAnchor.MiddleLeft);
        RectTransform itemLabelRect = itemLabel.GetComponent<RectTransform>();
        itemLabelRect.anchorMin = Vector2.zero;
        itemLabelRect.anchorMax = Vector2.one;
        itemLabelRect.offsetMin = new Vector2(12f, 0f);
        itemLabelRect.offsetMax = new Vector2(-12f, 0f);

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        return template;
    }

    private Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = CreatePanel(name, parent, new Color(0.16f, 0.22f, 0.31f, 0.98f));
        SetRect(buttonObject.GetComponent<RectTransform>(), anchoredPosition, size, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(action);
        Text text = CreateText("Label", buttonObject.transform, label, 19, TextAnchor.MiddleCenter);
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return button;
    }

    private GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private Text CreateText(string name, Transform parent, string value, int size, TextAnchor anchor)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.alignment = anchor;
        text.color = Color.white;
        return text;
    }

    private static void SetRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (int index = 0; index < root.childCount; index++)
        {
            Transform child = FindChildRecursive(root.GetChild(index), childName);
            if (child != null)
            {
                return child;
            }
        }

        return null;
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}

[Serializable]
public class SpriteScaleTuningExport
{
    public string exportedAt;
    public string sourceVersionId;
    public string sourceDisplayName;
    public string sourceResourcePath;
    public string referenceResourcePath;
    public int frameWidthPixels;
    public int frameHeightPixels;
    public int baselinePixelY;
    public string pivotMode;
    public string offsetConvention;
    public List<SpriteScaleTuningFrameValue> frames = new List<SpriteScaleTuningFrameValue>();
}

[Serializable]
public class SpriteScaleTuningFrameValue
{
    public string frameName;
    public int frameIndex;
    public int row;
    public int column;
    public float scale = 1f;
    public float offsetXPixels;
    public float offsetYPixels;
    public bool approved;
}
