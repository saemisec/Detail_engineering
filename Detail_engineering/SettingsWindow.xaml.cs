using System.Windows;

namespace Detail_engineering
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            var vm = new SettingsWindowViewModel();
            vm.CloseRequested += (_, __) => this.Close();
            this.DataContext = vm;
        }
        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
    
}
