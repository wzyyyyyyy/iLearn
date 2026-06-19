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

    public string BackgroundBrush => IsSelected ? "#203C3A" : "Transparent";

    public string ForegroundBrush => IsSelected ? "#D8FFF4" : "#C7D4DC";

    public string IndicatorBrush => IsSelected ? "#0F766E" : "Transparent";

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(BackgroundBrush));
        OnPropertyChanged(nameof(ForegroundBrush));
        OnPropertyChanged(nameof(IndicatorBrush));
    }
}
