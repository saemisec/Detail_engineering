using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Detail_engineering
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void Raise([CallerMemberName] string p = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

        public ICommand OpenSettingsCommand { get; }

        public MainWindowViewModel()
        {
            // محتوای اولیه: صفحه‌ی خانه
            _currentView = new HomeView();
            SidebarWidth = new GridLength(180);
            OpenSettingsCommand = new RelayCommand(_ => OpenSettings());
        }

        private void OpenSettings()
        {
            var w = new SettingsWindow
            {
                Owner = System.Windows.Application.Current.MainWindow,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
            };
            w.ShowDialog();

            // پس از بستن، اگر نیاز به اعمال اضافی داری:
            //PathHelper.BaseDir = Properties.Settings.Default.BaseFolder ?? "";
            // اگر WebView2 داری و JSON را از WPF سرو می‌کنی،
            // اینجا می‌تونی رفرش/Reload بدهی.
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
