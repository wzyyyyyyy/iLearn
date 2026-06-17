using iLearn.Notifications;
using Xunit;

namespace iLearn.Tests.Notifications;

public sealed class NotificationServiceTests
{
    [Fact]
    public async Task Show_RemovesNotificationAfterAutoDismissDelay()
    {
        var service = new NotificationService(TimeSpan.FromMilliseconds(20));

        service.Show("完成", "操作已完成", AppNotificationKind.Success);
        Assert.Single(service.Items);

        await Task.Delay(120, TestContext.Current.CancellationToken);

        Assert.Empty(service.Items);
    }
}
