using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace iLearn.ViewModels.Pages
{
    public partial class MediaViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private ObservableCollection<MediaItem> _mediaItems = new();

        public MediaViewModel()
        {
            // 初始化一些示例数据
            LoadSampleData();
        }

        [RelayCommand]
        private void Search()
        {
            // 实现搜索逻辑
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                LoadSampleData();
                return;
            }

            // 简单过滤逻辑示例
            var filteredItems = MediaItems.Where(item =>
                item.Title.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                item.Description.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))
                .ToList();

            MediaItems.Clear();
            foreach (var item in filteredItems)
            {
                MediaItems.Add(item);
            }
        }

        [RelayCommand]
        private void OpenMedia(MediaItem media)
        {
            // 实现打开媒体的逻辑
            // 例如：打开详情页面或播放媒体
        }

        private void LoadSampleData()
        {
            MediaItems.Clear();

            // 添加一些示例数据
            MediaItems.Add(new MediaItem
            {
                Id = 1,
                Title = "自然风光",
                Description = "美丽的自然景观摄影集锦",
                ThumbnailUrl = "/Assets/Images/media1.jpg"
            });

            MediaItems.Add(new MediaItem
            {
                Id = 2,
                Title = "城市建筑",
                Description = "现代城市建筑摄影集",
                ThumbnailUrl = "/Assets/Images/media2.jpg"
            });

            // 添加更多示例数据...
            for (int i = 3; i <= 12; i++)
            {
                MediaItems.Add(new MediaItem
                {
                    Id = i,
                    Title = $"媒体项目 {i}",
                    Description = $"这是媒体项目 {i} 的描述信息",
                    ThumbnailUrl = $"/Assets/Images/media{i % 3 + 1}.jpg"
                });
            }
        }
    }

    // 媒体项数据模型
    public class MediaItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
    }
}