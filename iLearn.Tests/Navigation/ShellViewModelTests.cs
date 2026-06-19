using iLearn.Navigation;
using iLearn.Notifications;
using iLearn.ViewModels;
using Xunit;

namespace iLearn.Tests.Navigation;

public sealed class ShellViewModelTests
{
    [Fact]
    public void ExternalNavigation_UpdatesSelectedNavigationItem()
    {
        var navigation = new NavigationService();
        var viewModel = new ShellViewModel(navigation, new NotificationService());

        navigation.NavigateTo(AppRoute.Media);

        Assert.Equal(AppRoute.Media, viewModel.CurrentRoute);
        Assert.Equal("课程视频", viewModel.CurrentRouteTitle);
        Assert.True(viewModel.Items.Single(item => item.Route == AppRoute.Media).IsSelected);
        Assert.False(viewModel.Items.Single(item => item.Route == AppRoute.Courses).IsSelected);
    }
}
