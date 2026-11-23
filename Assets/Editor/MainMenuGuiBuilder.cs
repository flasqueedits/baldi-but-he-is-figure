#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds the Baldi notebook-style multiplayer panel inside the existing
/// "MultiplayerMenu" Canvas in MainMenu.unity. Safe to run more than once.
/// </summary>
public static class MainMenuGuiBuilder
{
    private const string ScenePath = "Assets/Scene/MainMenu.unity";
    private const string CanvasName = "MultiplayerMenu";
    private const string PanelName = "MpPanel";
    private const string FontPath = "Assets/Font/TMP/COMIC.asset";
    private const string PaperPath = "Assets/Multiplayer/NotebookPaper.png";
    private const string ButtonPath = "Assets/Multiplayer/PaperButton.png";
    private const string WallPath = "Assets/Multiplayer/ClassroomWall.png";

    [MenuItem("Tools/Figure Multiplayer/MainMenu GUI'sini Kur")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[GUI] Cannot run setup while in Play mode.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject canvasGo = null;
        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.name == CanvasName && go.scene == scene)
            {
                canvasGo = go;
                break;
            }
        }
        if (canvasGo == null)
        {
            Debug.LogError("[GUI] " + CanvasName + " object not found in MainMenu scene.");
            return;
        }
        canvasGo.SetActive(true);

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[GUI] No Canvas component on '" + CanvasName + "'.");
            return;
        }
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        if (canvasGo.GetComponent<GraphicRaycaster>() == null)
            canvasGo.AddComponent<GraphicRaycaster>();

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null) Debug.LogWarning("[GUI] COMIC font not found, using default TMP font.");

        Sprite wall = CreateWallSprite();
        Sprite paper = CreatePaperSprite();
        Sprite buttonSprite = CreateButtonSprite();

        Transform oldPanel = canvasGo.transform.Find(PanelName);
        if (oldPanel != null) Object.DestroyImmediate(oldPanel.gameObject);

        // Classroom wall covering the whole screen.
        GameObject wallGo = MakeUI("SinifDuvari", canvasGo.transform);
        RectTransform wallRect = wallGo.GetComponent<RectTransform>();
        wallRect.anchorMin = Vector2.zero;
        wallRect.anchorMax = Vector2.one;
        wallRect.offsetMin = Vector2.zero;
        wallRect.offsetMax = Vector2.zero;
        Image wallImage = wallGo.AddComponent<Image>();
        wallImage.sprite = wall;
        wallImage.type = Image.Type.Tiled;
        wallImage.color = Color.white;
        wallGo.transform.SetAsFirstSibling();

        GameObject panel = MakeUI(PanelName, canvasGo.transform);
        SetRect(panel.GetComponent<RectTransform>(), new Vector2(0f, -10f), new Vector2(620f, 520f));
        Image panelImage = panel.AddComponent<Image>();
        panelImage.sprite = paper;
        panelImage.type = Image.Type.Simple;
        panelImage.color = Color.white;
        Outline panelOutline = panel.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.35f, 0.22f, 0.1f, 0.9f);
        panelOutline.effectDistance = new Vector2(3f, -3f);
        Shadow panelShadow = panel.AddComponent<Shadow>();
        panelShadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        panelShadow.effectDistance = new Vector2(7f, -7f);

        Color green = new Color(0.12f, 0.62f, 0.16f);
        Color red = new Color(0.85f, 0.12f, 0.12f);
        Color ink = new Color(0.12f, 0.15f, 0.2f);

        TextMeshProUGUI titleShadow = MakeTMP("BaslikGolge", panel.transform, font, 56, Color.black,
            TextAlignmentOptions.Center, new Vector2(3f, 200f), new Vector2(600f, 80f));
        titleShadow.text = "FIGURE CO-OP";
        TextMeshProUGUI title = MakeTMP("Baslik", panel.transform, font, 56, green,
            TextAlignmentOptions.Center, new Vector2(0f, 203f), new Vector2(600f, 80f));
        title.text = "FIGURE CO-OP";
        title.fontStyle = FontStyles.Bold;

        TextMeshProUGUI sub = MakeTMP("AltBaslik", panel.transform, font, 26, red,
            TextAlignmentOptions.Center, new Vector2(0f, 160f), new Vector2(600f, 46f));
        sub.text = "Multiplayer in Baldi's School!";

        TextMeshProUGUI info = MakeTMP("Bilgi", panel.transform, font, 20, ink,
            TextAlignmentOptions.Center, new Vector2(0f, 126f), new Vector2(580f, 38f));
        info.text = "All students - Figure / Baldi chases on server   |   UDP 7777";

        Toggle endless = MakeToggle("EndlessToggle", panel.transform, font, "ENDLESS MODU", new Vector2(-130f, 84f), buttonSprite, ink);
        TMP_InputField ip = MakeInput("IPInput", panel.transform, font, new Vector2(130f, 84f));

        Button host = MakeButton("HostButton", panel.transform, font, "OYUN KUR (HOST)",
            new Vector2(-130f, 20f), new Vector2(250f, 58f), buttonSprite, green);
        Button join = MakeButton("JoinButton", panel.transform, font, "KATIL (CLIENT)",
            new Vector2(130f, 20f), new Vector2(250f, 58f), buttonSprite, new Color(0.1f, 0.35f, 0.75f));

        Button single = MakeButton("SingleButton", panel.transform, font, "TEK OYUNCU",
            new Vector2(-130f, -48f), new Vector2(250f, 50f), buttonSprite, new Color(0.45f, 0.3f, 0.1f));
        Button back = MakeButton("BackButton", panel.transform, font, "KAPAT",
            new Vector2(130f, -48f), new Vector2(250f, 50f), buttonSprite, new Color(0.4f, 0.4f, 0.4f));

        TextMeshProUGUI status = MakeTMP("Status", panel.transform, font, 22, red,
            TextAlignmentOptions.Center, new Vector2(0f, -108f), new Vector2(580f, 46f));
        status.text = "HOST: starts the game.   CLIENT: enter host IP to join.";

        TextMeshProUGUI help = MakeTMP("Help", panel.transform, font, 18, ink,
            TextAlignmentOptions.Center, new Vector2(0f, -175f), new Vector2(580f, 70f));
        help.text = "Same PC: one window HOST, other CLIENT (127.0.0.1).\n" +
                    "Other PCs: enter host LAN IP.";

        MainMenuMultiplayerUI ui = canvasGo.GetComponent<MainMenuMultiplayerUI>();
        if (ui == null) ui = canvasGo.AddComponent<MainMenuMultiplayerUI>();
        ui.ipInput = ip;
        ui.endlessToggle = endless;
        ui.statusText = status;
        ui.hostButton = host;
        ui.joinButton = join;
        ui.singleButton = single;
        ui.backButton = back;
        EditorUtility.SetDirty(ui);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[GUI] Baldi notebook style multiplayer panel built.");
    }

    private static GameObject MakeUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static TextMeshProUGUI MakeTMP(string name, Transform parent, TMP_FontAsset font, int size,
        Color color, TextAlignmentOptions alignment, Vector2 position, Vector2 dimensions)
    {
        GameObject go = MakeUI(name, parent);
        SetRect(go.GetComponent<RectTransform>(), position, dimensions);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        return text;
    }

    private static Button MakeButton(string name, Transform parent, TMP_FontAsset font, string label,
        Vector2 position, Vector2 dimensions, Sprite sprite, Color textColor)
    {
        GameObject go = MakeUI(name, parent);
        SetRect(go.GetComponent<RectTransform>(), position, dimensions);
        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.92f, 0.6f);
        colors.pressedColor = new Color(0.85f, 0.75f, 0.45f);
        button.colors = colors;
        TextMeshProUGUI text = MakeTMP("Label", go.transform, font, 24, textColor,
            TextAlignmentOptions.Center, Vector2.zero, dimensions - new Vector2(12f, 6f));
        text.text = label;
        text.fontStyle = FontStyles.Bold;
        return button;
    }

    private static Toggle MakeToggle(string name, Transform parent, TMP_FontAsset font, string label,
        Vector2 position, Sprite sprite, Color textColor)
    {
        GameObject go = MakeUI(name, parent);
        SetRect(go.GetComponent<RectTransform>(), position, new Vector2(250f, 46f));
        Image bg = go.AddComponent<Image>();
        bg.sprite = sprite;
        bg.type = Image.Type.Sliced;
        Toggle toggle = go.AddComponent<Toggle>();
        toggle.targetGraphic = bg;

        GameObject check = MakeUI("Check", go.transform);
        SetRect(check.GetComponent<RectTransform>(), new Vector2(-98f, 0f), new Vector2(30f, 30f));
        Image checkImage = check.AddComponent<Image>();
        checkImage.color = new Color(0.12f, 0.6f, 0.16f);
        toggle.graphic = checkImage;

        TextMeshProUGUI text = MakeTMP("Label", go.transform, font, 23, textColor,
            TextAlignmentOptions.MidlineLeft, new Vector2(12f, 0f), new Vector2(210f, 44f));
        text.text = label;
        text.fontStyle = FontStyles.Bold;
        return toggle;
    }

    private static TMP_InputField MakeInput(string name, Transform parent, TMP_FontAsset font, Vector2 position)
    {
        GameObject go = MakeUI(name, parent);
        SetRect(go.GetComponent<RectTransform>(), position, new Vector2(250f, 46f));
        Image bg = go.AddComponent<Image>();
        bg.color = new Color(1f, 0.99f, 0.9f);
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.35f, 0.22f, 0.1f);
        outline.effectDistance = new Vector2(2f, -2f);

        TMP_InputField input = go.AddComponent<TMP_InputField>();
        TextMeshProUGUI placeholder = MakeTMP("Placeholder", go.transform, font, 20,
            new Color(0.5f, 0.5f, 0.5f), TextAlignmentOptions.MidlineLeft, Vector2.zero, new Vector2(230f, 42f));
        placeholder.text = "Host IP...";
        input.placeholder = placeholder;

        TextMeshProUGUI value = MakeTMP("Text", go.transform, font, 21, new Color(0.1f, 0.1f, 0.1f),
            TextAlignmentOptions.MidlineLeft, Vector2.zero, new Vector2(230f, 42f));
        input.textComponent = value;
        input.text = "127.0.0.1";
        return input;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 dimensions)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = dimensions;
        rect.anchoredPosition = position;
    }

    private static Sprite SaveSprite(string path, Texture2D texture, Vector4 border)
    {
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = border;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Sprite CreatePaperSprite()
    {
        const int s = 512;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        Color paper = new Color(1f, 0.965f, 0.76f);
        Color line = new Color(0.55f, 0.74f, 0.95f);
        Color margin = new Color(0.93f, 0.35f, 0.35f);
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            Color c = paper;
            if (y % 44 < 2) c = line;
            if (x > 66 && x < 70) c = margin;
            tex.SetPixel(x, y, c);
        }
        tex.Apply();
        return SaveSprite(PaperPath, tex, Vector4.zero);
    }

    private static Sprite CreateButtonSprite()
    {
        const int s = 128;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        Color fill = new Color(1f, 0.9f, 0.45f);
        Color edge = new Color(0.35f, 0.2f, 0.08f);
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            bool border = x < 7 || y < 7 || x >= s - 7 || y >= s - 7;
            tex.SetPixel(x, y, border ? edge : fill);
        }
        tex.Apply();
        return SaveSprite(ButtonPath, tex, new Vector4(16f, 16f, 16f, 16f));
    }

    private static Sprite CreateWallSprite()
    {
        const int s = 256;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        Color mortar = new Color(0.62f, 0.62f, 0.58f);
        Color brick = new Color(0.86f, 0.84f, 0.78f);
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            bool horizontal = y % 32 < 3;
            int offset = (y / 32) % 2 == 0 ? 0 : 32;
            bool vertical = (x + offset) % 64 < 3;
            tex.SetPixel(x, y, horizontal || vertical ? mortar : brick);
        }
        tex.Apply();
        return SaveSprite(WallPath, tex, Vector4.zero);
    }
}
#endif
