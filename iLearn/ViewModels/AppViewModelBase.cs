namespace iLearn.ViewModels;

public abstract partial class AppViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusText = string.Empty;

    protected void BeginBusy(string statusText)
    {
        StatusText = statusText;
        IsBusy = true;
    }

    protected void EndBusy(string statusText = "")
    {
        StatusText = statusText;
        IsBusy = false;
    }
}
