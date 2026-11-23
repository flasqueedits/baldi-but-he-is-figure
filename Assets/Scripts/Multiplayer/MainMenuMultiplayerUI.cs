using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Wires the custom notebook-style MainMenu multiplayer panel to the networking helpers.
/// </summary>
public class MainMenuMultiplayerUI : MonoBehaviour
{
    public TMP_InputField ipInput;
    public Toggle endlessToggle;
    public TMP_Text statusText;
    public Button hostButton;
    public Button joinButton;
    public Button singleButton;
    public Button backButton;

    private void Awake()
    {
        MultiplayerMenu.UseRuntimeMenu = false;
        NetworkHud.UseRuntimeHud = false;

        GameObject runtimeMenu = GameObject.Find("FigureMultiplayerMenu");
        if (runtimeMenu != null) Destroy(runtimeMenu);
    }

    private void Start()
    {
        if (ipInput != null && string.IsNullOrWhiteSpace(ipInput.text))
            ipInput.text = "127.0.0.1";

        if (hostButton != null) hostButton.onClick.AddListener(OnHost);
        if (joinButton != null) joinButton.onClick.AddListener(OnJoin);
        if (singleButton != null) singleButton.onClick.AddListener(OnSinglePlayer);
        if (backButton != null) backButton.onClick.AddListener(OnBack);

        ForceAsciiTexts();
        SetStatus("HOST: starts the game.   CLIENT: enter host IP to join.");
    }

    /// <summary>
    /// Overwrites every label with ASCII so an older scene still open in the editor
    /// cannot show missing-glyph boxes for Turkish characters.
    /// </summary>
    private void ForceAsciiTexts()
    {
        SetText("MpPanel/Baslik", "FIGURE CO-OP");
        SetText("MpPanel/BaslikGolge", "FIGURE CO-OP");
        SetText("MpPanel/AltBaslik", "Multiplayer in Baldi's School!");
        SetText("MpPanel/Bilgi", "All students - Figure / Baldi chases on server   |   UDP 7777");
        SetText("MpPanel/Status", "HOST: starts the game.   CLIENT: enter host IP to join.");
        SetText("MpPanel/Help", "Same PC: HOST + CLIENT (127.0.0.1).  Online: paste wss://your-app.onrender.com\nOther PCs on LAN: enter host LAN IP.  Press Enter to chat, P to ping.");
        SetText("MpPanel/HostButton/Label", "OYUN KUR (HOST)");
        SetText("MpPanel/JoinButton/Label", "KATIL (CLIENT)");
        SetText("MpPanel/SingleButton/Label", "TEK OYUNCU");
        SetText("MpPanel/BackButton/Label", "KAPAT");
        SetText("MpPanel/EndlessToggle/Label", "ENDLESS MODU");
    }

    private void SetText(string path, string value)
    {
        Transform child = transform.Find(path);
        if (child == null) return;
        TMP_Text text = child.GetComponent<TMP_Text>();
        if (text != null) text.text = value;
    }

    private bool Endless => endlessToggle != null && endlessToggle.isOn;

    private void OnHost()
    {
        SetStatus("Starting server... get ready!");
        MultiplayerActions.Host(Endless);
    }

    private void OnJoin()
    {
        string address = ipInput != null ? ipInput.text : "127.0.0.1";
        SetStatus("Connecting to: " + address);
        MultiplayerActions.Join(address, Endless);
    }

    private void OnSinglePlayer()
    {
        SetStatus("Starting single player...");
        MultiplayerActions.SinglePlayer(Endless);
    }

    private void OnBack()
    {
        gameObject.SetActive(false);
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }
}
