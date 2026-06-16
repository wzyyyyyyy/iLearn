using iLearn.Navigation;
using Xunit;

namespace iLearn.Tests.Navigation;

public sealed class NavigationServiceTests
{
    [Fact]
    public void NavigateTo_ChangesCurrentRoute_AndRaisesRouteChanged()
    {
        var service = new NavigationService();
        AppRoute? raisedRoute = null;
        var raiseCount = 0;
        service.RouteChanged += (_, route) =>
        {
            raisedRoute = route;
            raiseCount++;
        };

        service.NavigateTo(AppRoute.Downloads);

        Assert.Equal(AppRoute.Downloads, service.CurrentRoute);
        Assert.Equal(AppRoute.Downloads, raisedRoute);
        Assert.Equal(1, raiseCount);
    }
}
