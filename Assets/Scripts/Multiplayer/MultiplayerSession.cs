/// <summary>
/// Shared state between the main menu and the game scene.
/// </summary>
public static class MultiplayerSession
{
    public enum LaunchMode
    {
        None,
        Host,
        Client
    }

    public static LaunchMode Mode = LaunchMode.None;
    public static string Address = "127.0.0.1";
    public static bool Endless;
    public static bool UseWebSocket;

    public static void Reset()
    {
        Mode = LaunchMode.None;
    }
}
