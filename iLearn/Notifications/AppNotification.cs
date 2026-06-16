namespace iLearn.Notifications;

public sealed record AppNotification(
    string Title,
    string Message,
    AppNotificationKind Kind,
    DateTimeOffset CreatedAt);

public enum AppNotificationKind
{
    Info,
    Success,
    Warning,
    Error
}
