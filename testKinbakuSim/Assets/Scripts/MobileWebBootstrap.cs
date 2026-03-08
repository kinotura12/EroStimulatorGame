using UnityEngine;
using UnityEngine.UI;

public sealed class MobileWebBootstrap : MonoBehaviour
{
    const float LandscapeReferenceWidth = 1920f;
    const float LandscapeReferenceHeight = 1080f;

    static MobileWebBootstrap instance;
    Rect lastSafeArea = new Rect(-1f, -1f, -1f, -1f);
    Vector2Int lastScreenSize = new Vector2Int(-1, -1);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (instance != null) return;

        var go = new GameObject(nameof(MobileWebBootstrap));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<MobileWebBootstrap>();
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        ApplyRuntimeSettings(force: true);
    }

    void Update()
    {
        ApplyRuntimeSettings(force: false);
    }

    void ApplyRuntimeSettings(bool force)
    {
        if (!ShouldApplyMobileLayout()) return;

        ConfigureOrientation();

        if (!force &&
            lastSafeArea == Screen.safeArea &&
            lastScreenSize.x == Screen.width &&
            lastScreenSize.y == Screen.height)
        {
            return;
        }

        lastSafeArea = Screen.safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);

        ApplyCanvasScalerSettings();
        ApplySafeAreaToRootCanvases();
    }

    static bool ShouldApplyMobileLayout()
    {
        return Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld;
    }

    static void ConfigureOrientation()
    {
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.AutoRotation;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    static void ApplyCanvasScalerSettings()
    {
        var scalers = FindObjectsByType<CanvasScaler>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var scaler in scalers)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(LandscapeReferenceWidth, LandscapeReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
        }
    }

    static void ApplySafeAreaToRootCanvases()
    {
        Rect safeArea = Screen.safeArea;
        if (Screen.width <= 0 || Screen.height <= 0) return;

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var canvas in canvases)
        {
            if (!canvas.isRootCanvas) continue;

            var rect = canvas.GetComponent<RectTransform>();
            if (rect == null) continue;

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
