using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private const int JoystickLayer = 5;

    public static MobileJoystick Instance { get; private set; }

    public float Horizontal => input.x;
    public float Vertical => input.y;
    public Vector2 Direction => input;

    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;
    [SerializeField, Range(0f, 1f)] private float handleRange = 0.65f;
    [SerializeField, Range(0f, 1f)] private float deadZone = 0.12f;

    private Vector2 input;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        ResetJoystick();
    }

    public static MobileJoystick EnsureInScene()
    {
        MobileJoystick existingJoystick = FindFirstObjectByType<MobileJoystick>(FindObjectsInactive.Include);
        if (existingJoystick != null)
        {
            return existingJoystick;
        }

        EnsureEventSystem();

        GameObject canvasObject = new GameObject("MobileJoystickCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.layer = JoystickLayer;

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform joystickRect = CreateJoystickRect(canvasObject.transform);
        Image backgroundImage = joystickRect.gameObject.AddComponent<Image>();
        backgroundImage.sprite = CreateCircleSprite(160, new Color(1f, 1f, 1f, 0.22f), new Color(1f, 1f, 1f, 0.65f), 5);
        backgroundImage.raycastTarget = true;

        RectTransform handleRect = CreateHandleRect(joystickRect);
        Image handleImage = handleRect.gameObject.AddComponent<Image>();
        handleImage.sprite = CreateCircleSprite(96, new Color(1f, 1f, 1f, 0.62f), new Color(1f, 1f, 1f, 0.9f), 3);
        handleImage.raycastTarget = false;

        MobileJoystick joystick = joystickRect.gameObject.AddComponent<MobileJoystick>();
        joystick.Configure(joystickRect, handleRect);
        return joystick;
    }

    public void Configure(RectTransform joystickBackground, RectTransform joystickHandle)
    {
        background = joystickBackground;
        handle = joystickHandle;
        ResetJoystick();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (background == null || handle == null)
        {
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            return;
        }

        Vector2 radius = background.rect.size * 0.5f;
        Vector2 rawInput = new Vector2(localPoint.x / radius.x, localPoint.y / radius.y);
        input = Vector2.ClampMagnitude(rawInput, 1f);

        if (input.magnitude < deadZone)
        {
            input = Vector2.zero;
        }

        handle.anchoredPosition = new Vector2(input.x * radius.x, input.y * radius.y) * handleRange;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ResetJoystick();
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystemObject.layer = JoystickLayer;
    }

    private static RectTransform CreateJoystickRect(Transform parent)
    {
        GameObject joystickObject = new GameObject("MobileJoystick", typeof(RectTransform), typeof(CanvasRenderer));
        joystickObject.layer = JoystickLayer;
        joystickObject.transform.SetParent(parent, false);

        RectTransform rect = joystickObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(180f, 180f);
        rect.anchoredPosition = new Vector2(150f, 140f);
        return rect;
    }

    private static RectTransform CreateHandleRect(RectTransform parent)
    {
        GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer));
        handleObject.layer = JoystickLayer;
        handleObject.transform.SetParent(parent, false);

        RectTransform rect = handleObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(82f, 82f);
        rect.anchoredPosition = Vector2.zero;
        return rect;
    }

    private static Sprite CreateCircleSprite(int size, Color fill, Color border, int borderSize)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "GeneratedJoystickCircle";
        texture.filterMode = FilterMode.Bilinear;

        float center = (size - 1) * 0.5f;
        float radius = center;
        float innerRadius = radius - borderSize;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                Color pixel = Color.clear;

                if (distance <= radius)
                {
                    pixel = distance >= innerRadius ? border : fill;
                }

                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private void ResetJoystick()
    {
        input = Vector2.zero;

        if (handle != null)
        {
            handle.anchoredPosition = Vector2.zero;
        }
    }
}

public static class MobileJoystickBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateJoystickForGameplayScene()
    {
        if (Object.FindFirstObjectByType<Controle>() == null)
        {
            return;
        }

        if (Object.FindFirstObjectByType<FixedJoystick>(FindObjectsInactive.Include) != null)
        {
            return;
        }

        MobileJoystick.EnsureInScene();
    }
}
