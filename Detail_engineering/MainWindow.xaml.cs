using System.Windows;

namespace Detail_engineering
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private MainWindowViewModel VM => (MainWindowViewModel)DataContext;

        private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            VM.IsSidebarCollapsed = !VM.IsSidebarCollapsed;
        }

        private void Docs_Click(object sender, RoutedEventArgs e)
        {
            VM.CurrentView = new DocumentView(); // داخل همان ContentControl نمایش داده می‌شود
        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            VM.CurrentView = new HomeView(); // داخل همان ContentControl نمایش داده می‌شود
        }

        private void ThreeD_Click(object sender, RoutedEventArgs e)
        {
            VM.CurrentView = new ThreeDView();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
