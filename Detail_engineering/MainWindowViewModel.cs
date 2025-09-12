using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace Detail_engineering
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void Raise([CallerMemberName] string p = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

        public MainWindowViewModel()
        {
            // محتوای اولیه: صفحه‌ی خانه
            _currentView = new HomeView();
            SidebarWidth = new GridLength(180);
        }

        // Collapsible sidebar
        private bool _isSidebarCollapsed;
        public bool IsSidebarCollapsed
        {
            get => _isSidebarCollapsed;
            set
            {
                if (_isSidebarCollapsed == value) return;
                _isSidebarCollapsed = value;
                SidebarWidth = value ? new GridLength(85) : new GridLength(180);
                Raise(); Raise(nameof(SidebarToggleText));
            }
        }

        private GridLength _sidebarWidth;
        public GridLength SidebarWidth { get => _sidebarWidth; set { _sidebarWidth = value; Raise(); } }

        public string SidebarToggleText => IsSidebarCollapsed ? "Expand" : "Collapse";

        // Current content view (swapped in-place)
        private UserControl _currentView;
        public UserControl CurrentView
        {
            get => _currentView;
            set { _currentView = value; Raise(); }
        }
    }
}
