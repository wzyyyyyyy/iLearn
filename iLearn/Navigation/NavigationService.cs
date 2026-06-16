namespace iLearn.Navigation;

public sealed class NavigationService
{
    public event EventHandler<AppRoute>? RouteChanged;

    public AppRoute CurrentRoute { get; private set; } = AppRoute.Courses;

    public void NavigateTo(AppRoute route)
    {
        if (route == CurrentRoute)
        {
            return;
        }

        CurrentRoute = route;
        RouteChanged?.Invoke(this, route);
    }
}
