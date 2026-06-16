using System.Collections.ObjectModel;

namespace iLearn.Notifications;

public sealed class NotificationService : INotificationService
{
    private const int MaxNotifications = 5;
    private readonly ObservableCollection<AppNotification> _notifications = new();

    public NotificationService()
    {
        Items = new ReadOnlyObservableCollection<AppNotification>(_notifications);
    }

    public ReadOnlyObservableCollection<AppNotification> Items { get; }

    public void Show(string title, string message, AppNotificationKind kind)
    {
        _notifications.Insert(0, new AppNotification(title, message, kind, DateTimeOffset.Now));

        while (_notifications.Count > MaxNotifications)
        {
            _notifications.RemoveAt(_notifications.Count - 1);
        }
    }

    public void Clear(AppNotification notification)
    {
        _notifications.Remove(notification);
    }
}
