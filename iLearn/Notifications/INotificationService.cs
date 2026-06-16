using System.Collections.ObjectModel;

namespace iLearn.Notifications;

public interface INotificationService
{
    ReadOnlyObservableCollection<AppNotification> Items { get; }

    void Show(string title, string message, AppNotificationKind kind);

    void Clear(AppNotification notification);
}
