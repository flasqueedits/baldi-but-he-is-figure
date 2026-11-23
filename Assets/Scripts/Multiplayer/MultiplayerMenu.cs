using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds the Host / Join panel at runtime on the MainMenu scene.
/// No scene editing is needed. Shows connection hints and validates the address.
/// </summary>
public static class MultiplayerMenu
{
    /// <summary>Set to false if you build your own menu UI in the MainMenu scene.</summary>
    public static bool UseRuntimeMenu = true;

    private static GameObject overlay;
    private static Text statusText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (!UseRuntimeMenu) return;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            Build();
        }
        else if (overlay != null)
        {
            Object.Destroy(overlay);
            overlay = null;
            statusText = null;
        }
    }

    private static void Build()
    {
        if (!UseRuntimeMenu) return;
        if (Object.FindObjectOfType<MainMenuMultiplayerUI>() != null) return; // in-scene GUI takes over
        if (overlay != null) return;
        if (GameObject.Find("FigureMultiplayerMenu") != null) return;

        EnsureEventSystem();
        Font font = LoadFont();

        GameObject canvasGo = new GameObject("FigureMultiplayerMenu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        overlay = canvasGo;
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = MakeUI("Panel", canvasGo.transform);
        SetRect(panel.GetComponent<RectTransform>(), Vector2.zero, new Vector2(600f, 430f));
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.04f, 0.04f, 0.07f, 0.96f);
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.78f, 0.25f, 0.8f);
        outline.effectDistance = new Vector2(2f, -2f);

        Text title = MakeText("Title", panel.transform, font, 34, TextAnchor.MiddleCenter);
        SetRect(title.rectTransform, new Vector2(0f, 172f), new Vector2(580f, 54f));
        title.text = "FIGURE  -  CO-OP MULTIPLAYER";
        title.color = new Color(1f, 0.85f, 0.3f);

        Text info = MakeText("Bilgi", panel.transform, font, 15, TextAnchor.MiddleCenter);
        SetRect(info.rectTransform, new Vector2(0f, 138f), new Vector2(570f, 32f));
        info.text = "All players are students. Figure / Baldi chases on the server. UDP port: 7777";
        info.color = new Color(0.82f, 0.82f, 0.82f);

        GameObject toggleGo = MakeUI("ModToggle", panel.transform);
        SetRect(toggleGo.GetComponent<RectTransform>(), new Vector2(-130f, 94f), new Vector2(240f, 34f));
        Toggle toggle = toggleGo.AddComponent<Toggle>();
        Image toggleBg = toggleGo.AddComponent<Image>();
        toggleBg.color = new Color(0.22f, 0.22f, 0.28f);
        toggle.targetGraphic = toggleBg;
        GameObject checkMark = MakeUI("Check", toggleGo.transform);
        SetRect(checkMark.GetComponent<RectTransform>(), Vector2.zero, new Vector2(28f, 28f));
        Image checkImage = checkMark.AddComponent<Image>();
        checkImage.color = new Color(1f, 0.8f, 0.2f);
        toggle.graphic = checkImage;
        Text toggleLabel = MakeText("Etiket", toggleGo.transform, font, 16, TextAnchor.MiddleLeft);
        SetRect(toggleLabel.rectTransform, new Vector2(155f, 0f), new Vector2(210f, 34f));
        toggleLabel.text = "Endless modu";
        toggleLabel.color = Color.white;

        GameObject inputGo = MakeUI("IPField", panel.transform);
        SetRect(inputGo.GetComponent<RectTransform>(), new Vector2(130f, 94f), new Vector2(240f, 34f));
        Image inputBg = inputGo.AddComponent<Image>();
        inputBg.color = Color.white;
        InputField input = inputGo.AddComponent<InputField>();
        Text placeholder = MakeText("Placeholder", inputGo.transform, font, 15, TextAnchor.MiddleLeft);
        SetRect(placeholder.rectTransform, Vector2.zero, new Vector2(225f, 30f), new Vector2(7f, 0f));
        placeholder.text = "Host IP (e.g. 192.168.1.5)";
        placeholder.color = new Color(0.55f, 0.55f, 0.55f);
        input.placeholder = placeholder;
        Text inputText = MakeText("Metin", inputGo.transform, font, 16, TextAnchor.MiddleLeft);
        SetRect(inputText.rectTransform, Vector2.zero, new Vector2(225f, 30f), new Vector2(7f, 0f));
        inputText.color = new Color(0.1f, 0.1f, 0.1f);
        inputText.supportRichText = false;
        input.textComponent = inputText;
        input.text = "127.0.0.1";

        Button host = MakeButton("Host", panel.transform, font, "OYUN KUR (HOST)", new Vector2(-130f, 32f), new Color(0.18f, 0.52f, 0.28f));
        host.onClick.AddListener(() => Launch(true, toggle.isOn, input.text));

        Button join = MakeButton("Katil", panel.transform, font, "KATIL (CLIENT)", new Vector2(130f, 32f), new Color(0.18f, 0.38f, 0.7f));
        join.onClick.AddListener(() => Launch(false, toggle.isOn, input.text));

        statusText = MakeText("Durum", panel.transform, font, 16, TextAnchor.MiddleCenter);
        SetRect(statusText.rectTransform, new Vector2(0f, -12f), new Vector2(570f, 46f));
        statusText.text = "Host: starts game and waits.  Client: enter host IP to join.";
        statusText.color = new Color(0.75f, 0.92f, 0.75f);

        Text help = MakeText("Yardim", panel.transform, font, 14, TextAnchor.MiddleCenter);
        SetRect(help.rectTransform, new Vector2(0f, -62f), new Vector2(570f, 46f));
        help.text = "Same PC test: one window Host, other Client (127.0.0.1).\nOther PCs: enter host LAN IP. Windows + Linux must match.";
        help.color = new Color(0.62f, 0.62f, 0.62f);

        Button back = MakeButton("Back", panel.transform, font, "BACK TO SINGLE PLAYER", new Vector2(0f, -132f), new Color(0.32f, 0.32f, 0.38f));
        back.onClick.AddListener(() =>
        {
            MultiplayerSession.Reset();
            Object.Destroy(overlay);
            overlay = null;
            statusText = null;
        });
    }

    private static void Launch(bool host, bool endless, string address)
    {
        address = string.IsNullOrWhiteSpace(address) ? "127.0.0.1" : address.Trim();

        if (statusText != null)
            statusText.text = host ? "Starting server..." : "Connecting to: " + address;

        if (host) MultiplayerActions.Host(endless);
        else MultiplayerActions.Join(address, endless);
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Object.DontDestroyOnLoad(es);
    }

    private static Font LoadFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }

    private static GameObject MakeUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static Text MakeText(string name, Transform parent, Font font, int size, TextAnchor anchor)
    {
        GameObject go = MakeUI(name, parent);
        Text text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = anchor;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static Button MakeButton(string name, Transform parent, Font font, string label, Vector2 position, Color color)
    {
        GameObject go = MakeUI(name, parent);
        SetRect(go.GetComponent<RectTransform>(), position, new Vector2(230f, 50f));
        Image image = go.AddComponent<Image>();
        image.color = color;
        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(color.r + 0.12f, color.g + 0.12f, color.b + 0.12f);
        button.colors = colors;
        Text text = MakeText("Etiket", go.transform, font, 18, TextAnchor.MiddleCenter);
        SetRect(text.rectTransform, Vector2.zero, new Vector2(220f, 46f));
        text.text = label;
        text.color = Color.white;
        text.fontStyle = FontStyle.Bold;
        return button;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size, Vector2? anchoredOffset = null)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position + (anchoredOffset ?? Vector2.zero);
    }
}
