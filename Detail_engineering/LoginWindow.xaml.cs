using Detail_engineering;
using System.Threading.Tasks;
using System.Windows;

namespace Detail_engineering
{
    public partial class LoginWindow : Window
    {
        private const string HardUser = "admin";
        private const string HardPass = "Ardestan";

        private int _attempts = 0;
        private const int MaxAttempts = 3;

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            var u = TxtUser.Text?.Trim() ?? "";
            var p = TxtPass.Password ?? "";

            if (u == HardUser && p == HardPass)
            {
                var splash = new SplashWindow();
                splash.Owner = this;
                splash.Show();

                this.Hide();
                await Task.Delay(4000);

                var main = new MainWindow();
                main.Show();

                splash.Close();
                this.Close();
                return;
            }

            _attempts++;
            if (_attempts >= MaxAttempts)
            {
                MessageBox.Show("Too many wrong attempts. The application will close.", "Login",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }

            LblError.Text = $"Wrong password ({_attempts}/{MaxAttempts})";
            LblError.Visibility = Visibility.Visible;
            TxtPass.Clear();
            TxtPass.Focus();
        }
    }
}
