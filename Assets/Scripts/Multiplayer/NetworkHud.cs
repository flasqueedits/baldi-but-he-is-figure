using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Runtime GUI for the co-op session: connection state, connected student list,
/// caught overlay and a disconnect / return-to-menu panel. Press Tab to toggle the list.
/// </summary>
public class NetworkHud : MonoBehaviour
{
    /// <summary>Set to false if you build your own in-game HUD.</summary>
    public static bool UseRuntimeHud = true;

    private static NetworkHud instance;

    private Text infoText;
    private GameObject caughtPanel;
    private GameObject disconnectPanel;
    private GameObject winPanel;
    private Text chatText;
    private InputField chatInput;
    private string chatLog = "";
    private bool listVisible = true;
    private bool wasConnected;
    private float refreshTimer;

    public static void Create(NetworkPlayer localPlayer)
    {
        if (!UseRuntimeHud) return;
        if (instance != null) return;
        if (localPlayer == null) return;

        GameObject go = new GameObject("FigureMultiplayerHud", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        instance = go.AddComponent<NetworkHud>();
        instance.Build(localPlayer);
    }

    public static void ShowCaught()
    {
        if (instance == null) return;
        instance.caughtPanel.SetActive(true);
    }

    public static void ShowWin()
    {
        if (instance == null) return;
        instance.winPanel.SetActive(true);
    }

    public static void AddChat(string author, string message)
    {
        if (instance == null) return;
        instance.chatLog += author + ": " + message + "\n";
        if (instance.chatLog.Length > 600)
            instance.chatLog = instance.chatLog.Substring(instance.chatLog.Length - 600);
        if (instance.chatText != null) instance.chatText.text = instance.chatLog;
    }

    public static void ShowPing(Vector3 position)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "PingMarker";
        marker.transform.position = position + Vector3.up * 1.5f;
        marker.transform.localScale = Vector3.one * 0.6f;
        Collider col = marker.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);
        Renderer renderer = marker.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            renderer.material.color = new Color(0.2f, 1f, 1f);
            renderer.material.EnableKeyword("_EMISSION");
        }
        Object.Destroy(marker, 3f);
    }

    private void Build(NetworkPlayer localPlayer)
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        GameObject panel = new GameObject("BilgiPaneli", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(14f, -12f);
        panelRect.sizeDelta = new Vector2(340f, 190f);
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.55f);

        GameObject textGo = new GameObject("Bilgi", typeof(RectTransform));
        textGo.transform.SetParent(panel.transform, false);
        RectTransform rect = textGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(10f, -8f);
        rect.sizeDelta = new Vector2(320f, 175f);

        infoText = textGo.AddComponent<Text>();
        infoText.font = font;
        infoText.fontSize = 17;
        infoText.color = new Color(1f, 0.9f, 0.5f);
        infoText.alignment = TextAnchor.UpperLeft;
        infoText.text = localPlayer.playerName + "\nCO-OP  |  Port " + BaldiNetworkManager.DefaultPort;

        Text hint = new GameObject("Ipucu", typeof(RectTransform)).AddComponent<Text>();
        hint.transform.SetParent(transform, false);
        RectTransform hintRect = hint.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0f, 0f);
        hintRect.anchorMax = new Vector2(0f, 0f);
        hintRect.pivot = new Vector2(0f, 0f);
        hintRect.anchoredPosition = new Vector2(14f, 12f);
        hintRect.sizeDelta = new Vector2(420f, 30f);
        hint.font = font;
        hint.fontSize = 14;
        hint.color = new Color(0.8f, 0.8f, 0.8f, 0.8f);
        hint.text = "Tab: player list   |   Enter: chat   |   P: ping";

        // Chat log
        GameObject chatPanel = new GameObject("Chat", typeof(RectTransform));
        chatPanel.transform.SetParent(transform, false);
        RectTransform chatRect = chatPanel.GetComponent<RectTransform>();
        chatRect.anchorMin = new Vector2(1f, 0f);
        chatRect.anchorMax = new Vector2(1f, 0f);
        chatRect.pivot = new Vector2(1f, 0f);
        chatRect.anchoredPosition = new Vector2(-14f, 14f);
        chatRect.sizeDelta = new Vector2(420f, 200f);
        Image chatBg = chatPanel.AddComponent<Image>();
        chatBg.color = new Color(0f, 0f, 0f, 0.5f);

        GameObject logGo = new GameObject("Log", typeof(RectTransform));
        logGo.transform.SetParent(chatPanel.transform, false);
        RectTransform logRect = logGo.GetComponent<RectTransform>();
        logRect.anchorMin = new Vector2(0f, 1f);
        logRect.anchorMax = new Vector2(0f, 1f);
        logRect.pivot = new Vector2(0f, 1f);
        logRect.anchoredPosition = new Vector2(8f, -6f);
        logRect.sizeDelta = new Vector2(404f, 155f);
        chatText = logGo.AddComponent<Text>();
        chatText.font = font;
        chatText.fontSize = 16;
        chatText.color = new Color(0.9f, 0.95f, 1f);
        chatText.alignment = TextAnchor.LowerLeft;

        GameObject inputGo = new GameObject("Input", typeof(RectTransform));
        inputGo.transform.SetParent(chatPanel.transform, false);
        RectTransform inputRect = inputGo.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0f, 0f);
        inputRect.anchorMax = new Vector2(0f, 0f);
        inputRect.pivot = new Vector2(0f, 0f);
        inputRect.anchoredPosition = new Vector2(8f, 8f);
        inputRect.sizeDelta = new Vector2(404f, 32f);
        Image inputBg = inputGo.AddComponent<Image>();
        inputBg.color = new Color(1f, 1f, 1f, 0.85f);
        chatInput = inputGo.AddComponent<InputField>();
        Text inputText = new GameObject("Text", typeof(RectTransform)).AddComponent<Text>();
        inputText.transform.SetParent(inputGo.transform, false);
        RectTransform inputTextRect = inputText.GetComponent<RectTransform>();
        inputTextRect.anchorMin = Vector2.zero;
        inputTextRect.anchorMax = Vector2.one;
        inputTextRect.offsetMin = new Vector2(6f, 2f);
        inputTextRect.offsetMax = new Vector2(-6f, -2f);
        inputText.font = font;
        inputText.fontSize = 15;
        inputText.color = new Color(0.1f, 0.1f, 0.1f);
        chatInput.textComponent = inputText;

        caughtPanel = BuildOverlay("Yakalandi", font,
            "FIGURE CAUGHT YOU!\nOther players keep playing.",
            new Color(1f, 0.25f, 0.2f), false);

        winPanel = BuildOverlay("Kazandin", font,
            "ALL NOTEBOOKS COLLECTED - YOU WIN!",
            new Color(0.3f, 1f, 0.45f), true);
        winPanel.SetActive(false);

        disconnectPanel = BuildOverlay("BaglantiKesildi", font,
            "HOST CONNECTION LOST",
            new Color(1f, 0.45f, 0.2f), true);
        disconnectPanel.SetActive(false);
    }

    private GameObject BuildOverlay(string name, Font font, string message, Color color, bool withButton)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.8f);

        GameObject messageGo = new GameObject("Mesaj", typeof(RectTransform));
        messageGo.transform.SetParent(panel.transform, false);
        RectTransform msgRect = messageGo.GetComponent<RectTransform>();
        msgRect.anchorMin = new Vector2(0.5f, 0.5f);
        msgRect.anchorMax = new Vector2(0.5f, 0.5f);
        msgRect.pivot = new Vector2(0.5f, 0.5f);
        msgRect.sizeDelta = new Vector2(900f, 220f);
        msgRect.anchoredPosition = new Vector2(0f, withButton ? 60f : 0f);
        Text msgText = messageGo.AddComponent<Text>();
        msgText.font = font;
        msgText.fontSize = 42;
        msgText.color = color;
        msgText.alignment = TextAnchor.MiddleCenter;
        msgText.text = message;

        if (withButton)
        {
            GameObject buttonGo = new GameObject("MenuyeDon", typeof(RectTransform));
            buttonGo.transform.SetParent(panel.transform, false);
            RectTransform btnRect = buttonGo.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.sizeDelta = new Vector2(280f, 56f);
            btnRect.anchoredPosition = new Vector2(0f, -90f);
            Image btnImage = buttonGo.AddComponent<Image>();
            btnImage.color = new Color(0.2f, 0.45f, 0.75f);
            Button button = buttonGo.AddComponent<Button>();
            button.targetGraphic = btnImage;
            button.onClick.AddListener(ReturnToMenu);
            Text btnText = new GameObject("Etiket", typeof(RectTransform)).AddComponent<Text>();
            btnText.transform.SetParent(buttonGo.transform, false);
            RectTransform btnTextRect = btnText.GetComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero;
            btnTextRect.offsetMax = Vector2.zero;
            btnText.font = font;
            btnText.fontSize = 20;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.white;
            btnText.text = "BACK TO MAIN MENU";
        }

        panel.SetActive(false);
        return panel;
    }

    private void HandleCommand(string message)
    {
        string command = message.ToLowerInvariant();
        if (command == "/help")
        {
            AddChat("System", "Commands: /help  /ping  /where");
        }
        else if (command == "/ping")
        {
            if (NetworkClient.localPlayer != null)
                NetworkWorld.Instance.RequestPing(NetworkClient.localPlayer.transform.position);
        }
        else if (command == "/where")
        {
            if (NetworkClient.localPlayer != null)
                AddChat("System", "Position: " + NetworkClient.localPlayer.transform.position);
        }
        else
        {
            AddChat("System", "Unknown command. Type /help");
        }
    }

    private static void ReturnToMenu()
    {
        MultiplayerActions.ReturnToMenu();
    }

    private void Update()
    {
        if (NetworkWorld.Instance == null) return;

        if (chatInput != null && Input.GetKeyDown(KeyCode.Return) && !string.IsNullOrEmpty(chatInput.text))
        {
            string message = chatInput.text;
            if (message.StartsWith("/"))
            {
                HandleCommand(message);
            }
            else
            {
                NetworkWorld.Instance.RequestChat(message);
            }
            chatInput.text = "";
        }
        if (Input.GetKeyDown(KeyCode.P) && NetworkClient.localPlayer != null)
        {
            NetworkWorld.Instance.RequestPing(NetworkClient.localPlayer.transform.position);
        }

        if (Input.GetKeyDown(KeyCode.Tab)) listVisible = !listVisible;
        if (infoText == null || !listVisible) return;

        bool connected = NetworkClient.active || NetworkServer.active;
        if (wasConnected && !connected && disconnectPanel != null)
            disconnectPanel.SetActive(true);
        wasConnected = connected;

        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer > 0f) return;
        refreshTimer = 0.25f;

        int connections = NetworkServer.active ? NetworkServer.connections.Count : (NetworkClient.active ? 1 : 0);
        string text = "CO-OP MULTIPLAYER\n" +
                      "Players: " + connections + "  |  UDP " + BaldiNetworkManager.DefaultPort + "\n\n";

        NetworkPlayer[] players = FindObjectsOfType<NetworkPlayer>();
        foreach (NetworkPlayer player in players)
        {
            if (player == null) continue;
            text += "#" + player.playerNumber + "  " + player.playerName;
            if (player.isLocalPlayer) text += "  (sen)";
            if (player.caught) text += "  [caught]";
            text += "\n";
        }
        infoText.text = text;
    }
}
