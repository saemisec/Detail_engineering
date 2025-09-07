using System.Windows;

namespace Detail_engineering
{
    public partial class DocumentWindow : Window
    {
        public DocumentWindow()
        {
            InitializeComponent();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            if (Owner != null) Owner.Show();
        }
    }
}
