using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wpf.Ui.Controls;

namespace iLearn.Services
{
    public class WindowsManagerService
    {
        private readonly Dictionary<Type, Action> _showActions;
        private readonly Dictionary<Type, Action> _closeActions;

        public WindowsManagerService(
            Dictionary<Type, Action> showActions,
            Dictionary<Type, Action> closeActions)
        {
            _showActions = showActions;
            _closeActions = closeActions;
        }

        public void Show<TViewModel>()
        {
            if (_showActions.TryGetValue(typeof(TViewModel), out var show))
                show();
            else
                throw new InvalidOperationException($"No show action registered for {typeof(TViewModel).Name}");
        }

        public void Close<TViewModel>()
        {
            if (_closeActions.TryGetValue(typeof(TViewModel), out var close))
                close();
            else
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.DataContext is TViewModel)
                    {
                        window.Close();
                        break;
                    }
                }
            }
        }
    }
}
