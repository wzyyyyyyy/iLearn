namespace iLearn.Notifications;

public sealed record AppNotification(
    string Title,
    string Message,
    AppNotificationKind Kind,
    DateTimeOffset CreatedAt)
{
    public string AccentBrush => Kind switch
    {
        AppNotificationKind.Success => "#0F766E",
        AppNotificationKind.Warning => "#B45309",
        AppNotificationKind.Error => "#B91C1C",
        _ => "#2563EB"
    };

    public string BackgroundBrush => Kind switch
    {
        AppNotificationKind.Success => "#ECFDF5",
        AppNotificationKind.Warning => "#FFFBEB",
        AppNotificationKind.Error => "#FEF2F2",
        _ => "#EFF6FF"
    };

    public string BorderBrush => Kind switch
    {
        AppNotificationKind.Success => "#A7F3D0",
        AppNotificationKind.Warning => "#FDE68A",
        AppNotificationKind.Error => "#FECACA",
        _ => "#BFDBFE"
    };
}

public enum AppNotificationKind
{
    Info,
    Success,
    Warning,
    Error
}
