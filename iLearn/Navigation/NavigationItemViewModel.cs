namespace iLearn.Navigation;

public sealed partial class NavigationItemViewModel : ObservableObject
{
    public NavigationItemViewModel(AppRoute route, string title, string iconKey)
    {
        Route = route;
        Title = title;
        IconKey = iconKey;
    }

    public AppRoute Route { get; }

    public string Title { get; }

    public string IconKey { get; }

    [ObservableProperty]
    private bool _isSelected;
}
