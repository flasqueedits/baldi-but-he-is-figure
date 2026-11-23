using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Static helpers for custom GUI buttons. Call these from your own UI;
/// they handle mode, address, scene load and shutdown.
/// </summary>
public static class MultiplayerActions
{
    public static void Host(bool endless)
    {
        MultiplayerSession.Mode = MultiplayerSession.LaunchMode.Host;
        MultiplayerSession.Endless = endless;
        MultiplayerSession.Address = "127.0.0.1";
        LoadGame();
    }

    public static void Join(string address, bool endless)
    {
        MultiplayerSession.Mode = MultiplayerSession.LaunchMode.Client;
        MultiplayerSession.Endless = endless;
        MultiplayerSession.Address = string.IsNullOrWhiteSpace(address) ? "127.0.0.1" : address.Trim();
        MultiplayerSession.UseWebSocket = MultiplayerSession.Address.StartsWith("ws://") ||
                                          MultiplayerSession.Address.StartsWith("wss://");
        LoadGame();
    }

    public static void SinglePlayer(bool endless)
    {
        MultiplayerSession.Reset();
        LoadGame();
    }

    public static void ReturnToMenu()
    {
        MultiplayerSession.Reset();
        if (NetworkServer.active) NetworkManager.singleton.StopServer();
        if (NetworkClient.active) NetworkManager.singleton.StopClient();
        SceneManager.LoadScene("MainMenu");
    }

    private static void LoadGame()
    {
        PlayerPrefs.SetString("CurrentMode", MultiplayerSession.Endless ? "endless" : "story");
        PlayerPrefs.Save();
        SceneManager.LoadScene("School");
    }
}
