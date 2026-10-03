using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public sealed class ThanhGiongSceneTransition : MonoBehaviour
{
    private static ThanhGiongSceneTransition instance;
    private static bool transitioning;

    [SerializeField] private float fadeOutDuration = 0.7f;
    [SerializeField] private float minimumLoadScreen = 0.45f;
    [SerializeField] private float fadeInDuration = 0.85f;
    [SerializeField] private bool demoNavigationEnabled = true;

    private float veilAlpha;
    private string status = string.Empty;
    private GUIStyle captionStyle;
    private bool navigationHelpVisible;

    public static bool IsTransitioning => transitioning;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        transitioning = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() => EnsureInstance();

    private static void EnsureInstance()
    {
        if (instance != null) return;
        GameObject host = new GameObject("Thanh Giong Scene Transition");
        instance = host.AddComponent<ThanhGiongSceneTransition>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        veilAlpha = 1f;
        StartCoroutine(InitialReveal());
    }

    private void OnGUI()
    {
        Color previous = GUI.color;
        if (veilAlpha > 0.001f)
        {
            GUI.color = new Color(0.018f, 0.025f, 0.035f, veilAlpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

            if (!string.IsNullOrEmpty(status))
            {
                if (captionStyle == null)
                {
                    captionStyle = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = Mathf.Clamp(Screen.height / 30, 24, 42),
                        fontStyle = FontStyle.Bold,
                        wordWrap = true
                    };
                }
                GUI.color = new Color(1f, 0.78f, 0.22f, veilAlpha);
                GUI.Label(new Rect(Screen.width * 0.15f, Screen.height * 0.38f, Screen.width * 0.7f, Screen.height * 0.24f), status, captionStyle);
            }
        }

        if (demoNavigationEnabled && navigationHelpVisible && !transitioning)
        {
            GUIStyle hint = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Clamp(Screen.height / 60, 14, 20),
                fontStyle = FontStyle.Bold
            };
            GUI.color = new Color(1f, 1f, 1f, 0.88f);
            GUI.Box(new Rect(Screen.width - 390f, Screen.height - 48f, 375f, 34f), "DEMO MAP  |  F5: Màn trước  ·  F6: Màn sau  ·  F8: Xem lại", hint);
        }
        GUI.color = previous;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) navigationHelpVisible = !navigationHelpVisible;
        if (!demoNavigationEnabled || transitioning) return;

        if (Input.GetKeyDown(KeyCode.F6))
            TryTransitionToNext("ĐANG CHUYỂN ĐẾN MÀN TIẾP THEO...");
        else if (Input.GetKeyDown(KeyCode.F5))
        {
            string previous = GetPreviousScene(SceneManager.GetActiveScene().name);
            if (!string.IsNullOrEmpty(previous)) TryTransitionTo(previous, "QUAY LẠI MÀN TRƯỚC...");
        }
        else if (Input.GetKeyDown(KeyCode.F8))
            TryTransitionTo(SceneManager.GetActiveScene().name, "ĐANG TẢI LẠI BẢN ĐỒ...");
    }

    private IEnumerator InitialReveal()
    {
        yield return null;
        yield return Fade(1f, 0f, fadeInDuration);
    }

    public static bool TryTransitionTo(string sceneName, string caption = null)
    {
        EnsureInstance();
        if (transitioning || string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            return false;

        instance.StartCoroutine(instance.LoadScene(sceneName, caption));
        return true;
    }

    public static string GetNextScene(string currentScene)
    {
        return currentScene switch
        {
            "LangGiongTienTuyen" => "KinhThanhRenThep",
            "KinhThanhRenThep" => "PhaoDaiNgamQuanAn",
            "PhaoDaiNgamQuanAn" => "ThungLungVuotSong",
            "ThungLungVuotSong" => "TranTuyenNuiSoc",
            "TranTuyenNuiSoc" => "DinhSocHoaThanh",
            _ => null
        };
    }

    public static string GetPreviousScene(string currentScene)
    {
        return currentScene switch
        {
            "KinhThanhRenThep" => "LangGiongTienTuyen",
            "PhaoDaiNgamQuanAn" => "KinhThanhRenThep",
            "ThungLungVuotSong" => "PhaoDaiNgamQuanAn",
            "TranTuyenNuiSoc" => "ThungLungVuotSong",
            "DinhSocHoaThanh" => "TranTuyenNuiSoc",
            _ => null
        };
    }

    public static bool TryTransitionToNext(string caption = null)
    {
        string next = GetNextScene(SceneManager.GetActiveScene().name);
        return !string.IsNullOrEmpty(next) && TryTransitionTo(next, caption);
    }

    private IEnumerator LoadScene(string sceneName, string caption)
    {
        transitioning = true;
        SetPlayerInput(false);
        status = string.IsNullOrWhiteSpace(caption) ? "HÀNH TRÌNH TIẾP TỤC..." : caption;
        yield return Fade(veilAlpha, 1f, fadeOutDuration);

        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        if (load == null)
        {
            yield return Fade(1f, 0f, fadeInDuration);
            transitioning = false;
            SetPlayerInput(true);
            yield break;
        }

        load.allowSceneActivation = false;
        float hold = 0f;
        while (load.progress < 0.9f || hold < minimumLoadScreen)
        {
            hold += Time.unscaledDeltaTime;
            yield return null;
        }

        load.allowSceneActivation = true;
        while (!load.isDone) yield return null;
        yield return null;
        yield return null;

        status = string.Empty;
        SetPlayerInput(true);
        yield return Fade(1f, 0f, fadeInDuration);
        transitioning = false;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            veilAlpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            veilAlpha = Mathf.LerpUnclamped(from, to, t);
            yield return null;
        }
        veilAlpha = to;
    }

    private static void SetPlayerInput(bool enabled)
    {
        foreach (MountedHorseController controller in FindObjectsByType<MountedHorseController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            controller.enabled = enabled;
        foreach (AdventurerController controller in FindObjectsByType<AdventurerController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            controller.enabled = enabled;
    }
}
