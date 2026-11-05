namespace SmartGreenhouse.Interfaces;

public enum NoticeKind { Success, Warning, Info }

public static class Notifications
{
    public static event System.Action<string, NoticeKind>? Published;

    public static void Publish(string text, NoticeKind kind = NoticeKind.Info)
    {
        Published?.Invoke(text, kind);
    }
}
