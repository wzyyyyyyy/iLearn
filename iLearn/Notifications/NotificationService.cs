using System.Collections.ObjectModel;

namespace iLearn.Notifications;

public sealed class NotificationService : INotificationService
{
    private const int MaxNotifications = 5;
    private readonly ObservableCollection<AppNotification> _notifications = new();
    private readonly SynchronizationContext? _collectionContext;

    public NotificationService()
    {
        _collectionContext = SynchronizationContext.Current;
        Items = new ReadOnlyObservableCollection<AppNotification>(_notifications);
    }

    public ReadOnlyObservableCollection<AppNotification> Items { get; }

    public void Show(string title, string message, AppNotificationKind kind)
    {
        if (_collectionContext is not null && SynchronizationContext.Current != _collectionContext)
        {
            _collectionContext.Post(_ => ShowCore(title, message, kind), null);
            return;
        }

        ShowCore(title, message, kind);
    }

    public void Clear(AppNotification notification)
    {
        if (_collectionContext is not null && SynchronizationContext.Current != _collectionContext)
        {
            _collectionContext.Post(_ => _notifications.Remove(notification), null);
            return;
        }

        _notifications.Remove(notification);
    }

    private void ShowCore(string title, string message, AppNotificationKind kind)
    {
        _notifications.Insert(0, new AppNotification(title, message, kind, DateTimeOffset.Now));

        while (_notifications.Count > MaxNotifications)
        {
            _notifications.RemoveAt(_notifications.Count - 1);
        }
    }
}
