using CommunityToolkit.Mvvm.Messaging;
using iLearn.Helpers.Messages;
using iLearn.Models;
using iLearn.Navigation;
using iLearn.Notifications;
using iLearn.Services;
using iLearn.ViewModels;
using System.Collections.ObjectModel;

namespace iLearn.ViewModels.Pages;

public sealed partial class CoursesViewModel : AppViewModelBase
{
    private readonly ILearnApiService _ilearnApiService;
    private readonly NavigationService _navigationService;
    private readonly INotificationService _notifications;

    [ObservableProperty]
    private ObservableCollection<TermInfo> _termsOptions = [];

    [ObservableProperty]
    private TermInfo? _selectedTerm;

    [ObservableProperty]
    private ObservableCollection<ClassInfo> _myCourses = [];

    public CoursesViewModel(
        ILearnApiService ilearnApiService,
        NavigationService navigationService,
        INotificationService notifications)
    {
        _ilearnApiService = ilearnApiService;
        _navigationService = navigationService;
        _notifications = notifications;
        _ = InitializeAsync();
    }

    partial void OnSelectedTermChanged(TermInfo? value)
    {
        if (value is not null)
            _ = LoadCoursesAsync(value);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await InitializeAsync();
    }

    [RelayCommand]
    private void CourseSelected(ClassInfo? course)
    {
        if (course is null)
        {
            _notifications.Show("无法打开课程", "课程数据为空，请刷新后重试", AppNotificationKind.Warning);
            return;
        }

        WeakReferenceMessenger.Default.Send(new CourseMessage { classInfo = course });
        _navigationService.NavigateTo(AppRoute.Media);
    }

    private async Task InitializeAsync()
    {
        BeginBusy("正在加载学期...");
        try
        {
            var terms = await _ilearnApiService.GetTermsAsync();
            TermsOptions = new ObservableCollection<TermInfo>(terms);
            SelectedTerm = TermsOptions.FirstOrDefault();
            if (SelectedTerm is null)
                MyCourses.Clear();
        }
        catch (Exception ex)
        {
            _notifications.Show("课程加载失败", ex.Message, AppNotificationKind.Error);
            StatusText = "课程加载失败";
        }
        finally
        {
            EndBusy();
        }
    }

    private async Task LoadCoursesAsync(TermInfo term)
    {
        BeginBusy("正在加载课程...");
        try
        {
            var classes = await _ilearnApiService.GetClassesAsync(term.Year, term.Num);
            MyCourses = new ObservableCollection<ClassInfo>(classes);
            StatusText = classes.Count == 0 ? "当前学期没有课程" : $"已加载 {classes.Count} 门课程";
        }
        catch (Exception ex)
        {
            _notifications.Show("课程加载失败", ex.Message, AppNotificationKind.Error);
            StatusText = "课程加载失败";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
